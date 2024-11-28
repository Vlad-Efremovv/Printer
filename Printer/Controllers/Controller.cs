using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using PdfiumViewer;
using System.Net.Sockets;
using System.Drawing.Imaging;
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

        [HttpPost]
        [Route("printPDF")]
        public async Task<IActionResult> PrintPDFAsync(IFormFile file, string tpsIP, int port = 9100, float zoomImage = 1, bool inversion = false)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл отсутствует");

            string tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
            List<string> outputImagePaths = new List<string>();

            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    Console.WriteLine($"Файл сохранен: {file.FileName}");
                }

                outputImagePaths = renderPdfToFile(tempPath, 300);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при обработке файла: {ex.Message}");
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    try
                    {
                        System.IO.File.Delete(tempPath);
                        Console.WriteLine($"Временный файл удален: {tempPath}");
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Ошибка при удалении временного файла: {ex.Message}");
                    }
                }
            }

            // Отправляем все собранные изображения на принтер
            foreach (var outputImagePath in outputImagePaths)
            {
                Console.WriteLine($"{outputImagePath}");

                try
                {

                    // Загрузка изображения из временного файла
                    using (Bitmap image = new Bitmap(outputImagePath))
                    {
                        // Преобразование изображения в ESC/POS формат
                        byte[] imageData = ConvertImageToEscPos(image, zoomImage, inversion);

                        // Отправка данных на принтер через сокет
                        using (var client = new TcpClient())
                        {
                            await client.ConnectAsync(tpsIP, port);

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

                // await SendImageToPrinter(tpsIP, port, outputImagePath);
            }

            return Ok("PDF успешно обработан и отправлен на печать");
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

        //private async Task SendImageToPrinter(string tpsIP, int port, string imagePath)
        //{
        //    try
        //    {
        //        using (var client = new TcpClient(tpsIP, port))
        //        using (var stream = client.GetStream())
        //        {
        //            using (var bitmap = new Bitmap(imagePath))
        //            {
        //                // Преобразуем Bitmap в байтовый массив
        //                using (var memoryStream = new MemoryStream())
        //                {
        //                    bitmap.Save(memoryStream, ImageFormat.Bmp);
        //                    byte[] imageBytes = memoryStream.ToArray();

        //                    // Отправляем данные на принтер
        //                    //await stream.WriteAsync(imageBytes, 0, imageBytes.Length);

        //                    //// Добавляем команду резки бумаги для Epson ESC/POS
        //                    //byte[] cutCommand = new byte[] { 0x1D, 0x56, 0x41, 0x10 }; // Команда обрезки
        //                    //await stream.WriteAsync(cutCommand, 0, cutCommand.Length);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Ошибка при отправке изображения на принтер: {ex.Message}");
        //    }
        //}

        //public async Task<IActionResult> PrintBmpAsync(IFormFile file, string tpsIP, int port = 9100, float zoomImage = 1, bool inversion = true)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("Файл отсутствует");

        //    //if (Path.GetExtension(file.FileName)?.ToLower() != ".png"
        //    //    || Path.GetExtension(file.FileName)?.ToLower() != ".bmp"
        //    //    || Path.GetExtension(file.FileName)?.ToLower() != ".jpg")
        //    //    return BadRequest("Поддерживается форматы png bmp jpg");

        //    // Полный путь к временному файлу
        //    string tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
        //        // Сохранение загруженного файла во временное хранилище
        //        using (var stream = new FileStream(tempPath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }

        //    try
        //    {

        //        // Загрузка изображения из временного файла
        //        using (Bitmap image = new Bitmap(tempPath))
        //        {
        //            // Преобразование изображения в ESC/POS формат
        //            byte[] imageData = ConvertImageToEscPos(image, zoomImage, inversion);

        //            // Отправка данных на принтер через сокет
        //            using (var client = new TcpClient())
        //            {
        //                await client.ConnectAsync(tpsIP, port);

        //                using (var stream = client.GetStream())
        //                {
        //                    await stream.WriteAsync(imageData, 0, imageData.Length);
        //                    Console.WriteLine("Изображение отправлено на принтер.");
        //                }
        //            }
        //        }

        //        // Удаляем временный файл
        //        if (System.IO.File.Exists(tempPath))
        //        {
        //            System.IO.File.Delete(tempPath);
        //        }

        //        return Ok("Изображение успешно отправлено на принтер.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Ошибка печати: {ex.Message} {ex}");
        //        return StatusCode(500, $"Ошибка печати: {ex.Message}");
        //    }
        //    finally
        //    {
        //        // удаляем временный файл
        //        if (System.IO.File.Exists(tempPath))
        //        {
        //            System.IO.File.Delete(tempPath);
        //        }
        //    }
        //}


        private byte[] ConvertImageToEscPos(Bitmap bitmap, float zoomImage, bool inversion)
        {
            // Масштабируем изображение в 2 раза
            Bitmap scaledBitmap = ScaleBitmap(bitmap, zoomImage);

            // Преобразуем изображение в черно-белое
            Bitmap monochromeBitmap = ConvertToMonochrome(scaledBitmap, inversion);

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


        private Bitmap ConvertToMonochrome(Bitmap bitmap, bool inversion)
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
                    if (grayValue >= 128 && inversion) // Изначально белый пиксель
                    {
                        // Устанавливаем как чёрный
                        monoRow[x / 8] |= (byte)(0x80 >> (x % 8));
                    }

                    if (grayValue <= 128 && !inversion) // Изначально черный пиксель
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


        //[HttpPost]
        //[Route("printPDF")]
        //public async Task<IActionResult> PrintPDFAsync(IFormFile file, string tpsIP, int port = 9100, float zoomImage = 1, bool inversion = false)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("Файл отсутствует");

        //    string tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
        //    using (var stream = new FileStream(tempPath, FileMode.Create))
        //    {
        //        await file.CopyToAsync(stream);
        //        Console.WriteLine(file.FileName);
        //    }

        //    try
        //    {
        //        void renderPdfToFile(string pdfFilename, string outputImageFilename, int dpi)
        //        {
        //            Console.WriteLine(pdfFilename);
        //            using (var doc = PdfDocument.Load(pdfFilename))
        //            {               // Загрузка PDF-документа из файла
        //                Console.WriteLine($"Загрузка PDF-документа из файла");
        //                for (int page = 0; page < doc.PageCount; page++)
        //                {          // Цикл по страницам
        //                    Console.WriteLine($"Цикл по страницам");
        //                    using (var img = doc.Render(page, dpi, dpi, false))
        //                    {   // Рендер с заданным dpi и без печати
        //                        Console.WriteLine($"Рендер с заданным dpi и без печати");

        //                        string outputFilePath = $"page_{page}_{outputImageFilename}";
        //                        img.Save(outputFilePath);



        //                        Console.WriteLine($"Файл сохранен: {outputFilePath}"); // Вывод пути к сохраненному файлу
        //                    }
        //                }
        //            }
        //        }


        //        //using (var document = PdfDocument.Load(tempPath))
        //        //{
        //        //    for (int i = 0; i < document.PageCount; i++)
        //        //    {
        //        //        // Приведение типа Image к Bitmap

        //        //        using (Bitmap image = (Bitmap)document.Render(i, new Size(300, 300))) // Проверьте, принимает ли метод этот аргумент
        //        //        {
        //        //            if (inversion)
        //        //            {
        //        //                InvertImageColors(image);
        //        //            }

        //        //            PrintBitmap(image, tpsIP, port);
        //        //        }
        //        //    }
        //        //}

        //        return Ok("Изображения успешно отправлены на принтер.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Ошибка печати: {ex.Message}");
        //        return StatusCode(500, $"Ошибка печати: {ex.Message}");
        //    }
        //    finally
        //    {
        //        // Удаляем временный файл
        //        if (System.IO.File.Exists(tempPath))
        //        {
        //            System.IO.File.Delete(tempPath);
        //        }
        //    }
        //}

        //static void InvertImageColors(Bitmap image)
        //{
        //    for (int y = 0; y < image.Height; y++)
        //    {
        //        for (int x = 0; x < image.Width; x++)
        //        {
        //            Color originalColor = image.GetPixel(x, y);
        //            Color newColor = Color.FromArgb(originalColor.A, 255 - originalColor.R, 255 - originalColor.G, 255 - originalColor.B);
        //            image.SetPixel(x, y, newColor);
        //        }
        //    }
        //}

        //static void PrintBitmap(Bitmap bitmap, string tpsIP, int port)
        //{
        //    using (TcpClient client = new TcpClient(tpsIP, port))
        //    using (NetworkStream stream = client.GetStream())
        //    {
        //        using (var ms = new MemoryStream())
        //        {
        //            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //            ms.Position = 0;

        //            // Преобразуем изображение в байты для ESC/POS
        //            using (var writer = new BinaryWriter(stream))
        //            {
        //                int width = bitmap.Width;
        //                int height = bitmap.Height;

        //                for (int y = 0; y < height; y += 24) // ESC/POS поддерживает печать 24 строки за раз
        //                {
        //                    writer.Write(new byte[] { 0x1B, 0x2A, 33, (byte)(width % 256), (byte)(width / 256) });

        //                    for (int x = 0; x < width; x++)
        //                    {
        //                        byte data = 0;

        //                        for (int b = 0; b < 8; b++)
        //                        {
        //                            int pixelY = y + b;

        //                            if (pixelY < height)
        //                            {
        //                                Color pixel = bitmap.GetPixel(x, pixelY);
        //                                if (pixel.ToArgb() == Color.Black.ToArgb()) // Если пиксель черный
        //                                {
        //                                    data |= (byte)(1 << (7 - b));
        //                                }
        //                            }
        //                        }

        //                        writer.Write(data);
        //                    }

        //                    // Добавляем символ новой строки
        //                    writer.Write(new byte[] { 0x0A });
        //                }

        //                // Добавляем команду резки бумаги, если поддерживается
        //                // Например, для Epson ESC/POS:
        //                writer.Write(new byte[] { 0x1D, 0x56, 0x41, 0x10 });
        //            }
        //        }
        //    }
        //}



        //[HttpPost]
        //    [Route("printImage")]
        //    public async Task<IActionResult> PrintBmpAsync(IFormFile file, string tpsIP, int port = 9100, float zoomImage = 1, bool inversion = true)
        //    {
        //        if (file == null || file.Length == 0)
        //            return BadRequest("Файл отсутствует");

        //        //if (Path.GetExtension(file.FileName)?.ToLower() != ".png"
        //        //    || Path.GetExtension(file.FileName)?.ToLower() != ".bmp"
        //        //    || Path.GetExtension(file.FileName)?.ToLower() != ".jpg")
        //        //    return BadRequest("Поддерживается форматы png bmp jpg");

        //        // Полный путь к временному файлу
        //        string tempPath = Path.Combine(Path.GetTempPath(), file.FileName);

        //        try
        //        {
        //            // Сохранение загруженного файла во временное хранилище
        //            using (var stream = new FileStream(tempPath, FileMode.Create))
        //            {
        //                await file.CopyToAsync(stream);
        //            }

        //            // Загрузка изображения из временного файла
        //            using (Bitmap image = new Bitmap(tempPath))
        //            {
        //                // Преобразование изображения в ESC/POS формат
        //                byte[] imageData = ConvertImageToEscPos(image, zoomImage, inversion);

        //                // Отправка данных на принтер через сокет
        //                using (var client = new TcpClient())
        //                {
        //                    await client.ConnectAsync(tpsIP, port);

        //                    using (var stream = client.GetStream())
        //                    {
        //                        await stream.WriteAsync(imageData, 0, imageData.Length);
        //                        Console.WriteLine("Изображение отправлено на принтер.");
        //                    }
        //                }
        //            }

        //            // Удаляем временный файл
        //            if (System.IO.File.Exists(tempPath))
        //            {
        //                System.IO.File.Delete(tempPath);
        //            }

        //            return Ok("Изображение успешно отправлено на принтер.");
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Ошибка печати: {ex.Message} {ex}");
        //            return StatusCode(500, $"Ошибка печати: {ex.Message}");
        //        }
        //        finally
        //        {
        //            // удаляем временный файл
        //            if (System.IO.File.Exists(tempPath))
        //            {
        //                System.IO.File.Delete(tempPath);
        //            }
        //        }
        //    }


        //    private byte[] ConvertImageToEscPos(Bitmap bitmap, float zoomImage, bool inversion)
        //    {
        //        // Масштабируем изображение в 2 раза
        //        Bitmap scaledBitmap = ScaleBitmap(bitmap, zoomImage);

        //        // Преобразуем изображение в черно-белое
        //        Bitmap monochromeBitmap = ConvertToMonochrome(scaledBitmap, inversion);

        //        using (var ms = new MemoryStream())
        //        {
        //            // Добавляем команды и изображение
        //            using (var writer = new BinaryWriter(ms))
        //            {
        //                // Команда инициализации принтера
        //                writer.Write(new byte[] { 0x1B, 0x40 });

        //                // Устанавливаем межстрочный интервал в 0
        //                writer.Write(new byte[] { 0x1B, 0x33, 0x00 });

        //                // Преобразуем изображение в байты для ESC/POS
        //                int width = monochromeBitmap.Width;
        //                int height = monochromeBitmap.Height;

        //                for (int y = 0; y < height; y += 25) // ESC/POS поддерживает печать 24 строк за раз
        //                {
        //                    writer.Write(new byte[] { 0x1B, 0x2A, 33, (byte)(width % 256), (byte)(width / 256) });

        //                    for (int x = 0; x < width; x++)
        //                    {
        //                        for (int k = 0; k < 3; k++) // 3 байта на каждые 24 строки
        //                        {
        //                            byte data = 0;

        //                            for (int b = 0; b < 8; b++)
        //                            {
        //                                int pixelY = y + k * 8 + b;

        //                                if (pixelY < height)
        //                                {
        //                                    Color pixel = monochromeBitmap.GetPixel(x, pixelY);
        //                                    if (pixel.R == 0) // Если пиксель черный
        //                                    {
        //                                        data |= (byte)(1 << (7 - b));
        //                                    }
        //                                }
        //                            }

        //                            writer.Write(data);
        //                        }
        //                    }

        //                    // Добавляем символ новой строки
        //                    writer.Write(new byte[] { 0x0A });
        //                }

        //                // Добавляем команду резки бумаги
        //                writer.Write(new byte[] { 0x1D, 0x56, 0x41, 0x10 });
        //            }

        //            return ms.ToArray();
        //        }
        //    }


        //    // Масштабирует изображение до указанного размера.
        //    private Bitmap ScaleBitmap(Bitmap bitmap, float scaleFactor)
        //    {
        //        // Рассчитываем новые размеры на основе коэффициента масштабирования
        //        int newWidth = (int)(bitmap.Width * scaleFactor);
        //        int newHeight = (int)(bitmap.Height * scaleFactor);

        //        Bitmap scaledBitmap = new Bitmap(newWidth, newHeight);
        //        using (Graphics g = Graphics.FromImage(scaledBitmap))
        //        {
        //            // Используем высококачественный алгоритм интерполяции
        //            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        //            g.DrawImage(bitmap, 0, 0, newWidth, newHeight);
        //        }

        //        return scaledBitmap;
        //    }


        //    private Bitmap ConvertToMonochrome(Bitmap bitmap, bool inversion)
        //    {
        //        // Преобразуем изображение в формат RGB, если оно индексированное
        //        Bitmap rgbBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
        //        using (Graphics g = Graphics.FromImage(rgbBitmap))
        //        {
        //            g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        //        }

        //        // Получаем доступ к пикселям через LockBits для быстрой обработки
        //        BitmapData rgbData = rgbBitmap.LockBits(
        //            new Rectangle(0, 0, rgbBitmap.Width, rgbBitmap.Height),
        //            ImageLockMode.ReadOnly,
        //            PixelFormat.Format24bppRgb
        //        );

        //        byte[] rgbBytes = new byte[rgbData.Height * rgbData.Stride];
        //        Marshal.Copy(rgbData.Scan0, rgbBytes, 0, rgbBytes.Length);

        //        int widthBytes = (rgbBitmap.Width + 7) / 8; // Ширина в байтах для 1bpp
        //        List<byte[]> validRows = new List<byte[]>();

        //        // Преобразуем все строки в чёрно-белый формат
        //        for (int y = 0; y < rgbBitmap.Height; y++)
        //        {
        //            int rgbOffset = y * rgbData.Stride;

        //            // Буфер для строки в чёрно-белом формате
        //            byte[] monoRow = new byte[widthBytes];

        //            for (int x = 0; x < rgbBitmap.Width; x++)
        //            {
        //                // Рассчитываем оттенок серого
        //                int grayValue = (rgbBytes[rgbOffset + x * 3] +
        //                                 rgbBytes[rgbOffset + x * 3 + 1] +
        //                                 rgbBytes[rgbOffset + x * 3 + 2]) / 3;

        //                // Инвертируем цвет (чёрный становится белым и наоборот)
        //                if (grayValue >= 128 && inversion) // Изначально белый пиксель
        //                {
        //                    // Устанавливаем как чёрный
        //                    monoRow[x / 8] |= (byte)(0x80 >> (x % 8));
        //                }

        //                if (grayValue <= 128 && !inversion) // Изначально черный пиксель
        //                {
        //                    // Устанавливаем как чёрный
        //                    monoRow[x / 8] |= (byte)(0x80 >> (x % 8));
        //                }
        //            }

        //            // Добавляем строку, даже если она полностью белая (так как 1bpp формат требует каждой строки)
        //            validRows.Add(monoRow);
        //        }

        //        rgbBitmap.UnlockBits(rgbData);

        //        // Создаём новое изображение только с непустыми строками
        //        Bitmap monochromeBitmap = new Bitmap(rgbBitmap.Width, validRows.Count, PixelFormat.Format1bppIndexed);

        //        BitmapData monoData = monochromeBitmap.LockBits(
        //            new Rectangle(0, 0, monochromeBitmap.Width, monochromeBitmap.Height),
        //            ImageLockMode.WriteOnly,
        //            PixelFormat.Format1bppIndexed
        //        );

        //        // Копируем строки в новое изображение
        //        byte[] monoBytes = new byte[validRows.Count * monoData.Stride];
        //        for (int y = 0; y < validRows.Count; y++)
        //        {
        //            Buffer.BlockCopy(validRows[y], 0, monoBytes, y * monoData.Stride, validRows[y].Length);
        //        }

        //        Marshal.Copy(monoBytes, 0, monoData.Scan0, monoBytes.Length);

        //        monochromeBitmap.UnlockBits(monoData);

        //        return monochromeBitmap;
        //    }
    }
}
