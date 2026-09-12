using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using ZatcaIntegrationApi.Models;

namespace ZatcaIntegrationApi.Services
{
    public class ZatcaInvoiceProcessingService
    {
        public object Process(InvoiceProcessingRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Xml))
                throw new ArgumentException("Invoice XML is required.");

            if (string.IsNullOrWhiteSpace(request.PrivateKey))
                throw new ArgumentException("Private key is required.");

            if (string.IsNullOrWhiteSpace(request.Certificate))
                throw new ArgumentException("Certificate is required.");

            if (string.IsNullOrWhiteSpace(request.Pih))
                throw new ArgumentException("PIH is required.");

            XmlDocument document = new XmlDocument
            {
                PreserveWhitespace = true
            };

            document.LoadXml(request.Xml);

            document = PrettyXml(document);

            string certificateContent =
                DecodeCertificate(request.Certificate);

            bool isSimplified =
                IsSimplifiedInvoice(document);

            if (isSimplified)
            {
                SignResult signResult =
                    new EInvoiceSigner().SignDocument(
                        document,
                        certificateContent,
                        request.PrivateKey
                    );

                if (signResult == null)
                    throw new InvalidOperationException(
                        "ZATCA SDK returned null SignResult.");

                if (signResult.SignedEInvoice == null)
                {
                    string errorMessage =
                        signResult.ErrorMessage ??
                        "Unknown signing error.";

                    throw new InvalidOperationException(
                        $"ZATCA SDK signing failed. Error: {errorMessage}");
                }

                RequestResult requestResult =
                    new RequestGenerator().GenerateRequest(
                        signResult.SignedEInvoice
                    );

                if (requestResult == null)
                    throw new InvalidOperationException(
                        "ZATCA SDK returned null RequestResult.");

                if (requestResult.InvoiceRequest == null)
                {
                    string debugInfo =
                        GetRequestResultDebugInfo(
                            requestResult);

                    throw new InvalidOperationException(
                        $"ZATCA SDK returned an empty InvoiceRequest for the simplified invoice. RequestResult: {debugInfo}");
                }

                return new
                {
                    isProcessed = true,
                    invoiceType = "Simplified",
                    invoiceHash =
                        requestResult.InvoiceRequest.InvoiceHash,
                    uuid =
                        requestResult.InvoiceRequest.Uuid,
                    invoice =
                        requestResult.InvoiceRequest.Invoice
                };
            }

            HashResult hashResult =
                new EInvoiceHashGenerator()
                    .GenerateEInvoiceHashing(document);

            if (hashResult == null)
                throw new InvalidOperationException(
                    "ZATCA SDK returned null HashResult.");

            if (string.IsNullOrWhiteSpace(hashResult.Hash))
            {
                string sdkError =
                    hashResult.ErrorMessage ??
                    "Unknown hashing error.";

                string exceptionType =
                    hashResult.Exception?
                        .GetType()
                        .FullName ?? string.Empty;

                string exceptionMessage =
                    hashResult.Exception?
                        .Message ?? string.Empty;

                string innerExceptionType =
                    hashResult.Exception?
                        .InnerException?
                        .GetType()
                        .FullName ?? string.Empty;

                string innerExceptionMessage =
                    hashResult.Exception?
                        .InnerException?
                        .Message ?? string.Empty;

                string stackTrace =
                    hashResult.Exception?
                        .StackTrace ?? string.Empty;

                throw new InvalidOperationException(
                    $"ZATCA SDK hashing failed. " +
                    $"SDK Error: {sdkError} | " +
                    $"Exception Type: {exceptionType} | " +
                    $"Exception: {exceptionMessage} | " +
                    $"Inner Exception Type: {innerExceptionType} | " +
                    $"InnerException: {innerExceptionMessage} | " +
                    $"StackTrace: {stackTrace}");
            }

            RequestResult standardRequestResult =
                new RequestGenerator()
                    .GenerateRequest(document);

            if (standardRequestResult == null)
                throw new InvalidOperationException(
                    "ZATCA SDK returned null RequestResult.");

            if (standardRequestResult.InvoiceRequest == null)
            {
                string debugInfo =
                    GetRequestResultDebugInfo(
                        standardRequestResult);

                throw new InvalidOperationException(
                    $"ZATCA SDK returned an empty InvoiceRequest after the invoice hash was generated successfully. RequestResult: {debugInfo}");
            }

            standardRequestResult.InvoiceRequest.InvoiceHash =
                hashResult.Hash;

            return new
            {
                isProcessed = true,
                invoiceType = "Standard",
                invoiceHash =
                    standardRequestResult.InvoiceRequest.InvoiceHash,
                uuid =
                    standardRequestResult.InvoiceRequest.Uuid,
                invoice =
                    standardRequestResult.InvoiceRequest.Invoice
            };
        }

        private static string GetRequestResultDebugInfo(
            RequestResult requestResult)
        {
            var parts = new List<string>();

            parts.Add(
                $"IsValid={requestResult.IsValid}");

            if (requestResult.ErrorMessages != null)
            {
                int errorIndex = 1;

                foreach (var error in requestResult.ErrorMessages)
                {
                    parts.Add(
                        $"Error[{errorIndex}]={error}");

                    errorIndex++;
                }
            }

            if (requestResult.Steps != null)
            {
                int stepIndex = 1;

                foreach (var step in requestResult.Steps)
                {
                    var stepValues =
                        new List<string>();

                    var stepProperties =
                        step.GetType()
                            .GetProperties();

                    foreach (var property in stepProperties)
                    {
                        try
                        {
                            object? value =
                                property.GetValue(step);

                            if (value == null)
                            {
                                stepValues.Add(
                                    $"{property.Name}=null");

                                continue;
                            }

                            if (value is string stringValue)
                            {
                                stepValues.Add(
                                    $"{property.Name}={stringValue}");

                                continue;
                            }

                            if (value is Exception exception)
                            {
                                stepValues.Add(
                                    $"{property.Name}.Type={exception.GetType().FullName}");

                                stepValues.Add(
                                    $"{property.Name}.Message={exception.Message}");

                                stepValues.Add(
                                    $"{property.Name}.InnerException={exception.InnerException?.Message ?? string.Empty}");

                                stepValues.Add(
                                    $"{property.Name}.StackTrace={exception.StackTrace ?? string.Empty}");

                                continue;
                            }

                            if (value is IEnumerable enumerable)
                            {
                                var items =
                                    new List<string>();

                                foreach (var item in enumerable)
                                {
                                    if (item == null)
                                    {
                                        items.Add("null");
                                        continue;
                                    }

                                    if (item is Exception itemException)
                                    {
                                        items.Add(
                                            $"ExceptionType={itemException.GetType().FullName}; " +
                                            $"Message={itemException.Message}; " +
                                            $"InnerException={itemException.InnerException?.Message ?? string.Empty}");

                                        continue;
                                    }

                                    items.Add(
                                        item.ToString() ??
                                        "null");
                                }

                                stepValues.Add(
                                    $"{property.Name}=[{string.Join(", ", items)}]");

                                continue;
                            }

                            stepValues.Add(
                                $"{property.Name}={value}");
                        }
                        catch (Exception ex)
                        {
                            stepValues.Add(
                                $"{property.Name}=Unable to read: {ex.Message}");
                        }
                    }

                    parts.Add(
                        $"Step[{stepIndex}]: {string.Join("; ", stepValues)}");

                    stepIndex++;
                }
            }

            parts.Add(
                $"InvoiceRequest={(requestResult.InvoiceRequest == null ? "null" : "not-null")}");

            return string.Join(
                " | ",
                parts);
        }

        private static bool IsSimplifiedInvoice(
            XmlDocument document)
        {
            XmlNamespaceManager namespaceManager =
                new XmlNamespaceManager(
                    document.NameTable);

            namespaceManager.AddNamespace(
                "cbc",
                "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"
            );

            XmlNode? invoiceTypeCode =
                document.SelectSingleNode(
                    "//cbc:InvoiceTypeCode",
                    namespaceManager
                );

            if (invoiceTypeCode == null)
                throw new ArgumentException(
                    "InvoiceTypeCode was not found in the invoice XML.");

            XmlAttribute? nameAttribute =
                invoiceTypeCode.Attributes?["name"];

            if (nameAttribute == null ||
                string.IsNullOrWhiteSpace(
                    nameAttribute.Value))
                throw new ArgumentException(
                    "InvoiceTypeCode name attribute is missing.");

            return nameAttribute.Value
                .StartsWith("02");
        }

        private static string DecodeCertificate(
            string certificate)
        {
            try
            {
                byte[] certificateBytes =
                    Convert.FromBase64String(
                        certificate);

                return Encoding.UTF8.GetString(
                    certificateBytes);
            }
            catch (FormatException)
            {
                return certificate;
            }
        }

        private static XmlDocument PrettyXml(
            XmlDocument inputXml)
        {
            XmlDocument formattedXml =
                new XmlDocument
                {
                    PreserveWhitespace = true
                };

            using MemoryStream memoryStream =
                new MemoryStream();

            using (StreamWriter streamWriter =
                   new StreamWriter(
                       memoryStream,
                       new UTF8Encoding(false),
                       1024,
                       true))
            {
                XmlWriterSettings settings =
                    new XmlWriterSettings
                    {
                        Indent = true,
                        IndentChars = "    ",
                        OmitXmlDeclaration = false,
                        Encoding = Encoding.UTF8
                    };

                using XmlWriter xmlWriter =
                    XmlWriter.Create(
                        streamWriter,
                        settings);

                inputXml.Save(xmlWriter);
            }

            string xml =
                Encoding.UTF8
                    .GetString(
                        memoryStream.ToArray())
                    .Trim();

            formattedXml.LoadXml(xml);

            return formattedXml;
        }
    }
}