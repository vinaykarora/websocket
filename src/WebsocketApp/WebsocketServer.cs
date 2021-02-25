using RawPrint;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace WebsocketApp
{
    public class WebsocketServer
    {
        public event OnLogMessage LogMessage;
        public delegate void OnLogMessage(object sender, LogMessageEventArgs e);
        public class LogMessageEventArgs : EventArgs
        {
            public string Message { get; set; }
            public LogMessageEventArgs(string Message)
            {
                this.Message = Message;
            }
        }

        public bool started = false;
        public async void Start(string httpListenerPrefix)
        {
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add(httpListenerPrefix);
            httpListener.Start();
            LogMessage(this, new LogMessageEventArgs("Listening..."));
            started = true;

            while (started)
            {
                HttpListenerContext httpListenerContext = await httpListener.GetContextAsync();
                if (httpListenerContext.Request.IsWebSocketRequest)
                {
                    ProcessRequest(httpListenerContext);
                }
                else
                {
                    httpListenerContext.Response.StatusCode = 400;
                    httpListenerContext.Response.Close();
                    LogMessage(this, new LogMessageEventArgs("Closed..."));
                }
            }
        }

        public void Stop()
        {
            started = false;
        }
        private async void ProcessRequest(HttpListenerContext httpListenerContext)
        {
            WebSocketContext webSocketContext = null;

            try
            {
                webSocketContext = await httpListenerContext.AcceptWebSocketAsync(subProtocol: null);
                LogMessage(this, new LogMessageEventArgs("Connected"));
            }
            catch (Exception e)
            {
                httpListenerContext.Response.StatusCode = 500;
                httpListenerContext.Response.Close();
                LogMessage(this, new LogMessageEventArgs(string.Format("Exception: {0}", e)));
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;
            try
            {


                while (webSocket.State == WebSocketState.Open)
                {

                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);

                    WebSocketReceiveResult result = null;

                    using var ms = new System.IO.MemoryStream();
                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, System.IO.SeekOrigin.Begin);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using var reader = new System.IO.StreamReader(ms, Encoding.UTF8);
                        var requestJson = Encoding.UTF8.GetString(ms.ToArray());
                        var printRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<PrintRequest>(requestJson);
                        bool valid = true;
                        byte[] toBytes = Encoding.UTF8.GetBytes("");

                        if (printRequest == null)
                        {
                            toBytes = Encoding.UTF8.GetBytes("Error...");
                            await webSocket.SendAsync(new ArraySegment<byte>(toBytes, 0, int.Parse(toBytes.Length.ToString())), WebSocketMessageType.Binary, result.EndOfMessage, CancellationToken.None);
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(printRequest.name))
                            {
                                var printers = "";
                                foreach (var imp in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                                {
                                    printers += imp + "\n";
                                }

                                toBytes = Encoding.UTF8.GetBytes("Printer Not Listed \n Available Printers are: " + printers);
                                valid = false;
                            }
                            if (printRequest.printQty<=0)
                            {
                                toBytes = Encoding.UTF8.GetBytes("The print quantity of the document was not indicated.");
                                valid = false;
                            }
                            if (string.IsNullOrWhiteSpace(printRequest.qrtext))
                            {
                                toBytes = Encoding.UTF8.GetBytes("There is no data to generate QR code.");
                                valid = false;
                            }


                            if (valid)
                            {
                                byte[] qrCodeBytes = Barcoder.GeneratorQR(printRequest.qrtext);
                                using System.IO.MemoryStream stream = new System.IO.MemoryStream();
                                stream.Write(qrCodeBytes, 0, qrCodeBytes.Length);
                                
                                IPrinter printer = new Printer();
                                printer.PrintRawStream(printRequest.name, stream, "Vinay");

                                toBytes = Encoding.UTF8.GetBytes("Correcto...");
                            }

                            await webSocket.SendAsync(new ArraySegment<byte>(toBytes, 0, int.Parse(toBytes.Length.ToString())), WebSocketMessageType.Binary, result.EndOfMessage, CancellationToken.None);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogMessage(this, new LogMessageEventArgs(string.Format("Exception: {0} \nLinea:{1}", e, e.StackTrace)));
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();
            }
        }
    }
}