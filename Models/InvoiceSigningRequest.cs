namespace ZatcaIntegrationApi.Models
{
    public class InvoiceSigningRequest
    {
        public string Xml { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public string Certificate { get; set; } = string.Empty;
    }
}