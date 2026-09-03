using StoreApp.Models.Fields;

namespace StoreApp.Tests.Models.Fields
{
    public class DocumentFieldSchemaTests
    {
        [Fact]
        public void All_ContainsHeaderAndLineItemFieldsWithUniqueKeys()
        {
            Assert.Equal(DocumentFieldSchema.HeaderFields.Count + DocumentFieldSchema.LineItemFields.Count, DocumentFieldSchema.All.Count);
            Assert.Equal(DocumentFieldSchema.All.Count, DocumentFieldSchema.All.Select(f => f.Key).Distinct().Count());
        }

        [Fact]
        public void HeaderFields_AreAllHeaderScoped()
        {
            Assert.All(DocumentFieldSchema.HeaderFields, field => Assert.Equal(DocumentFieldScope.Header, field.Scope));
        }

        [Fact]
        public void LineItemFields_AreAllLineItemScoped()
        {
            Assert.All(DocumentFieldSchema.LineItemFields, field => Assert.Equal(DocumentFieldScope.LineItem, field.Scope));
        }

        [Fact]
        public void DocumentNumber_IsRequiredHeaderField()
        {
            var field = Assert.Single(DocumentFieldSchema.HeaderFields, f => f.Key == "document_number");
            Assert.True(field.IsRequired);
            Assert.Equal(DocumentFieldDataType.Text, field.DataType);
        }
    }
}
