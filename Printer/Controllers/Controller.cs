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

        //локальный
        //[HttpPost]
        //[Route("printpdf")]
        //public async Task<IActionResult> PrintPdfAsync(IFormFile file)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("Файл не получен.");

        //    // Проверка расширения файла
        //    if (Path.GetExtension(file.FileName)?.ToLower() != ".pdf")
        //        return BadRequest("Файл должен быть в формате PDF.");

        //    // Указываем путь к корневой директории приложения
        //    string rootPath = Path.Combine(Directory.GetCurrentDirectory(), file.FileName);

        //    try
        //    {
        //        // Сохраняем файл в корневую директорию
        //        using (var stream = new FileStream(rootPath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }

        //        // Создаем PrintDocument для печати PDF
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
        //                        _logger.LogInformation($"Документ отправлен на печать страницу {i + 1}.");
        //                    }
        //                    else
        //                    {
        //                        _logger.LogError("Принтер не найден.");
        //                        return BadRequest("Принтер не найден.");
        //                    }
        //                }
        //            }
        //        }

        //        return Ok("Документ отправлен на печать.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Ошибка при печати документа.");
        //        return StatusCode(500, $"Ошибка при печати: {ex.Message}");
        //    }
        //    finally
        //    {
        //        // Удалим файл после печати
        //        if (System.IO.File.Exists(rootPath))
        //        {
        //            System.IO.File.Delete(rootPath);
        //        }
        //    }
        //}

        // по порту
        [HttpPost]
        [Route("printpdf")]
        public async Task<IActionResult> PrintPdfAsync([FromForm] PrintRequestDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Файл не получен.");

            // Проверка расширения файла
            if (Path.GetExtension(request.File.FileName)?.ToLower() != ".pdf")
                return BadRequest("Файл должен быть в формате PDF.");

            // Указываем путь к временной директории
            string tempPath = Path.Combine(Path.GetTempPath(), request.File.FileName);

            try
            {
                // Сохраняем файл во временной директории
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                // Отправляем файл на принтер по TCP/IP (например, через LPR)
                using (var client = new TcpClient(request.PrinterIp, 9100)) // Порт 9100 - стандартный для печати
                using (var networkStream = client.GetStream())
                {
                    using (var fileStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
                    {
                        await fileStream.CopyToAsync(networkStream);
                    }
                }

                _logger.LogInformation("Документ отправлен на печать.");
                return Ok("Документ успешно отправлен на печать.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при печати документа.");
                return StatusCode(500, $"Ошибка при печати: {ex.Message}");
            }
            finally
            {
                // Удалим временный файл после попытки печати
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }
}
