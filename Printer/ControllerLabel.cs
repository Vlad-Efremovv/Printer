using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using PdfiumViewer;
using System.Net.Sockets;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Printer
{
    [ApiController]
    public class ControllerLabel : ControllerBase
    {
        private readonly ILogger<ControllerСheque> _logger;

        public ControllerLabel(ILogger<ControllerСheque> logger)
        {
            _logger = logger;
        }

        public class PrintRequestDto
        {
            public IFormFile File { get; set; }
            public string PrinterIp { get; set; }
        }

        [HttpPost]
        [Route("printPDF")]
        public async Task<IActionResult> PrintPDFAsync(IFormFile file, string printerIp, int port = 9100)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл отсутствует");

            string tempPath = Path.Combine(Path.GetTempPath(), file.FileName);

            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                List<string> outputImagePaths = renderPdfToFile(tempPath, 300);

                foreach (var outputImagePath in outputImagePaths)
                {
                    using (Bitmap image = new Bitmap(outputImagePath))
                    {
                        byte[] printData = ConvertImageForGodex(image);
                        await SendDataToPrinter(printerIp, port, printData);

                        Console.WriteLine("Изображение отправлено на принтер.");
                    }
                }

                return Ok("PDF успешно обработан и отправлен на печать");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при обработке файла: {ex.Message}");
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }

        private List<string> renderPdfToFile(string pdfFilename, int dpi)
        {
            var outputPaths = new List<string>();
            try
            {
                using (var doc = PdfDocument.Load(pdfFilename))
                {
                    Console.WriteLine("Загрузка PDF-документа из файла...");
                    for (int page = 0; page < doc.PageCount; page++)
                    {
                        using (var img = doc.Render(page, dpi, dpi, false))
                        {
                            // Формируем путь для сохранения каждого изображения
                            string outputFilePath = Path.Combine(Path.GetTempPath(), $"page_{page}.png");
                            img.Save(outputFilePath, ImageFormat.Png);
                            Console.WriteLine($"Файл сохранен: {outputFilePath}");
                            outputPaths.Add(outputFilePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при рендеринге PDF: {ex.Message}");
            }
            return outputPaths;
        }

        //private byte[] ConvertImageForGodex(Bitmap bitmap)
        //{
        //    // Разрешение для печати
        //    const int dpi = 203; // dpi для Godex ZX430i (проверьте в спецификациях принтера)

        //    // Преобразовать изображение в формат, поддерживаемый принтером
        //    using (var ms = new MemoryStream())
        //    {
        //        // Преобразование в 1bpp (черно-белый) изображение
        //        using (var monochromeBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format1bppIndexed))
        //        {
        //            using (Graphics g = Graphics.FromImage(monochromeBitmap))
        //            {
        //                // Рисуем черно-белое изображение
        //                g.Clear(Color.White); // заполняем белым фоном
        //                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        //            }

        //            // Сохранение в поток
        //            monochromeBitmap.Save(ms, ImageFormat.Bmp);
        //        }

        //        // Здесь заготовка для команд принтера
        //        // Добавление ваших команд для Godex начинающих и завершающих печать
        //        // Например, нужно будет удостовериться, что направляете свое изображение в формате (BMP или другой поддерживаемый формат)

        //        // В конце возврат данных изображения
        //        return ms.ToArray();
        //    }
        //}

        private byte[] ConvertImageForGodex(Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                // Сохранение битмапа в формате, поддерживаемом принтером
                bitmap.Save(ms, ImageFormat.Bmp); // Проверьте поддерживаемый формат

                using (var writer = new BinaryWriter(new MemoryStream()))
                {
                    // Инициализация принтера
                    writer.Write(new byte[] { 0x1B, 0x40 }); // Сброс принтера

                    // Обработка изображения в формате принтера
                    // Здесь ваш код для означает получение изображения в нужном формате
                    byte[] imageData = ms.ToArray();

                    // Пример отправки данных изображения для печати
                    writer.Write(new byte[] { 0x1D, 0x76, 0x30, 0x00, (byte)(bitmap.Width % 256), (byte)(bitmap.Width / 256) }); // Настройка ширины

                    // Записываем данные изображения
                    writer.Write(imageData);

                    // Завершение печати
                    writer.Write(new byte[] { 0x1D, 0x56, 0x41, 0x00 }); // Например, резка бумаги

                    return ((MemoryStream)writer.BaseStream).ToArray();
                }
            }
        }



        private async Task SendDataToPrinter(string printerIp, int port, byte[] data)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(printerIp, port);
                using (var stream = client.GetStream())
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
        }

        // Масштабирует изображение до указанного размера.
        private Bitmap ScaleBitmap(Bitmap bitmap, float scaleFactor)
        {
            // Рассчитываем новые размеры на основе коэффициента масштабирования
            int newWidth = (int)(bitmap.Width * scaleFactor);
            int newHeight = (int)(bitmap.Height * scaleFactor);

            Bitmap scaledBitmap = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(scaledBitmap))
            {
                // Используем высококачественный алгоритм интерполяции
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, newWidth, newHeight);
            }

            return scaledBitmap;
        }
    }
}
