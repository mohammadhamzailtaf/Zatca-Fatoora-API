using System.Text;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using ZatcaIntegrationApi.Models;

namespace ZatcaIntegrationApi.Services
{
    public class ZatcaInvoiceSigningService
    {
        public object Sign(InvoiceSigningRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Xml))
                throw new ArgumentException("Invoice XML is required.");

            if (string.IsNullOrWhiteSpace(request.PrivateKey))
                throw new ArgumentException("Private Key is required.");

            if (string.IsNullOrWhiteSpace(request.Certificate))
                throw new ArgumentException("Certificate is required.");

            XmlDocument document = new XmlDocument
            {
                PreserveWhitespace = true
            };

            document.LoadXml(request.Xml);

            string certificate =
                DecodeCertificate(request.Certificate);

            EInvoiceSigner signer =
                new EInvoiceSigner();

            SignResult signResult =
                signer.SignDocument(
                    document,
                    certificate,
                    request.PrivateKey
                );

            if (signResult == null)
            {
                return new
                {
                    isSigned = false,
                    message = "ZATCA SDK returned no signing result."
                };
            }

            if (!signResult.IsValid)
            {
                return new
                {
                    isSigned = false,
                    message = "ZATCA SDK could not sign the invoice."
                };
            }

            if (signResult.SignedEInvoice == null)
            {
                return new
                {
                    isSigned = false,
                    message = "ZATCA SDK did not return a signed invoice."
                };
            }

            string signedXml =
                signResult.SignedEInvoice.OuterXml;

            string signedInvoiceBase64 =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        signedXml
                    )
                );

            return new
            {
                isSigned = true,
                signedXml = signedXml,
                signedInvoiceBase64 = signedInvoiceBase64
            };
        }

        private static string DecodeCertificate(
            string certificate)
        {
            if (string.IsNullOrWhiteSpace(certificate))
                return certificate;

            try
            {
                byte[] certificateBytes =
                    Convert.FromBase64String(
                        certificate
                    );

                string decodedCertificate =
                    Encoding.UTF8.GetString(
                        certificateBytes
                    );

                if (!string.IsNullOrWhiteSpace(
                    decodedCertificate))
                {
                    return decodedCertificate;
                }
            }
            catch
            {
            }

            return certificate;
        }
    }
}