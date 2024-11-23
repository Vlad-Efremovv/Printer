using Microsoft.AspNetCore.Mvc;
using System.Drawing;
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

        // по порту
        [HttpPost]
        [Route("printpdf")]
        public async Task<IActionResult> PrintPdfAsync([FromForm] PrintRequestDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Файл не получен.");

            // Проверка расширения файла
            //if (Path.GetExtension(request.File.FileName)?.ToLower() != ".pdf")
            //    return BadRequest("Файл должен быть в формате PDF.");

            string tempPath = Path.Combine(Path.GetTempPath(), request.File.FileName);

            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            try
            {
                using (TcpClient client = new TcpClient(request.PrinterIp, 9100))
                using (NetworkStream stream = client.GetStream())
                {
                    using (Image image = Image.FromFile(tempPath))
                    {
                        // Преобразование изображения в черно-белое
                        using (Bitmap bitmap = new Bitmap(image))
                        {
                            Bitmap grayscaleBitmap = new Bitmap(bitmap.Width, bitmap.Height);
                            for (int y = 0; y < bitmap.Height; y++)
                            {
                                for (int x = 0; x < bitmap.Width; x++)
                                {
                                    Color pixelColor = bitmap.GetPixel(x, y);
                                    // Вычисляем яркость (от черного до белого)
                                    int grayValue = (int)((pixelColor.R * 0.299) + (pixelColor.G * 0.587) + (pixelColor.B * 0.114));
                                    grayscaleBitmap.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                                }
                            }

                            // Преобразуем в массив байтов для передачи
                            byte[] imageBytes = CreateEscPosImage(grayscaleBitmap);
                            await stream.WriteAsync(imageBytes, 0, imageBytes.Length);
                        }
                    }

                    Console.WriteLine("Файл успешно отправлен на принтер.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошла ошибка: " + ex.Message);
                return StatusCode(500, "Ошибка при отправке на принтер.");
            }

            return Ok("Изображение успешно отправлено на принтер.");


            return Ok();

            //using var tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //try
            //{
            //    // Подключение к принтеру
            //    await tcpClient.ConnectAsync(request.PrinterIp, 9100);
            //    Console.WriteLine("Успешно подключено к принтеру.");

            //    // Вывод всех доступных принтеров
            //    Console.WriteLine("Доступные принтеры:");
            //    foreach (string printer in PrinterSettings.InstalledPrinters)
            //    {
            //        Console.WriteLine($"- {printer}");
            //    }

            //    using (var document = PdfDocument.Load(tempPath))
            //    {
            //        for (int i = 0; i < document.PageCount; i++)
            //        {
            //            using (var printDocument = new PrintDocument())
            //            {
            //                printDocument.PrinterSettings.PrinterName = "Имя_вашего_принтера";

            //                printDocument.PrintPage += (sender, e) =>
            //                {
            //                    // Рендерим текущую страницу PDF в графический объект
            //                    e.Graphics.DrawImage(document.Render(i, e.MarginBounds.Width, e.MarginBounds.Height, true), e.MarginBounds);
            //                };

            //                // Проверяем, установлен ли принтер
            //                if (printDocument.PrinterSettings.IsValid)
            //                {
            //                    printDocument.Print();
            //                    Console.WriteLine($"Страница {i + 1} успешно распечатана.");
            //                }
            //                else
            //                {
            //                    Console.WriteLine("Принтер не доступен.");
            //                    return StatusCode(500);
            //                }
            //            }
            //        }
            //    }

            //    Console.WriteLine("Все страницы успешно отправлены на печать.");
            //}
            //catch (Exception ex)
            //{
            //    // Обработка исключений
            //    Console.WriteLine($"Произошла ошибка: {ex.Message}");
            //}
            //finally
            //{
            //    if (System.IO.File.Exists(tempPath))
            //    {
            //        System.IO.File.Delete(tempPath);
            //    }
            //}


            // return StatusCode(200);
        }

        private byte[] CreateEscPosImage(Bitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // ESC/POS команды для установки режима растровой печати
                ms.WriteByte(0x1B); // ESC
                ms.WriteByte(0x2A); // *
                ms.WriteByte(0x21); // Выбор растрового изображения
                ms.WriteByte((byte)(bitmap.Width % 256)); // Ширина изображения (младший байт)
                ms.WriteByte((byte)(bitmap.Width / 256)); // Ширина изображения (старший байт)

                // Печатаем изображение
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x += 8)
                    {
                        byte b = 0;
                        // Обработка 8 пикселей за раз
                        for (int bit = 0; bit < 8; bit++)
                        {
                            if (x + bit < bitmap.Width)
                            {
                                Color pixelColor = bitmap.GetPixel(x + bit, y);
                                // Преобразование цвета в 0 (черный) или 1 (белый)
                                if (pixelColor.R < 128) // Порог для черного
                                    b |= (byte)(1 << (7 - bit)); // Устанавливаем бит
                            }
                        }
                        ms.WriteByte(b);
                    }
                }

                ms.WriteByte(0x0A); // Перевод строки
                return ms.ToArray();
            }
        }
    }
}
