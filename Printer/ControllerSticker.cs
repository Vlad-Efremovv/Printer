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
    public class ControllerSticker : ControllerBase
    {
        private readonly ILogger<ControllerSticker> _logger;

        public ControllerSticker(ILogger<ControllerSticker> logger)
        {
            _logger = logger;
        }

        public class PrintRequestDto
        {
            public IFormFile File { get; set; }
            public string PrinterIp { get; set; }
        }

        [HttpPost]
        [Route("printSticker")]
        public async Task<IActionResult> PrintPDFAsync(
            string tpsIP,
            string secretQR,
            string descriptionQR = "",
            int textSize = 35,
            int sizeQR = 14,
            int port = 9100
        )
        {
            try
            {
                string zplToSend = "";
                if (descriptionQR == "")
                {
                    zplToSend = "^XA" +
                                $"^FO200,200" +
                                $"^BQa,3,{sizeQR}" +
                                $"^FDHA,{secretQR}^FS" +
                                "^XZ";
                }
                else
                {
                    string[] descriptions = descriptionQR.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    int middleOfTheElement = 450;
                    int lineSpacing = 50;

                    zplToSend = "^XA" +
                                "^FO250,50" +
                                $"^BQ,2,{sizeQR}" +
                                $"^FDHA,{secretQR}^FS";

                    for (int i = 0; i < descriptions.Length; i++)
                    {
                        zplToSend += $"^FO200,{middleOfTheElement + (i * lineSpacing)}" +
                                     $"^A0N,{textSize},{textSize}" +
                                     $"^FD" +
                                     $"{descriptions[i]}" +
                                     $"^FS";
                    }

                    zplToSend += "^XZ";
                }

                Console.WriteLine("Печать на принтере: " + tpsIP + " Код переданный на принтер " + zplToSend);

                // using (TcpClient client = new TcpClient())
                // {
                //     _logger.LogInformation("Attempting to connect to printer at {tpsIP}:{Port}", tpsIP, port);
                //     client.Connect(tpsIP, port);
                //     _logger.LogInformation("Successfully connected to printer.");
                //
                //     // Write ZPL String to connection
                //     using (System.IO.StreamWriter
                //            writer = new System.IO.StreamWriter(client.GetStream(), Encoding.UTF8))
                //     {
                //         writer.Write(zplToSend);
                //         writer.Flush();
                //     }
                // }

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