using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.ImageSharp;
using System.IO;

namespace DrugTracker.Services
{
    public interface IQrCodeService
    {
        byte[] GenerateQrCode(string content);
        string? DecodeQrCode(Stream imageStream);
    }

    public class QrCodeService : IQrCodeService
    {
        public byte[] GenerateQrCode(string content)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q))
            using (BitmapByteQRCode qrCode = new BitmapByteQRCode(qrCodeData))
            {
                return qrCode.GetGraphic(20);
            }
        }

        public string? DecodeQrCode(Stream imageStream)
        {
            try
            {
                using (var image = Image.Load<Rgba32>(imageStream))
                {
                    var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>();
                    var result = reader.Decode(image);
                    return result?.Text;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
