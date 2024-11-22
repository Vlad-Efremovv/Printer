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
                return BadRequest("Файл не получен.");

            // Проверка расширения файла
            if (Path.GetExtension(file.FileName)?.ToLower() != ".pdf")
                return BadRequest("Файл должен быть в формате PDF.");

            // Указываем путь к корневой директории приложения
            string rootPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(file.FileName));

            try
            {
                // Сохраняем файл в корневую директорию
                using (var stream = new FileStream(rootPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string printerName = "Samsung SCX-3200 Series"; // Замените на имя вашего принтера

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
                                _logger.LogCritical("Документ отправлен на печать.");
                                return Ok("Документ отправлен на печать.");
                            }
                            else
                            {
                                _logger.LogCritical("Принтер не найден.");
                                return BadRequest("Принтер не найден.");
                            }
                        }
                    }
                }

                
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при печати: {ex.Message}");
            }
            finally
            {
                //// Убедимся, что файл удаляется после печати
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
                            _logger.LogCritical("Документ отправлен на печать.");
                            //return Ok("Документ отправлен на печать.");
                        }
                        else
                        {
                            _logger.LogCritical("Принтер не найден.");
                           // return BadRequest("Принтер не найден.");
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
