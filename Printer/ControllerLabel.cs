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
        public async Task<IActionResult> PrintPDFAsync(string tpsIP, string QRsecret, string QRdescription = "",
            int port = 9100
        )
        {
            Console.WriteLine(QRdescription);
            try
            {
                string zplToSend = "";
                if (QRdescription == "")
                {
                    zplToSend = "^XA" +
                                "^FO200,200" +
                                "^BQa,3,15" +
                                $"^FDHA,{QRsecret}^FS" +
                                "^XZ";
                }
                else
                {
                    string[] descriptions = QRdescription.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    int topPad = 450;

                    Console.WriteLine(descriptions.Length);
                    Console.WriteLine(descriptions[0]);

                    zplToSend = "^XA" +
                                "^FO250,50" +
                                "^BQ,2,13" +
                                $"^FDHA,{QRsecret}^FS";

                    for (int i = 0; i < descriptions.Length; i++)
                    {
                        zplToSend += $"^FO200,{topPad + (i * 50)}" +
                                     $"^A0N,35,35" +
                                     $"^FD" +
                                     $"{descriptions[i]}" +
                                     $"^FS";
                    }

                    zplToSend += "^XZ";
                    Console.WriteLine(zplToSend);
                }

                using (TcpClient client = new TcpClient())
                {
                    _logger.LogInformation("Attempting to connect to printer at {tpsIP}:{Port}", tpsIP, port);
                    client.Connect(tpsIP, port);
                    _logger.LogInformation("Successfully connected to printer.");

                    // Write ZPL String to connection
                    using (System.IO.StreamWriter
                           writer = new System.IO.StreamWriter(client.GetStream(), Encoding.UTF8))
                    {
                        writer.Write(zplToSend);
                        writer.Flush();
                    }
                }

                return StatusCode(200, "Переданов печать на принтер: " + tpsIP);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing file: {Message}", ex.Message);
                return StatusCode(500, $"Ошибка при печати: {ex.Message}");
            }
        }
    }
}