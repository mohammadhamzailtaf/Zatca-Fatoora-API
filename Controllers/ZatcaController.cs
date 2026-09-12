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

        public ZatcaController(
            ZatcaCertificateService certificateService,
            ZatcaInvoiceValidationService validationService,
            ZatcaInvoiceProcessingService processingService)
        {
            _certificateService = certificateService;
            _validationService = validationService;
            _processingService = processingService;
        }

        [HttpPost("generate-csr")]
        public IActionResult GenerateCsr([FromBody] CsrRequest request)
        {
            try
            {
                var result = _certificateService.GenerateCsr(request);
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
                var result = _validationService.Validate(request);
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
                var result = _processingService.Process(request);
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isProcessed = false,
                    message = ex.Message
                });
            }
        }
    }
}