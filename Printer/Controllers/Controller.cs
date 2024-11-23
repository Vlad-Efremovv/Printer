using Microsoft.AspNetCore.Mvc;
using PdfiumViewer;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Net.Sockets;
using System.Text;



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

        [HttpGet]
        [Route("getPrinter")]
        public async Task<IActionResult> PrintPdfAsync() => Ok(PrinterSettings.InstalledPrinters);


        [HttpPost]
        [Route("printPDF")]
        public async Task<IActionResult> PrintPdfAsyncss(IFormFile file, string printerName = "T80 (копия 1)")
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл отсутствует");

            if (Path.GetExtension(file.FileName)?.ToLower() != ".pdf")
                return BadRequest("Файл не формата PDF");

            // полный путь к файлу
            string rootPath = Path.Combine(Directory.GetCurrentDirectory(), file.FileName);

            try
            {
                // Сохранение файла во временное хранилище (Папка temp)
                using (var stream = new FileStream(rootPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // загружаеи локальное имя принтера
                using (var document = PdfiumViewer.PdfDocument.Load(rootPath))
                {
                    for (int i = 0; i < document.PageCount; i++)
                    {
                        using (var printDocument = new PrintDocument())
                        {
                            // устанавливаем локальное имя принтера в сети
                            printDocument.PrinterSettings.PrinterName = printerName;

                            // Устанавливаем размер бумаги (ширина 8 см = 80 мм)
                            // 8 см по ширине, 12 см по высоте
                            printDocument.DefaultPageSettings.PaperSize = new PaperSize("Custom", 80 * 10, 80 * 15);

                            // Устанавливаем ориентацию страницы на книжную (портрет)
                            printDocument.DefaultPageSettings.Landscape = false; // false - портретная ориентация


                            // Устанавливаем все поля на ноль
                            printDocument.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);


                            printDocument.PrintPage += (sender, e) =>
                            {
                                // Масштабируем изображение
                                //float scaleFactor = 0.66f; // Масштаб 66%
                                float scaleFactor = 1.5f; // Масштаб 66%

                                // Рассчитываем новые размеры для изображения с учетом масштаба
                                int scaledWidth = (int)(e.MarginBounds.Width * scaleFactor);
                                int scaledHeight = (int)(e.MarginBounds.Height * scaleFactor);

                                // Рендерим страницу PDF в графику с применением масштаба
                                e.Graphics.DrawImage(document.Render(i, scaledWidth, scaledHeight, true), e.MarginBounds);
                            };

                            if (printDocument.PrinterSettings.IsValid)
                            {
                                // Печать файла если принтер доступен
                                printDocument.Print();
                                _logger.LogInformation($"Расспечатано страниц: {i + 1}.");
                            }
                            else
                            {
                                _logger.LogError("Ошибка печати, изза проблемы с принтером");
                                return BadRequest("Ошибка печати, изза проблемы с принтером");
                            }
                        }
                    }
                }

                return Ok("Печать прошла успешно");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Произошла ошибка: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
            finally
            {
                // удаляем временный файл
                if (System.IO.File.Exists(rootPath))
                {
                    System.IO.File.Delete(rootPath);
                }
            }
        }




    }

}