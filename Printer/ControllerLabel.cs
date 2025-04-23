using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using PdfiumViewer;
using System.Net.Sockets;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace Printer
{
    [ApiController]
    public class ControllerLabel : ControllerBase
    {
        private readonly ILogger<ControllerLabel> _logger;

        public ControllerLabel(ILogger<ControllerLabel> logger)
        {
            _logger = logger;
        }

        public class PrintRequestDto
        {
            public IFormFile File { get; set; }
            public string PrinterIp { get; set; }
        }

        [HttpPost]
        [Route("print")]
        public async Task<IActionResult> PrintPDFAsync(string printerIP = "10.121.22.57", int port = 09100,
            string textQR = "5938d8d3-fc8f-42f8-8c17-95be96929ed7")

        {
            string ipAddress = printerIP;
            string zplImageData = string.Empty;
            string filePath = @"C:\Users\Vlad\Pictures\Screenshots\Снимок экрана 2025-04-23 102352.png";

            try
            {
                byte[] binaryData = System.IO.File.ReadAllBytes(filePath);
                foreach (Byte b in binaryData)
                {
                    string hexRep = String.Format("{0:X}", b);
                    if (hexRep.Length == 1)
                        hexRep = "0" + hexRep;
                    zplImageData += hexRep;
                }

                string zplToSend = "^XA^FO0,0^GFA," + binaryData.Length + "," + binaryData.Length + ",100," +
                                   zplImageData + "^XZ";

                zplToSend = "^XA" +
                            "^FO250,50" + // Position for QR Code at (250,30)
                            "^BQ,2,8" + // QR Code settings
                            $"^FDHA,{textQR}^FS" + // QR Code Data
                            "^FO200,450" + // Position for the first line of text at (100,200)
                            "^A0N,35,35^FDПанель управления^FS" + // First line
                            "^FO200,490" + // Position for the second line (shifted down for space)
                            "^A0N,35,35^FDКЕГН.1.1.4556.32.110.90.100-01^FS" + // Second line
                            "^FO200,540" + // Position for the third line
                            "^A0N,35,35^FD\u2116 00863^FS" + // Third line
                            "^FO200,590" + // Position for the fourth line
                            "^A0N,35,35^FDДата: 23-04-2025^FS" + // Fourth line
                            "^FO200,640" + // Position for the fifth line
                            "^A0N,35,35^FDООО \"КСК Элком\"^FS" + // Fifth line
                            "^XZ";


                using (System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient())
                {
                    _logger.LogInformation("Attempting to connect to printer at {PrinterIP}:{Port}", ipAddress, port);
                    client.Connect(ipAddress, port);
                    _logger.LogInformation("Successfully connected to printer.");

                    // Write ZPL String to connection
                    using (System.IO.StreamWriter
                           writer = new System.IO.StreamWriter(client.GetStream(), Encoding.UTF8))
                    {
                        writer.Write(zplToSend);
                        writer.Flush();
                    }
                }

                return StatusCode(200, "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing file: {Message}", ex.Message);
                return StatusCode(500, $"Ошибка при обработке файла: {ex.Message}");
            }
        }
    }
}