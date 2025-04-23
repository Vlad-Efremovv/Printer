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
        public async Task<IActionResult> PrintPDFAsync(IFormFile file, string printerIP, int port = 09100)
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

                string zplToSend = "^XA^FO0,0^GFA," + binaryData.Length + "," + binaryData.Length + ",100," + zplImageData + "^XZ";

                zplToSend = "^XA\n^FO250,250\n^BQa,2,10\n^FDHA,Степа я хочу повышение зарплаты^FS\n^XZ\n";

                
                string printImage = "^XA^FO115,50^IME:LOGO.PNG^FS^XZ";

                // Open connection
                using (System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient())
                {
                    _logger.LogInformation("Attempting to connect to printer at {PrinterIP}:{Port}", ipAddress, port);
                    client.Connect(ipAddress, port);
                    _logger.LogInformation("Successfully connected to printer.");

                    // Write ZPL String to connection
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(client.GetStream(), Encoding.UTF8))
                    {
                        writer.Write(zplToSend + zplImageData);
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