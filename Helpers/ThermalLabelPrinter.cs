using System;
using System.Text;

namespace XpertPharm5Donation.Helpers
{
    /// <summary>
    /// Production-ready thermal label printer for Xprinter 203 DPI printers.
    /// Generates TSPL commands for pixel-perfect pharmacy barcode labels.
    /// 
    /// Hardware Spec:
    /// - Resolution: 203 DPI (8 dots/mm)
    /// - Label: 40mm × 20mm = 320 dots × 160 dots (landscape)
    /// </summary>
    public class ThermalLabelPrinter
    {
        // Physical dimensions in dots (203 DPI = 8 dots/mm)
        // 40mm × 20mm label (landscape)
        private const int LABEL_WIDTH_MM = 40;
        private const int LABEL_HEIGHT_MM = 20;
        private const int DPI = 203;
        private const int DOTS_PER_MM = 8; // 203 DPI = 8 dots/mm

        private const int LABEL_WIDTH_DOTS = 320;   // 40mm * 8 dots/mm
        private const int LABEL_HEIGHT_DOTS = 160;  // 20mm * 8 dots/mm

        // Spacing and positioning (in dots)
        private const int MARGIN_TOP = 5;
        private const int MARGIN_LEFT = 4;
        private const int MARGIN_RIGHT = 4;
        private const int MARGIN_BOTTOM = 5;

        // Barcode dimensions
        private const int BARCODE_HEIGHT = 32; // height in dots
        private const int BARCODE_WIDTH_MULTIPLIER = 2; // width multiplier for Code128

        private readonly string _printerName;

        public ThermalLabelPrinter(string printerName = "Xprinter")
        {
            _printerName = printerName;
        }

        /// <summary>
        /// Prints a pharmacy barcode label with all details.
        /// </summary>
        /// <param name="product">Product name (e.g., "FLAZOL 500MG")</param>
        /// <param name="barcode">Barcode number to encode (Code128)</param>
        /// <param name="lot">Lot number (e.g., "2222")</param>
        /// <param name="exp">Expiry date (e.g., "30-04-2027")</param>
        /// <param name="header">Header text (e.g., "PHARMACIE ARAB")</param>
        /// <param name="isFree">If true, displays "GRATUIT"; otherwise displays price from product data</param>
        /// <param name="price">Price text to display (e.g., "100 DA"). Ignored if isFree is true.</param>
        public void PrintLabel(string product, string barcode, string lot, string exp, 
                              string header, bool isFree, string price = "")
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentException("Barcode cannot be empty", nameof(barcode));

            if (string.IsNullOrWhiteSpace(header))
                header = "PHARMACIE";

            // Build TSPL command string
            StringBuilder tspl = new StringBuilder();

            // ===== Setup =====
            tspl.AppendLine("SIZE 40mm, 20mm");          // Physical label size
            tspl.AppendLine("GAP 0mm, 0mm");             // Gap between labels
            tspl.AppendLine("DIRECTION 1");               // Landscape orientation
            tspl.AppendLine("SPEED 4");                   // Print speed (1-5)
            tspl.AppendLine("DENSITY 8");                 // Darkness level
            tspl.AppendLine("CLS");                       // Clear buffer

            // ===== Header (Pharmacy Name) =====
            if (!string.IsNullOrWhiteSpace(header))
            {
                int headerX = CenterX(header, fontWidth: 14);
                tspl.AppendLine($"TEXT {headerX},6,\"0\",0,3,3,\"{TsplEscape(header)}\"");
            }

            // ===== Barcode (Code128) =====
            int barcodeY = 36;
            int barcodeX = CenterBarcode(barcode, maxWidth: 220, rightBoundary: 238);
            
            tspl.AppendLine($"BARCODE {barcodeX},{barcodeY},\"128\",{BARCODE_HEIGHT},0,0,2,2,\"{TsplEscape(barcode)}\"");

            // ===== Barcode Number (Human Readable) =====
            int numberY = barcodeY + BARCODE_HEIGHT + 5;
            int numberX = CenterX(barcode, fontWidth: 16, rightBoundary: 238);
            tspl.AppendLine($"TEXT {numberX},{numberY},\"0\",0,3,3,\"{TsplEscape(barcode)}\"");

            // ===== Product Name =====
            int productY = 101;
            string displayProduct = TruncateProduct(product, maxChars: 22);
            tspl.AppendLine($"TEXT {MARGIN_LEFT},{productY},\"0\",0,2,2,\"{TsplEscape(displayProduct)}\"");

            // ===== Price / Free Label =====
            int priceY = 116;
            string priceText = isFree ? "GRATUIT" : (string.IsNullOrWhiteSpace(price) ? "GRATUIT" : price);
            int priceX = CenterX(priceText, fontWidth: 20, rightBoundary: 238);
            tspl.AppendLine($"TEXT {priceX},{priceY},\"0\",0,4,4,\"{TsplEscape(priceText)}\"");

            // ===== Lot and Expiry (Bottom of label) =====
            string expText = $"Exp : {TsplEscape(exp)}";
            tspl.AppendLine($"TEXT 276,152,\"0\",270,2,2,\"{expText}\"");

            string lotText = $"Lot : {TsplEscape(lot)}";
            tspl.AppendLine($"TEXT 314,152,\"0\",270,2,2,\"{lotText}\"");

            // ===== Print =====
            tspl.AppendLine("PRINT 1,1"); // Print 1 label, 1 set

            // Send to printer
            try
            {
                bool success = RawPrinterHelper.SendRawTSPL(_printerName, tspl.ToString());
                if (!success)
                    throw new Exception("Failed to send TSPL commands to printer");
            }
            catch (Exception ex)
            {
                throw new Exception($"Thermal printer error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates X position to center text horizontally on the label.
        /// </summary>
        private int CenterX(string text, int fontWidth = 6, int? rightBoundary = null)
        {
            if (string.IsNullOrEmpty(text))
                return MARGIN_LEFT;

            // Approximate text width based on character count and font width
            int textWidth = text.Length * fontWidth;
            int availableWidth = (rightBoundary ?? LABEL_WIDTH_DOTS - MARGIN_RIGHT) - MARGIN_LEFT;
            int x = MARGIN_LEFT + (availableWidth - textWidth) / 2;

            return Math.Max(MARGIN_LEFT, x);
        }

        /// <summary>
        /// Calculates X position to center barcode on the label.
        /// </summary>
        private int CenterBarcode(string barcode, int maxWidth, int rightBoundary)
        {
            int barcodeWidth = Math.Min(maxWidth, barcode.Length * 18);
            int availableWidth = rightBoundary - MARGIN_LEFT;
            int x = MARGIN_LEFT + (availableWidth - barcodeWidth) / 2;

            return Math.Max(MARGIN_LEFT, x);
        }

        /// <summary>
        /// Truncates product name if it's too long for the label.
        /// </summary>
        private string TruncateProduct(string product, int maxChars = 25)
        {
            if (string.IsNullOrWhiteSpace(product))
                return "UNKNOWN";

            if (product.Length <= maxChars)
                return product;

            return product.Substring(0, maxChars - 3) + "...";
        }

        /// <summary>
        /// Escapes special characters for TSPL commands.
        /// </summary>
        private string TsplEscape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Replace problematic characters
            text = text.Replace("\"", "\\\"");
            text = text.Replace("\n", " ");
            text = text.Replace("\r", " ");

            return text;
        }
    }
}
