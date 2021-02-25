namespace WebsocketApp
{
    public class PrintRequest
    {
        public int printQty { get; set; } = 1;
        public string qrtext { get; set; }
        public string name { get; set; } = "";
    }
}