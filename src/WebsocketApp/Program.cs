using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace WebsocketApp
{
    class Program
    {
        public static WebsocketServer ws;
        static void Main(string[] args)
        {
            ws = new WebsocketServer();
            ws.LogMessage += Ws_LogMessage;
            ws.Start("http://localhost:2645/service/");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void Ws_LogMessage(object sender, WebsocketServer.LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public class WebsocketServer
    {
        public event OnLogMessage LogMessage;
        public delegate void OnLogMessage(Object sender, LogMessageEventArgs e);
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
                LogMessage(this, new LogMessageEventArgs(String.Format("Exception: {0}", e)));
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;
            try
            {


                while (webSocket.State == WebSocketState.Open)
                {

                    ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);

                    WebSocketReceiveResult result = null;

                    using (var ms = new System.IO.MemoryStream())
                    {
                        do
                        {
                            result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                        while (!result.EndOfMessage);

                        ms.Seek(0, System.IO.SeekOrigin.Begin);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            using (var reader = new System.IO.StreamReader(ms, Encoding.UTF8))
                            {
                                var r = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                                var t = Newtonsoft.Json.JsonConvert.DeserializeObject<Datos>(r);
                                bool valid = true;
                                byte[] toBytes = Encoding.UTF8.GetBytes(""); ;

                                if (t != null)
                                {
                                    if (t.printer.Trim() == string.Empty)
                                    {
                                        var printers = "";
                                        foreach (var imp in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                                        {
                                            printers += imp + "\n";
                                        }

                                        toBytes = Encoding.UTF8.GetBytes("Printer Not Listed \n Available Printers are: " + printers);
                                        valid = false;
                                    }
                                    if (t.name.Trim() == string.Empty)
                                    {
                                        toBytes = Encoding.UTF8.GetBytes("The name of the Document was not indicated");
                                        valid = false;
                                    }
                                    if (t.code == null)
                                    {
                                        toBytes = Encoding.UTF8.GetBytes("There is no data to send to the Printer");
                                        valid = false;
                                    }


                                    if (valid)
                                    {
                                        print.RawPrinter.SendStringToPrinter(t.printer, t.code, t.name);
                                        toBytes = Encoding.UTF8.GetBytes("Correcto...");
                                    }

                                    await webSocket.SendAsync(new ArraySegment<byte>(toBytes, 0, int.Parse(toBytes.Length.ToString())), WebSocketMessageType.Binary, result.EndOfMessage, CancellationToken.None);
                                }
                                else
                                {
                                    toBytes = Encoding.UTF8.GetBytes("Error...");
                                    await webSocket.SendAsync(new ArraySegment<byte>(toBytes, 0, int.Parse(toBytes.Length.ToString())), WebSocketMessageType.Binary, result.EndOfMessage, CancellationToken.None);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogMessage(this, new LogMessageEventArgs(String.Format("Exception: {0} \nLinea:{1}", e, e.StackTrace)));
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();
            }
        }
    }

    public class Datos
    {
        public string name { get; set; }
        public string code { get; set; }
        public string printer { get; set; } = "";
    }
}