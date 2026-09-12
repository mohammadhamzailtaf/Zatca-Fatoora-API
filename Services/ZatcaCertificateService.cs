using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using ZatcaIntegrationApi.Models;

namespace ZatcaIntegrationApi.Services
{
    public class ZatcaCertificateService
    {
        private static readonly HashSet<string> SupportedInvoiceTypes = new()
        {
            "1000",
            "0100",
            "1100"
        };

        public object GenerateCsr(CsrRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.InvoiceType))
            {
                request.InvoiceType = "1100";
            }

            if (!SupportedInvoiceTypes.Contains(request.InvoiceType))
            {
                throw new ArgumentException(
                    "InvoiceType must be 1000, 0100, or 1100."
                );
            }

            CsrGenerationDto csrGenerationDto = new(
                request.CommonName,
                request.SerialNumber,
                request.OrganizationIdentifier,
                request.OrganizationUnitName,
                request.OrganizationName,
                request.CountryName,
                request.InvoiceType,
                request.LocationAddress,
                request.IndustryBusinessCategory
            );

            CsrGenerator csrGenerator = new();

            CsrResult csrResult = csrGenerator.GenerateCsr(
                csrGenerationDto,
                EnvironmentType.NonProduction,
                false
            );

            return new
            {
                isValid = csrResult.IsValid,
                csr = csrResult.Csr,
                privateKey = csrResult.PrivateKey,
                invoiceType = request.InvoiceType
            };
        }
    }
}