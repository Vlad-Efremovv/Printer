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

        // �� �����
        [HttpPost]
        [Route("printpdf")]
        public async Task<IActionResult> PrintPdfAsync([FromForm] PrintRequestDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("���� �� �������.");

            // �������� ���������� �����
            //if (Path.GetExtension(request.File.FileName)?.ToLower() != ".pdf")
            //    return BadRequest("���� ������ ���� � ������� PDF.");

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
                        // �������������� ����������� � �����-�����
                        using (Bitmap bitmap = new Bitmap(image))
                        {
                            Bitmap grayscaleBitmap = new Bitmap(bitmap.Width, bitmap.Height);
                            for (int y = 0; y < bitmap.Height; y++)
                            {
                                for (int x = 0; x < bitmap.Width; x++)
                                {
                                    Color pixelColor = bitmap.GetPixel(x, y);
                                    // ��������� ������� (�� ������� �� ������)
                                    int grayValue = (int)((pixelColor.R * 0.299) + (pixelColor.G * 0.587) + (pixelColor.B * 0.114));
                                    grayscaleBitmap.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                                }
                            }

                            // ����������� � ������ ������ ��� ��������
                            byte[] imageBytes = CreateEscPosImage(grayscaleBitmap);
                            await stream.WriteAsync(imageBytes, 0, imageBytes.Length);
                        }
                    }

                    Console.WriteLine("���� ������� ��������� �� �������.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("��������� ������: " + ex.Message);
                return StatusCode(500, "������ ��� �������� �� �������.");
            }

            return Ok("����������� ������� ���������� �� �������.");


            return Ok();

            //using var tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //try
            //{
            //    // ����������� � ��������
            //    await tcpClient.ConnectAsync(request.PrinterIp, 9100);
            //    Console.WriteLine("������� ���������� � ��������.");

            //    // ����� ���� ��������� ���������
            //    Console.WriteLine("��������� ��������:");
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
            //                printDocument.PrinterSettings.PrinterName = "���_������_��������";

            //                printDocument.PrintPage += (sender, e) =>
            //                {
            //                    // �������� ������� �������� PDF � ����������� ������
            //                    e.Graphics.DrawImage(document.Render(i, e.MarginBounds.Width, e.MarginBounds.Height, true), e.MarginBounds);
            //                };

            //                // ���������, ���������� �� �������
            //                if (printDocument.PrinterSettings.IsValid)
            //                {
            //                    printDocument.Print();
            //                    Console.WriteLine($"�������� {i + 1} ������� �����������.");
            //                }
            //                else
            //                {
            //                    Console.WriteLine("������� �� ��������.");
            //                    return StatusCode(500);
            //                }
            //            }
            //        }
            //    }

            //    Console.WriteLine("��� �������� ������� ���������� �� ������.");
            //}
            //catch (Exception ex)
            //{
            //    // ��������� ����������
            //    Console.WriteLine($"��������� ������: {ex.Message}");
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
                // ESC/POS ������� ��� ��������� ������ ��������� ������
                ms.WriteByte(0x1B); // ESC
                ms.WriteByte(0x2A); // *
                ms.WriteByte(0x21); // ����� ���������� �����������
                ms.WriteByte((byte)(bitmap.Width % 256)); // ������ ����������� (������� ����)
                ms.WriteByte((byte)(bitmap.Width / 256)); // ������ ����������� (������� ����)

                // �������� �����������
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x += 8)
                    {
                        byte b = 0;
                        // ��������� 8 �������� �� ���
                        for (int bit = 0; bit < 8; bit++)
                        {
                            if (x + bit < bitmap.Width)
                            {
                                Color pixelColor = bitmap.GetPixel(x + bit, y);
                                // �������������� ����� � 0 (������) ��� 1 (�����)
                                if (pixelColor.R < 128) // ����� ��� �������
                                    b |= (byte)(1 << (7 - bit)); // ������������� ���
                            }
                        }
                        ms.WriteByte(b);
                    }
                }

                ms.WriteByte(0x0A); // ������� ������
                return ms.ToArray();
            }
        }
    }
}
