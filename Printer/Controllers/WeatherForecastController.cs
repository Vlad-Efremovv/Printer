using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PdfiumViewer;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;

namespace Printer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

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
            string rootPath = Path.Combine(Directory.GetCurrentDirectory(), file.FileName);

            try
            {
                // ��������� ���� � �������� ����������
                using (var stream = new FileStream(rootPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // ������� PrintDocument ��� ������ PDF
                using (var document = PdfiumViewer.PdfDocument.Load(rootPath))
                {
                    for (int i = 0; i < document.PageCount; i++)
                    {
                        using (var printDocument = new PrintDocument())
                        {
                            printDocument.PrinterSettings.PrinterName = "Samsung SCX-3200 Series";

                            printDocument.PrintPage += (sender, e) =>
                            {
                                e.Graphics.DrawImage(document.Render(i, e.MarginBounds.Width, e.MarginBounds.Height, true), e.MarginBounds);
                            };

                            if (printDocument.PrinterSettings.IsValid)
                            {
                                printDocument.Print();
                                _logger.LogInformation($"�������� ��������� �� ������ �������� {i + 1}.");
                            }
                            else
                            {
                                _logger.LogError("������� �� ������.");
                                return BadRequest("������� �� ������.");
                            }
                        }
                    }
                }

                return Ok("�������� ��������� �� ������.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ������ ���������.");
                return StatusCode(500, $"������ ��� ������: {ex.Message}");
            }
            finally
            {
                // ������ ���� ����� ������
                if (System.IO.File.Exists(rootPath))
                {
                    System.IO.File.Delete(rootPath);
                }
            }
        }
    }
}
