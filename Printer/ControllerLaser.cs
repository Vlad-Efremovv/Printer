using Microsoft.AspNetCore.Mvc;
using PdfiumViewer;
using System.Net.Sockets;
using System.IO;

namespace Printer
{
    [ApiController]
    public class ControllerLaser : ControllerBase
    {
        private readonly ILogger<ControllerLaser> _logger;

        public ControllerLaser(ILogger<ControllerLaser> logger)
        {
            _logger = logger;
        }

        public class PrintRequestDto
        {
            public IFormFile File { get; set; }
            public string PrinterIp { get; set; }
        }

        [HttpPost]
        [Route("printLaser")]
        public async Task<IActionResult> PrintPDFAsync(PrintRequestDto printRequest)
        {
            if (printRequest.File == null || printRequest.File.Length == 0)
                return BadRequest("Файл отсутствует");

            string tempPath = Path.Combine(Path.GetTempPath(), printRequest.File.FileName);

            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await printRequest.File.CopyToAsync(stream);
                }

                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(printRequest.PrinterIp, 9100);

                    using (var networkStream = client.GetStream())
                    {
                        byte[] pdfHeader = new byte[] { 0x25, 0x50, 0x44, 0x46 };
                        networkStream.Write(pdfHeader, 0, pdfHeader.Length);

                        using (var fileStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
                        {
                            await fileStream.CopyToAsync(networkStream);
                        }
                    }
                }

                Console.WriteLine("Файл успешно отправлен на принтер.");
                return Ok("PDF успешно отправлен на печать");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при печати: {ex.Message}");
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }
}
