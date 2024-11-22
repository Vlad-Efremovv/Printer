using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Printing;

namespace Printer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        [HttpPost]
        [Route("printpdf")]
        public async Task<IActionResult> PrintPdfAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("���� �� �������.");

            // �������� ���������� �����
            if (Path.GetExtension(file.FileName)?.ToLower() != ".pdf")
                return BadRequest("���� ������ ���� � ������� PDF.");

            // ��������� ���� � �������� ���������� ����������
            string rootPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(file.FileName));

            try
            {
                // ��������� ���� � �������� ����������
                using (var stream = new FileStream(rootPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string printerName = "Samsung SCX-3200 Series"; // �������� �� ��� ������ ��������

                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    Console.WriteLine(printer);
                    using (var document = PdfDocument.Load(rootPath))
                    {
                        using (var printDocument = document.CreatePrintDocument())
                        {
                            printDocument.PrinterSettings.PrinterName = printer;

                            if (printDocument.PrinterSettings.IsValid)
                            {
                                printDocument.Print();
                                _logger.LogCritical("�������� ��������� �� ������.");
                                return Ok("�������� ��������� �� ������.");
                            }
                            else
                            {
                                _logger.LogCritical("������� �� ������.");
                                return BadRequest("������� �� ������.");
                            }
                        }
                    }
                }

                
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"������ ��� ������: {ex.Message}");
            }
            finally
            {
                //// ��������, ��� ���� ��������� ����� ������
                //await Task.Delay(10000);

                //if (System.IO.File.Exists(rootPath))
                //{
                //    System.IO.File.Delete(rootPath);
                //}

                using (var document = PdfDocument.Load(rootPath))
                {
                    using (var printDocument = document.CreatePrintDocument())
                    {
                        printDocument.PrinterSettings.PrinterName = "Samsung SCX-3200 Series";

                        if (printDocument.PrinterSettings.IsValid)
                        {
                            printDocument.Print();
                            _logger.LogCritical("�������� ��������� �� ������.");
                            //return Ok("�������� ��������� �� ������.");
                        }
                        else
                        {
                            _logger.LogCritical("������� �� ������.");
                           // return BadRequest("������� �� ������.");
                        }
                    }
                }
            }
            return Ok();
        }

       private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }
    }
}
