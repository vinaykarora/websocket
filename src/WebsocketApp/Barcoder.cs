using QRCoder;

namespace WebsocketApp
{
    public class Barcoder
    {
        public static byte[] GeneratorQR(string qrText)
        {
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            PdfByteQRCode qrCode = new PdfByteQRCode(qrCodeData);
            byte[] qrCodeAsPdfByteArr = qrCode.GetGraphic(20);
            return qrCodeAsPdfByteArr;
        }
    }
}
