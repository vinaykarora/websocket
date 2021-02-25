using System;

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
}