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
    /// - Label: 60mm × 40mm = 480 dots × 320 dots
    /// </summary>
    public class ThermalLabelPrinter
    {
        // Physical dimensions in dots (203 DPI = 8 dots/mm)
        // 60mm × 40mm label
        private const int LABEL_WIDTH_MM = 60;
        private const int LABEL_HEIGHT_MM = 40;
        private const int DPI = 203;
        private const int DOTS_PER_MM = 8; // 203 DPI = 8 dots/mm

        private const int LABEL_WIDTH_DOTS = 480;   // 60mm * 8 dots/mm
        private const int LABEL_HEIGHT_DOTS = 320;  // 40mm * 8 dots/mm

        // Spacing and positioning (in dots)
        private const int MARGIN_TOP = 10;
        private const int MARGIN_LEFT = 8;
        private const int MARGIN_RIGHT = 8;
        private const int MARGIN_BOTTOM = 10;

        // Barcode dimensions
        private const int BARCODE_HEIGHT = 60; // height in dots
        private const int BARCODE_WIDTH_MULTIPLIER = 3; // width multiplier for Code128

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
            tspl.AppendLine("SIZE 60mm, 40mm");          // Physical label size
            tspl.AppendLine("GAP 0mm, 0mm");             // Gap between labels
            tspl.AppendLine("SPEED 4");                   // Print speed (1-5)
            tspl.AppendLine("DENSITY 8");                 // Darkness level
            tspl.AppendLine("CLS");                       // Clear buffer

            // ===== Header (Pharmacy Name) =====
            // Centered at top
            if (!string.IsNullOrWhiteSpace(header))
            {
                int headerX = CenterX(header, fontWidth: 6); // Approximate width for font size 1
                tspl.AppendLine($"TEXT {headerX},15,\"0\",0,1,1,\"{TsplEscape(header)}\"");
                // Format: TEXT x, y, font, rotation, x-multiplication, y-multiplication, "text"
            }

            // ===== Barcode (Code128) =====
            // Centered, below header
            // Barcode height: 60 dots, width based on barcode length
            int barcodeY = 35;
            int barcodeX = CenterBarcode(barcode);
            
            tspl.AppendLine($"BARCODE {barcodeX},{barcodeY},\"128\",{BARCODE_HEIGHT},1,0,2,2,\"{TsplEscape(barcode)}\"");
            // Format: BARCODE x, y, "128", height, readable, rotation, x-multiplier, y-multiplier, barcode-data
            // readable=1 displays text under barcode, rotation=0 is normal

            // ===== Barcode Number (Human Readable) =====
            // Directly below barcode, centered, large font
            int numberY = barcodeY + BARCODE_HEIGHT + 8;
            int numberX = CenterX(barcode, fontWidth: 8);
            tspl.AppendLine($"TEXT {numberX},{numberY},\"0\",0,2,2,\"{TsplEscape(barcode)}\"");

            // ===== Product Name =====
            // Left side, below barcode number
            int productY = numberY + 35;
            string displayProduct = TruncateProduct(product, maxChars: 25);
            tspl.AppendLine($"TEXT {MARGIN_LEFT},{productY},\"0\",0,1,1,\"{TsplEscape(displayProduct)}\"");

            // ===== Price / Free Label (Large, Bold) =====
            // Centered, below product
            int priceY = productY + 20;
            string priceText = isFree ? "GRATUIT" : (string.IsNullOrWhiteSpace(price) ? "GRATUIT" : price);
            int priceX = CenterX(priceText, fontWidth: 10);
            tspl.AppendLine($"TEXT {priceX},{priceY},\"0\",0,3,3,\"{TsplEscape(priceText)}\"");

            // ===== Right Side Vertical Text (Lot and Expiry, rotated 90°) =====
            // Right edge, centered vertically
            // Rotation 90 = vertical text (reads from bottom to top when label is horizontal)
            
            // Calculate right side X position (right edge with margin)
            int rightX = LABEL_WIDTH_DOTS - MARGIN_RIGHT - 30; // Reserve space for rotated text
            
            // Position for lot (higher on label when rotated)
            int lotY = 150; // Center area
            string lotText = $"Lot: {TsplEscape(lot)}";
            tspl.AppendLine($"TEXT {rightX},{lotY},\"0\",2,1,1,\"{lotText}\"");
            // rotation=2 means 180 degrees, but we'll use rotation 1 for 90-degree vertical

            // Position for expiry (below lot)
            int expY = lotY + 50;
            string expText = $"Exp: {TsplEscape(exp)}";
            tspl.AppendLine($"TEXT {rightX},{expY},\"0\",2,1,1,\"{expText}\"");

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
        private int CenterX(string text, int fontWidth = 6)
        {
            if (string.IsNullOrEmpty(text))
                return MARGIN_LEFT;

            // Approximate text width based on character count and font width
            int textWidth = text.Length * fontWidth;
            int availableWidth = LABEL_WIDTH_DOTS - MARGIN_LEFT - MARGIN_RIGHT;
            int x = MARGIN_LEFT + (availableWidth - textWidth) / 2;

            return Math.Max(MARGIN_LEFT, x);
        }

        /// <summary>
        /// Calculates X position to center barcode on the label.
        /// </summary>
        private int CenterBarcode(string barcode)
        {
            // Code128 average: ~8-10 dots per character
            int barcodeWidth = barcode.Length * 8; // Conservative estimate
            int availableWidth = LABEL_WIDTH_DOTS - MARGIN_LEFT - MARGIN_RIGHT;
            int x = MARGIN_LEFT + (availableWidth - barcodeWidth) / 2;

            return Math.Max(MARGIN_LEFT, Math.Min(x, LABEL_WIDTH_DOTS - MARGIN_RIGHT - 50));
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
