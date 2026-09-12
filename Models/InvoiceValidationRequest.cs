namespace ZatcaIntegrationApi.Models
{
    public class InvoiceValidationRequest
    {
        public string Xml { get; set; } = string.Empty;
        public string Certificate { get; set; } = string.Empty;
        public string Pih { get; set; } = string.Empty;
    }
}