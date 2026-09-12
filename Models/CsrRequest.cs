namespace ZatcaIntegrationApi.Models
{
    public class CsrRequest
    {
        public string CommonName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string OrganizationIdentifier { get; set; } = string.Empty;
        public string OrganizationUnitName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public string InvoiceType { get; set; } = "1100";
        public string LocationAddress { get; set; } = string.Empty;
        public string IndustryBusinessCategory { get; set; } = string.Empty;
    }
}