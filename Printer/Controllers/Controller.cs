using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Net.Sockets;
using System.Runtime.InteropServices;

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

        //[HttpGet]
        //[Route("getPrinter")]
        //public async Task<IActionResult> PrintPdfAsync() => Ok(PrinterSettings.InstalledPrinters);


        //[HttpPost]
        //[Route("printPDF")]
        //public async Task<IActionResult> PrintPdfAsyncss(IFormFile file, string printerName = "T80 (копия 1)")
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("Файл отсутствует");

        //    if (Path.GetExtension(file.FileName)?.ToLower() != ".pdf")
        //        return BadRequest("Файл не формата PDF");

        //    // полный путь к файлу
        //    string rootPath = Path.Combine(Directory.GetCurrentDirectory(), file.FileName);

        //    try
        //    {
        //        // Сохранение файла во временное хранилище (Папка temp)
        //        using (var stream = new FileStream(rootPath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }

        //        // загружаеи локальное имя принтера
        //        using (var document = PdfiumViewer.PdfDocument.Load(rootPath))
        //        {
        //            for (int i = 0; i < document.PageCount; i++)
        //            {
        //                using (var printDocument = new PrintDocument())
        //                {
        //                    // устанавливаем локальное имя принтера в сети
        //                    printDocument.PrinterSettings.PrinterName = printerName;

        //                    // Устанавливаем размер бумаги (ширина 8 см = 80 мм)
        //                    // 8 см по ширине, 12 см по высоте
        //                    printDocument.DefaultPageSettings.PaperSize = new PaperSize("Custom", 80 * 10, 80 * 15);

        //                    // Устанавливаем ориентацию страницы на книжную (портрет)
        //                    printDocument.DefaultPageSettings.Landscape = false; // false - портретная ориентация


        //                    // Устанавливаем все поля на ноль
        //                    printDocument.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);


        //                    printDocument.PrintPage += (sender, e) =>
        //                    {
        //                        // Масштабируем изображение
        //                        //float scaleFactor = 0.66f; // Масштаб 66%
        //                        float scaleFactor = 1.5f; // Масштаб 66%

        //                        // Рассчитываем новые размеры для изображения с учетом масштаба
        //                        int scaledWidth = (int)(e.MarginBounds.Width * scaleFactor);
        //                        int scaledHeight = (int)(e.MarginBounds.Height * scaleFactor);

        //                        // Рендерим страницу PDF в графику с применением масштаба
        //                        e.Graphics.DrawImage(document.Render(i, scaledWidth, scaledHeight, true), e.MarginBounds);
        //                    };

        //                    if (printDocument.PrinterSettings.IsValid)
        //                    {
        //                        // Печать файла если принтер доступен
        //                        printDocument.Print();
        //                        _logger.LogInformation($"Расспечатано страниц: {i + 1}.");
        //                    }
        //                    else
        //                    {
        //                        _logger.LogError("Ошибка печати, изза проблемы с принтером");
        //                        return BadRequest("Ошибка печати, изза проблемы с принтером");
        //                    }
        //                }
        //            }
        //        }

        //        return Ok("Печать прошла успешно");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Произошла ошибка: {ex.Message}");
        //        return StatusCode(500, $"Произошла ошибка: {ex.Message}");
        //    }
        //    finally
        //    {
        //        // удаляем временный файл
        //        if (System.IO.File.Exists(rootPath))
        //        {
        //            System.IO.File.Delete(rootPath);
        //        }
        //    }
        //}

        // Функция для конвертации изображения в байты
        //static async Task<byte[]> ImageToBytesAsync(string imagePath)
        //{
        //    using (Bitmap bitmap = new Bitmap(imagePath))
        //    {
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            // Сохраняем изображение в формат JPEG (можно заменить на другой формат, если нужно)
        //            await Task.Run(() => bitmap.Save(ms, ImageFormat.Jpeg));
        //            return ms.ToArray();
        //        }
        //    }
        //}

        [HttpPost]
        [Route("printImage")]
        public async Task<IActionResult> PrintBmpAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл отсутствует");

            //if (Path.GetExtension(file.FileName)?.ToLower() != ".png"
            //    || Path.GetExtension(file.FileName)?.ToLower() != ".bmp"
            //    || Path.GetExtension(file.FileName)?.ToLower() != ".jpg")
            //    return BadRequest("Поддерживается форматы png bmp jpg");

            // Полный путь к временному файлу
            string tempPath = Path.Combine(Path.GetTempPath(), file.FileName);

            try
            {
                // Сохранение загруженного файла во временное хранилище
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Загрузка изображения из временного файла
                using (Bitmap image = new Bitmap(tempPath))
                {
                    // Преобразование изображения в ESC/POS формат
                    byte[] imageData = ConvertImageToEscPos(image);

                    // Отправка данных на принтер через сокет
                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync("192.168.0.105", 9100);

                        using (var stream = client.GetStream())
                        {
                            await stream.WriteAsync(imageData, 0, imageData.Length);
                            Console.WriteLine("Изображение отправлено на принтер.");
                        }
                    }
                }

                // Удаляем временный файл
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }

                return Ok("Изображение успешно отправлено на принтер.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка печати: {ex.Message} {ex}");
                return StatusCode(500, $"Ошибка печати: {ex.Message}");
            }
            finally
            {
                // удаляем временный файл
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }


        private byte[] ConvertImageToEscPos(Bitmap bitmap, int zoom = 1)
        {
            // Масштабируем изображение в 2 раза
            Bitmap scaledBitmap = ScaleBitmap(bitmap, bitmap.Width * zoom, bitmap.Height * zoom);

            // Преобразуем изображение в черно-белое
            Bitmap monochromeBitmap = ConvertToMonochrome(scaledBitmap);

            using (var ms = new MemoryStream())
            {
                // Добавляем команды и изображение
                using (var writer = new BinaryWriter(ms))
                {
                    // Команда инициализации принтера
                    writer.Write(new byte[] { 0x1B, 0x40 });

                    // Устанавливаем межстрочный интервал в 0
                    writer.Write(new byte[] { 0x1B, 0x33, 0x00 });

                    // Преобразуем изображение в байты для ESC/POS
                    int width = monochromeBitmap.Width;
                    int height = monochromeBitmap.Height;

                    for (int y = 0; y < height; y += 25) // ESC/POS поддерживает печать 24 строк за раз
                    {
                        writer.Write(new byte[] { 0x1B, 0x2A, 33, (byte)(width % 256), (byte)(width / 256) });

                        for (int x = 0; x < width; x++)
                        {
                            for (int k = 0; k < 3; k++) // 3 байта на каждые 24 строки
                            {
                                byte data = 0;

                                for (int b = 0; b < 8; b++)
                                {
                                    int pixelY = y + k * 8 + b;

                                    if (pixelY < height)
                                    {
                                        Color pixel = monochromeBitmap.GetPixel(x, pixelY);
                                        if (pixel.R == 0) // Если пиксель черный
                                        {
                                            data |= (byte)(1 << (7 - b));
                                        }
                                    }
                                }

                                writer.Write(data);
                            }
                        }

                        // Добавляем символ новой строки
                        writer.Write(new byte[] { 0x0A });
                    }

                    // Добавляем команду резки бумаги
                    writer.Write(new byte[] { 0x1D, 0x56, 0x41, 0x10 });
                }

                return ms.ToArray();
            }
        }


        // Масштабирует изображение до указанного размера.
        private Bitmap ScaleBitmap(Bitmap bitmap, int newWidth, int newHeight)
        {
            Bitmap scaledBitmap = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(scaledBitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, newWidth, newHeight);
            }
            return scaledBitmap;
        }


        private Bitmap ConvertToMonochrome(Bitmap bitmap)
        {
            // Преобразуем изображение в формат RGB, если оно индексированное
            Bitmap rgbBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(rgbBitmap))
            {
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            }

            // Получаем доступ к пикселям через LockBits для быстрой обработки
            BitmapData rgbData = rgbBitmap.LockBits(
                new Rectangle(0, 0, rgbBitmap.Width, rgbBitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );

            byte[] rgbBytes = new byte[rgbData.Height * rgbData.Stride];
            Marshal.Copy(rgbData.Scan0, rgbBytes, 0, rgbBytes.Length);

            int widthBytes = (rgbBitmap.Width + 7) / 8; // Ширина в байтах для 1bpp
            List<byte[]> validRows = new List<byte[]>();

            // Преобразуем все строки в чёрно-белый формат
            for (int y = 0; y < rgbBitmap.Height; y++)
            {
                int rgbOffset = y * rgbData.Stride;

                // Буфер для строки в чёрно-белом формате
                byte[] monoRow = new byte[widthBytes];

                for (int x = 0; x < rgbBitmap.Width; x++)
                {
                    // Рассчитываем оттенок серого
                    int grayValue = (rgbBytes[rgbOffset + x * 3] +
                                     rgbBytes[rgbOffset + x * 3 + 1] +
                                     rgbBytes[rgbOffset + x * 3 + 2]) / 3;

                    // Инвертируем цвет (чёрный становится белым и наоборот)
                    if (grayValue >= 128) // Изначально белый пиксель
                    {
                        // Устанавливаем как чёрный
                        monoRow[x / 8] |= (byte)(0x80 >> (x % 8));
                    }
                }

                // Добавляем строку, даже если она полностью белая (так как 1bpp формат требует каждой строки)
                validRows.Add(monoRow);
            }

            rgbBitmap.UnlockBits(rgbData);

            // Создаём новое изображение только с непустыми строками
            Bitmap monochromeBitmap = new Bitmap(rgbBitmap.Width, validRows.Count, PixelFormat.Format1bppIndexed);

            BitmapData monoData = monochromeBitmap.LockBits(
                new Rectangle(0, 0, monochromeBitmap.Width, monochromeBitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format1bppIndexed
            );

            // Копируем строки в новое изображение
            byte[] monoBytes = new byte[validRows.Count * monoData.Stride];
            for (int y = 0; y < validRows.Count; y++)
            {
                Buffer.BlockCopy(validRows[y], 0, monoBytes, y * monoData.Stride, validRows[y].Length);
            }

            Marshal.Copy(monoBytes, 0, monoData.Scan0, monoBytes.Length);

            monochromeBitmap.UnlockBits(monoData);

            return monochromeBitmap;
        }
    }
}