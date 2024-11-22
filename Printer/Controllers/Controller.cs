using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PdfiumViewer;
using SnmpSharpNet;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Printer.Controllers
{
    [ApiController]
    public class Controller : ControllerBase
    {
        private readonly ILogger<Controller> _logger;

        public Controller(ILogger<Controller> logger)
        {
            _logger = logger;
        }

        public class PrintRequestDto
        {
            public IFormFile File { get; set; }
            public string PrinterIp { get; set; }
        }

        //���������
        //[HttpPost]
        //[Route("printpdf")]
        //public async Task<IActionResult> PrintPdfAsync(IFormFile file)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("���� �� �������.");

        //    // �������� ���������� �����
        //    if (Path.GetExtension(file.FileName)?.ToLower() != ".pdf")
        //        return BadRequest("���� ������ ���� � ������� PDF.");

        //    // ��������� ���� � �������� ���������� ����������
        //    string rootPath = Path.Combine(Directory.GetCurrentDirectory(), file.FileName);

        //    try
        //    {
        //        // ��������� ���� � �������� ����������
        //        using (var stream = new FileStream(rootPath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }

        //        // ������� PrintDocument ��� ������ PDF
        //        using (var document = PdfiumViewer.PdfDocument.Load(rootPath))
        //        {
        //            for (int i = 0; i < document.PageCount; i++)
        //            {
        //                using (var printDocument = new PrintDocument())
        //                {
        //                    printDocument.PrinterSettings.PrinterName = "Samsung SCX-3200 Series";

        //                    printDocument.PrintPage += (sender, e) =>
        //                    {
        //                        e.Graphics.DrawImage(document.Render(i, e.MarginBounds.Width, e.MarginBounds.Height, true), e.MarginBounds);
        //                    };

        //                    if (printDocument.PrinterSettings.IsValid)
        //                    {
        //                        printDocument.Print();
        //                        _logger.LogInformation($"�������� ��������� �� ������ �������� {i + 1}.");
        //                    }
        //                    else
        //                    {
        //                        _logger.LogError("������� �� ������.");
        //                        return BadRequest("������� �� ������.");
        //                    }
        //                }
        //            }
        //        }

        //        return Ok("�������� ��������� �� ������.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "������ ��� ������ ���������.");
        //        return StatusCode(500, $"������ ��� ������: {ex.Message}");
        //    }
        //    finally
        //    {
        //        // ������ ���� ����� ������
        //        if (System.IO.File.Exists(rootPath))
        //        {
        //            System.IO.File.Delete(rootPath);
        //        }
        //    }
        //}

        // �� �����
        [HttpPost]
        [Route("printpdf")]
        public async Task<IActionResult> PrintPdfAsync([FromForm] PrintRequestDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("���� �� �������.");

            // �������� ���������� �����
            if (Path.GetExtension(request.File.FileName)?.ToLower() != ".pdf")
                return BadRequest("���� ������ ���� � ������� PDF.");

            // ��������� ���� � ��������� ����������
            string tempPath = Path.Combine(Path.GetTempPath(), request.File.FileName);

            try
            {
                // ��������� ���� �� ��������� ����������
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                // ���������� ���� �� ������� �� TCP/IP (��������, ����� LPR)
                using (var client = new TcpClient(request.PrinterIp, 9100)) // ���� 9100 - ����������� ��� ������
                using (var networkStream = client.GetStream())
                {
                    using (var fileStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
                    {
                        await fileStream.CopyToAsync(networkStream);
                    }
                }

                _logger.LogInformation("�������� ��������� �� ������.");
                return Ok("�������� ������� ��������� �� ������.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ������ ���������.");
                return StatusCode(500, $"������ ��� ������: {ex.Message}");
            }
            finally
            {
                // ������ ��������� ���� ����� ������� ������
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }
}
