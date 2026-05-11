using System;
using System.Text;
using System.Linq;

namespace XDonation.Helpers
{
    public class ThermalLabelPrinter
    {
        // 40mm = 320 dots. 
        // We use 250 dots for content to ensure a wide horizontal padding before meta info.
        private const int CONTENT_WIDTH = 250; 
        private const int TOTAL_WIDTH = 320;
        private readonly string _printerName;

        public ThermalLabelPrinter(string printerName = "Xprinter XP-233B (Copie 1)")
        {
            _printerName = printerName;
        }

        public void PrintLabel(string product, string barcode, string lot, string exp, 
                              string header, bool isFree, string price = "")
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentException("Barcode cannot be empty", nameof(barcode));

            var builder = new TsplBuilder()
                .Size("40 mm", "20 mm")
                .Gap("2 mm", "0 mm")
                .Density(8)
                .Speed(4)
                .Direction(0, 0)
                .Reference(0, 0)
                .Cls();

            // --- COMPACT MAIN CONTENT AREA ---

            // 1. Header - Tight to top
            int headerX = GetCenterX(header, 12, CONTENT_WIDTH);
            builder.Text(headerX, 5, "3", 0, 1, 1, header);

            // 2. Barcode - Reduced height for compactness
            string barcodeType = "128M";
            if ((barcode.Length == 12 || barcode.Length == 13) && barcode.All(char.IsDigit) && IsValidEanChecksum(barcode))
            {
                barcodeType = "EAN13";
            }
            else if (barcode.Length == 8 && barcode.All(char.IsDigit) && IsValidEanChecksum(barcode))
            {
                barcodeType = "EAN8";
            }

            int barcodeX = GetCenterX(barcode, 16, CONTENT_WIDTH); 
            builder.Barcode(barcodeX, 28, barcodeType, 35, 0, 0, 2, 2, barcode);

            // 3. Barcode Number - Moved up
            int numX = GetCenterX(barcode, 16, CONTENT_WIDTH);
            builder.Text(numX, 68, "4", 0, 1, 1, barcode);

            // 4. Product Name - Tightened Y-offset
            string displayProduct = TruncateProduct(product, 20);
            builder.Text(10, 102, "2", 0, 1, 1, displayProduct);

            // 5. Price / GRATUIT - Font 4 (Bold & Compact)
            string priceText = isFree ? "GRATUIT" : (string.IsNullOrWhiteSpace(price) ? "GRATUIT" : price);
            int priceX = GetCenterX(priceText, 16, CONTENT_WIDTH);
            builder.Text(priceX, 122, "4", 0, 1, 1, priceText);
            builder.Text(priceX + 1, 122, "4", 0, 1, 1, priceText); // Bold effect

            // --- VERTICAL META INFO (Right Side) ---
            builder.Text(285, 10, "2", 90, 1, 1, $"Exp : {exp}");
            builder.Text(310, 10, "2", 90, 1, 1, $"Lot : {lot}");

            builder.Print(1, 1);

            try
            {
                RawPrinterHelper.SendRawTSPL(_printerName, builder.Build());
            }
            catch (Exception ex)
            {
                throw new Exception($"Printer Error: {ex.Message}");
            }
        }

        private int GetCenterX(string text, int charWidth, int boundaryWidth)
        {
            if (string.IsNullOrEmpty(text)) return 10;
            int textWidth = text.Length * charWidth;
            return Math.Max(5, (boundaryWidth - textWidth) / 2);
        }

        private string TruncateProduct(string product, int max)
        {
            if (string.IsNullOrWhiteSpace(product)) return "N/A";
            return product.Length <= max ? product : product.Substring(0, max - 3) + "...";
        }

        private bool IsValidEanChecksum(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode) || !barcode.All(char.IsDigit)) return false;
            
            // UPC-A (12) or EAN-13 (13)
            if (barcode.Length == 12 || barcode.Length == 13)
            {
                // Pad 12 digit UPC-A to 13 digits for calculation
                string code = barcode.Length == 12 ? "0" + barcode : barcode;
                int sum = 0;
                for (int i = 0; i < 12; i++)
                {
                    int digit = code[i] - '0';
                    sum += (i % 2 == 0) ? digit : digit * 3;
                }
                int checkDigit = (10 - (sum % 10)) % 10;
                return checkDigit == (code[12] - '0');
            }
            // EAN-8
            else if (barcode.Length == 8)
            {
                int sum = 0;
                for (int i = 0; i < 7; i++)
                {
                    int digit = barcode[i] - '0';
                    sum += (i % 2 == 0) ? digit * 3 : digit;
                }
                int checkDigit = (10 - (sum % 10)) % 10;
                return checkDigit == (barcode[7] - '0');
            }
            
            return false;
        }
    }
}
