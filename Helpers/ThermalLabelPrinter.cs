using System;
using System.Text;

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

            StringBuilder tspl = new StringBuilder();

            // Setup
            tspl.AppendLine("SIZE 40 mm, 20 mm");
            tspl.AppendLine("GAP 3 mm, 0 mm");
            tspl.AppendLine("DIRECTION 0,0"); 
            tspl.AppendLine("REFERENCE 0,0");
            tspl.AppendLine("CLS");

            // --- COMPACT MAIN CONTENT AREA ---

            // 1. Header - Tight to top
            int headerX = GetCenterX(header, 12, CONTENT_WIDTH);
            tspl.AppendLine($"TEXT {headerX},5,\"3\",0,1,1,\"{TsplEscape(header)}\"");

            // 2. Barcode - Reduced height for compactness
            int barcodeX = GetCenterX(barcode, 16, CONTENT_WIDTH); 
            tspl.AppendLine($"BARCODE {barcodeX},28,\"128M\",35,0,0,2,2,\"{TsplEscape(barcode)}\"");

            // 3. Barcode Number - Moved up
            int numX = GetCenterX(barcode, 16, CONTENT_WIDTH);
            tspl.AppendLine($"TEXT {numX},68,\"4\",0,1,1,\"{TsplEscape(barcode)}\"");

            // 4. Product Name - Tightened Y-offset
            string displayProduct = TruncateProduct(product, 20);
            tspl.AppendLine($"TEXT 10,102,\"2\",0,1,1,\"{TsplEscape(displayProduct)}\"");

            // 5. Price / GRATUIT - Font 4 (Bold & Compact)
            string priceText = isFree ? "GRATUIT" : (string.IsNullOrWhiteSpace(price) ? "GRATUIT" : price);
            int priceX = GetCenterX(priceText, 16, CONTENT_WIDTH);
            tspl.AppendLine($"TEXT {priceX},122,\"4\",0,1,1,\"{TsplEscape(priceText)}\"");
            tspl.AppendLine($"TEXT {priceX + 1},122,\"4\",0,1,1,\"{TsplEscape(priceText)}\""); // Bold effect


            // --- VERTICAL META INFO (Right Side) ---
            tspl.AppendLine($"TEXT 285,10,\"2\",90,1,1,\"Exp : {TsplEscape(exp)}\"");
            tspl.AppendLine($"TEXT 310,10,\"2\",90,1,1,\"Lot : {TsplEscape(lot)}\"");

            tspl.AppendLine("PRINT 1,1");

            try
            {
                RawPrinterHelper.SendRawTSPL(_printerName, tspl.ToString());
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

        private string TsplEscape(string text)
        {
            return text?.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ") ?? "";
        }
    }
}
