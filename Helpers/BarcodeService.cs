using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Windows.Compatibility;

namespace XDonation.Helpers
{
    public class BarcodeService
    {
        public static BitmapSource? GenerateBarcodeImage(string content, int width = 300, int height = 80)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                var format = BarcodeFormat.CODE_128;
                if ((content.Length == 12 || content.Length == 13) && content.All(char.IsDigit))
                {
                    format = BarcodeFormat.EAN_13;
                }
                else if (content.Length == 8 && content.All(char.IsDigit))
                {
                    format = BarcodeFormat.EAN_8;
                }

                var writer = new BarcodeWriterPixelData
                {
                    Format = format,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = width,
                        Height = height,
                        Margin = 0,
                        PureBarcode = true // Only barcode, no text underneath
                    }
                };

                ZXing.Rendering.PixelData pixelData;
                try
                {
                    pixelData = writer.Write(content);
                }
                catch (ArgumentException)
                {
                    // Fallback to CODE_128 if EAN checksum fails
                    writer.Format = BarcodeFormat.CODE_128;
                    pixelData = writer.Write(content);
                }

                var writeableBitmap = new System.Windows.Media.Imaging.WriteableBitmap(
                    pixelData.Width,
                    pixelData.Height,
                    96,
                    96,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null);

                writeableBitmap.WritePixels(
                    new System.Windows.Int32Rect(0, 0, pixelData.Width, pixelData.Height),
                    pixelData.Pixels,
                    pixelData.Width * 4,
                    0);

                writeableBitmap.Freeze(); // Make cross-thread accessible

                return writeableBitmap;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Barcode Gen Error for '{content}': {ex.Message}");
                return null;
            }
        }
    }
}
