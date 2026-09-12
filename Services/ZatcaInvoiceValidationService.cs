using System.Reflection;
using System.Text;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using ZatcaIntegrationApi.Models;

namespace ZatcaIntegrationApi.Services
{
    public class ZatcaInvoiceValidationService
    {
        public object Validate(InvoiceValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Xml))
                throw new ArgumentException("Invoice XML is required.");

            if (string.IsNullOrWhiteSpace(request.Certificate))
                throw new ArgumentException("Certificate is required.");

            if (string.IsNullOrWhiteSpace(request.Pih))
                throw new ArgumentException("PIH is required.");

            XmlDocument document = new XmlDocument
            {
                PreserveWhitespace = true
            };

            document.LoadXml(request.Xml);

            string certificate =
                DecodeCertificate(request.Certificate);

            EInvoiceValidator validator =
                new EInvoiceValidator();

            ValidationResult validationResult =
                validator.ValidateEInvoice(
                    document,
                    certificate,
                    request.Pih
                );

            if (validationResult == null)
            {
                return new
                {
                    isValid = false,
                    message = "ZATCA SDK returned no validation result."
                };
            }

            var steps =
                validationResult.ValidationSteps
                    .Select(step => new
                    {
                        validationStep =
                            step.ValidationStepName,

                        isValid =
                            step.IsValid,

                        errors =
                            step.ErrorMessages,

                        warnings =
                            step.WarningMessages,

                        diagnostics =
                            GetDiagnostics(step)
                    })
                    .ToList();

            return new
            {
                isValid =
                    validationResult.IsValid,

                validationSteps =
                    steps
            };
        }

        private static Dictionary<string, string> GetDiagnostics(
            object step)
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>();

            if (step == null)
                return result;

            PropertyInfo[] properties =
                step.GetType().GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance
                );

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object? value =
                        property.GetValue(step);

                    if (value == null)
                        continue;

                    if (value is Exception exception)
                    {
                        result[property.Name] =
                            exception.ToString();
                    }
                    else
                    {
                        result[property.Name] =
                            value.ToString() ?? string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    result[property.Name] =
                        ex.Message;
                }
            }

            return result;
        }

        private string DecodeCertificate(
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
