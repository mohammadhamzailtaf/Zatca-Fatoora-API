using Microsoft.AspNetCore.Mvc;
using ZatcaIntegrationApi.Models;
using ZatcaIntegrationApi.Services;

namespace ZatcaIntegrationApi.Controllers
{
    [ApiController]
    [Route("api/zatca")]
    public class ZatcaController : ControllerBase
    {
        private readonly ZatcaCertificateService _certificateService;
        private readonly ZatcaInvoiceValidationService _validationService;
        private readonly ZatcaInvoiceProcessingService _processingService;
        private readonly ZatcaInvoiceSigningService _signingService;

        public ZatcaController(
            ZatcaCertificateService certificateService,
            ZatcaInvoiceValidationService validationService,
            ZatcaInvoiceProcessingService processingService,
            ZatcaInvoiceSigningService signingService)
        {
            _certificateService = certificateService;
            _validationService = validationService;
            _processingService = processingService;
            _signingService = signingService;
        }

        [HttpPost("generate-csr")]
        public IActionResult GenerateCsr(
            [FromBody] CsrRequest request)
        {
            try
            {
                var result =
                    _certificateService.GenerateCsr(request);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    isValid = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isValid = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("validate-invoice")]
        public IActionResult ValidateInvoice(
            [FromBody] InvoiceValidationRequest request)
        {
            try
            {
                var result =
                    _validationService.Validate(request);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    isValid = false,
                    message = ex.Message
                });
            }
            catch (System.Xml.XmlException ex)
            {
                return BadRequest(new
                {
                    isValid = false,
                    message = "Invalid invoice XML.",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isValid = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("process-invoice")]
        public IActionResult ProcessInvoice(
            [FromBody] InvoiceProcessingRequest request)
        {
            try
            {
                var result =
                    _processingService.Process(request);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    isProcessed = false,
                    message = ex.Message
                });
            }
            catch (System.Xml.XmlException ex)
            {
                return BadRequest(new
                {
                    isProcessed = false,
                    message = "Invalid invoice XML.",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isProcessed = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("sign-invoice")]
        public IActionResult SignInvoice(
            [FromBody] InvoiceSigningRequest request)
        {
            try
            {
                var result =
                    _signingService.Sign(request);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    isSigned = false,
                    message = ex.Message
                });
            }
            catch (System.Xml.XmlException ex)
            {
                return BadRequest(new
                {
                    isSigned = false,
                    message = "Invalid invoice XML.",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isSigned = false,
                    message = ex.Message
                });
            }
        }
    }
}