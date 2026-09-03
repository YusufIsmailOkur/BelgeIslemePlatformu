using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using StoreApp.Services.Abstractions;
using StoreApp.Services.Extraction;
using StoreApp.Tests.Fakes;

namespace StoreApp.Tests.Services.Extraction
{
    public class GeminiFieldExtractorTests
    {
        private const string SuccessResponseJson = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  { "text": "{\"document_number\":\"F-2026-01\",\"document_date\":null,\"company_name\":\"Örnek A.Ş.\",\"customer_name\":null,\"due_date\":null,\"description\":null,\"line_items\":[{\"item_description\":\"Kablo\",\"quantity\":\"100\",\"unit\":\"metre\",\"unit_price\":\"12.50\",\"note\":null}]}" }
                ],
                "role": "model"
              }
            }
          ]
        }
        """;

        private static GeminiFieldExtractor CreateExtractor(
            FakeHttpMessageHandler handler, bool enabled = true, string apiKey = "test-key")
        {
            var httpClient = new HttpClient(handler);
            var options = Options.Create(new GeminiOptions { Enabled = enabled, ApiKey = apiKey, Model = "gemini-2.5-flash" });
            return new GeminiFieldExtractor(httpClient, options, new CapturingLogger<GeminiFieldExtractor>());
        }

        [Fact]
        public async Task ExtractAsync_WhenDisabled_ReturnsNullWithoutHttpCall()
        {
            var handler = new FakeHttpMessageHandler();
            var extractor = CreateExtractor(handler, enabled: false);

            var result = await extractor.ExtractAsync("Fatura No: 1", Array.Empty<ParsedTable>());

            Assert.Null(result);
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public async Task ExtractAsync_WhenApiKeyMissing_ReturnsNullWithoutHttpCall()
        {
            var handler = new FakeHttpMessageHandler();
            var extractor = CreateExtractor(handler, enabled: true, apiKey: "");

            var result = await extractor.ExtractAsync("Fatura No: 1", Array.Empty<ParsedTable>());

            Assert.Null(result);
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public async Task ExtractAsync_NoContentToSend_ReturnsNullWithoutHttpCall()
        {
            var handler = new FakeHttpMessageHandler();
            var extractor = CreateExtractor(handler);

            var result = await extractor.ExtractAsync(null, Array.Empty<ParsedTable>());

            Assert.Null(result);
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public async Task ExtractAsync_SuccessfulResponse_ParsesHeaderFieldsAndLineItems()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SuccessResponseJson, Encoding.UTF8, "application/json")
            });
            var extractor = CreateExtractor(handler);

            var result = await extractor.ExtractAsync("Fatura No: F-2026-01\nFirma: Örnek A.Ş.", Array.Empty<ParsedTable>());

            Assert.NotNull(result);
            Assert.Equal("F-2026-01", result!.HeaderFields["document_number"].Value);
            Assert.Equal("AI", result.HeaderFields["document_number"].Source);
            Assert.Equal("Örnek A.Ş.", result.HeaderFields["company_name"].Value);
            Assert.False(result.HeaderFields.ContainsKey("document_date"));

            var lineItem = Assert.Single(result.LineItems);
            Assert.Equal("Kablo", lineItem.Fields["item_description"].Value);
            Assert.Equal("100", lineItem.Fields["quantity"].Value);

            Assert.Equal(1, handler.CallCount);
            Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("x-goog-api-key").Single());
            Assert.Contains("gemini-2.5-flash", handler.LastRequest.RequestUri!.ToString());
        }

        [Fact]
        public async Task ExtractAsync_HttpErrorStatus_ReturnsNullGracefully()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var extractor = CreateExtractor(handler);

            var result = await extractor.ExtractAsync("Fatura No: 1", Array.Empty<ParsedTable>());

            Assert.Null(result);
        }

        [Fact]
        public async Task ExtractAsync_NetworkException_ReturnsNullGracefully()
        {
            var handler = new FakeHttpMessageHandler(exception: new HttpRequestException("bağlantı hatası"));
            var extractor = CreateExtractor(handler);

            var result = await extractor.ExtractAsync("Fatura No: 1", Array.Empty<ParsedTable>());

            Assert.Null(result);
        }
    }
}
