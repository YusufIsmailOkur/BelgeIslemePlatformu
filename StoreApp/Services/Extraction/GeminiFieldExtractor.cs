using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StoreApp.Models.Fields;
using StoreApp.Services.Abstractions;

namespace StoreApp.Services.Extraction
{
    // Google Gemini (ücretsiz katman, generativelanguage.googleapis.com) ile şema kontrollü
    // JSON çıkarımı yapar (bkz. Föy 06, madde 5). Kural tabanlı çıkarımın (madde 4) boş/zayıf
    // kaldığı alanları doldurmak için DocumentProcessingService tarafından çağrılır.
    public sealed class GeminiFieldExtractor : IAiFieldExtractor
    {
        private const string AiSource = "AI";
        private const double AiConfidence = 0.6;

        private static readonly JsonSerializerOptions ResponseJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiFieldExtractor> _logger;

        public GeminiFieldExtractor(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiFieldExtractor> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ExtractionResult?> ExtractAsync(
            string? rawText, IReadOnlyList<ParsedTable> tables, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return null;
            }

            var content = BuildPromptContent(rawText, tables);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            try
            {
                var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                request.Headers.Add("x-goog-api-key", _options.ApiKey);
                request.Content = JsonContent.Create(BuildRequestBody(content));

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Gemini AI çıkarımı başarısız oldu: HTTP {StatusCode}", (int)response.StatusCode);
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                return ParseResponse(responseJson);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                // AI çıkarımı yardımcı bir özelliktir; hata durumunda pipeline kural tabanlı
                // sonuçla devam eder (bkz. Föy 06 riskleri: "AI çıktısı tutarsız olabilir").
                _logger.LogWarning(ex, "Gemini AI çıkarımı sırasında hata oluştu.");
                return null;
            }
        }

        private static string BuildPromptContent(string? rawText, IReadOnlyList<ParsedTable> tables)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(rawText))
            {
                parts.Add(rawText);
            }

            foreach (var table in tables)
            {
                parts.Add(string.Join(" | ", table.Headers));
                foreach (var row in table.Rows)
                {
                    parts.Add(string.Join(" | ", row));
                }
            }

            return string.Join('\n', parts);
        }

        private static object BuildRequestBody(string documentContent)
        {
            var prompt =
                "Aşağıdaki belge metninden yalnızca açıkça yazılı olan bilgileri çıkar. " +
                "Bir alan belgede yoksa veya emin değilsen null bırak, tahmin veya uydurma yapma.\n\n" +
                "Belge metni:\n" + documentContent;

            return new
            {
                contents = new object[] { new { parts = new object[] { new { text = prompt } } } },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = BuildResponseSchema()
                }
            };
        }

        private static JsonObject BuildResponseSchema()
        {
            var properties = new JsonObject();
            foreach (var field in DocumentFieldSchema.HeaderFields)
            {
                properties[field.Key] = new JsonObject { ["type"] = "STRING", ["nullable"] = true };
            }

            var lineItemProperties = new JsonObject();
            foreach (var field in DocumentFieldSchema.LineItemFields)
            {
                lineItemProperties[field.Key] = new JsonObject { ["type"] = "STRING", ["nullable"] = true };
            }

            properties["line_items"] = new JsonObject
            {
                ["type"] = "ARRAY",
                ["items"] = new JsonObject { ["type"] = "OBJECT", ["properties"] = lineItemProperties }
            };

            return new JsonObject { ["type"] = "OBJECT", ["properties"] = properties };
        }

        private ExtractionResult? ParseResponse(string responseJson)
        {
            using var document = JsonDocument.Parse(responseJson);
            var text = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var dto = JsonSerializer.Deserialize<GeminiExtractionDto>(text, ResponseJsonOptions);
            if (dto is null)
            {
                return null;
            }

            var headerFields = new Dictionary<string, ExtractedField>();
            AddIfPresent(headerFields, "document_number", dto.document_number);
            AddIfPresent(headerFields, "document_date", dto.document_date);
            AddIfPresent(headerFields, "company_name", dto.company_name);
            AddIfPresent(headerFields, "customer_name", dto.customer_name);
            AddIfPresent(headerFields, "due_date", dto.due_date);
            AddIfPresent(headerFields, "description", dto.description);

            var lineItems = new List<ExtractedLineItem>();
            foreach (var item in dto.line_items ?? new List<GeminiLineItemDto>())
            {
                var fields = new Dictionary<string, ExtractedField>();
                AddIfPresent(fields, "item_description", item.item_description);
                AddIfPresent(fields, "quantity", item.quantity);
                AddIfPresent(fields, "unit", item.unit);
                AddIfPresent(fields, "unit_price", item.unit_price);
                AddIfPresent(fields, "note", item.note);

                if (fields.Count > 0)
                {
                    lineItems.Add(new ExtractedLineItem(fields));
                }
            }

            return new ExtractionResult(headerFields, lineItems);
        }

        private static void AddIfPresent(Dictionary<string, ExtractedField> fields, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                fields[key] = new ExtractedField(key, value.Trim(), AiConfidence, AiSource);
            }
        }

        private sealed record GeminiExtractionDto(
            string? document_number,
            string? document_date,
            string? company_name,
            string? customer_name,
            string? due_date,
            string? description,
            List<GeminiLineItemDto>? line_items);

        private sealed record GeminiLineItemDto(
            string? item_description,
            string? quantity,
            string? unit,
            string? unit_price,
            string? note);
    }
}
