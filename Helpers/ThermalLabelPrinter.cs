using System;
using System.Linq;

namespace XDonation.Helpers
{
    public class ThermalLabelPrinter
    {
        private readonly string _printerName;

        public ThermalLabelPrinter(string printerName = "Xprinter XP-233B (Copie 1)")
        {
            _printerName = printerName;
        }

        public void PrintLabel(
            string product,
            string barcode,
            string lot,
            string exp,
            string header,
            bool isFree,
            string price = "")
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentException("Barcode cannot be empty", nameof(barcode));

            var builder = new TsplBuilder()
                .Size("40 mm", "20 mm")
                .Gap("3 mm", "0 mm")
                .Density(8)
                .Speed(4)
                .Direction(0, 0)
                .Reference(0, 0)
                .Cls();

            // =========================
            // HEADER
            // =========================
            builder.Text(
                90,     // X
                5,      // Y  (was 2)
                "1",    // FONT
                0,      // ROTATION
                1,      // XMULT
                1,      // YMULT
                header
            );

            // =========================
            // BARCODE TYPE
            // =========================
            string barcodeType = "128M";

            if ((barcode.Length == 12 || barcode.Length == 13) &&
                barcode.All(char.IsDigit) &&
                IsValidEanChecksum(barcode))
            {
                barcodeType = "EAN13";
            }
            else if (barcode.Length == 8 &&
                     barcode.All(char.IsDigit) &&
                     IsValidEanChecksum(barcode))
            {
                barcodeType = "EAN8";
            }

            // =========================
            // BARCODE
            // =========================
            builder.Barcode(
                20,             // X
                20,             // Y  (was 20)
                barcodeType,    // TYPE
                35,             // HEIGHT
                0,              // HUMAN READABLE
                0,              // ROTATION
                2,              // NARROW
                2,              // WIDE
                barcode         // DATA
            );

            // =========================
            // BARCODE NUMBER
            // =========================
            builder.Text(
                100,
                65,     // Y  (was 62)
                "2",
                0,
                1,
                1,
                barcode
            );

            // =========================
            // PRODUCT
            // =========================
            string displayProduct = TruncateProduct(product, 28);

            builder.Text(
                30,
                91,     // Y  (was 88)
                "1",
                0,
                1,
                1,
                displayProduct
            );

            // =========================
            // PRICE
            // =========================
            string priceText = isFree
                ? "GRATUIT"
                : (string.IsNullOrWhiteSpace(price)
                    ? "GRATUIT"
                    : price);

            builder.Text(
                95,
                123,    // Y  (was 120)
                "2",
                0,
                1,
                1,
                priceText
            );

            // =========================
            // VERTICAL INFO
            // =========================
            builder.Text(
                295,
                8,      // Y  (was 5)
                "1",
                90,
                1,
                1,
                $"EXP:{exp}"
            );

            builder.Text(
                315,
                8,      // Y  (was 5)
                "1",
                90,
                1,

1,
                $"LOT:{lot}"
            );

            // =========================
            // PRINT
            // =========================
            builder.Print(1);

            try
            {
                RawPrinterHelper.SendRawTSPL(
                    _printerName,
                    builder.Build()
                );
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Printer Error: {ex.Message}"
                );
            }
        }

        private string TruncateProduct(string product, int max)
        {
            if (string.IsNullOrWhiteSpace(product))
                return "N/A";

            return product.Length <= max
                ? product
                : product.Substring(0, max - 3) + "...";
        }

        private bool IsValidEanChecksum(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode) ||
                !barcode.All(char.IsDigit))
                return false;

            // EAN13 / UPC
            if (barcode.Length == 12 || barcode.Length == 13)
            {
                string code = barcode.Length == 12
                    ? "0" + barcode
                    : barcode;

                int sum = 0;

                for (int i = 0; i < 12; i++)
                {
                    int digit = code[i] - '0';

                    sum += (i % 2 == 0)
                        ? digit
                        : digit * 3;
                }

                int checkDigit =
                    (10 - (sum % 10)) % 10;

                return checkDigit ==
                       (code[12] - '0');
            }

            // EAN8
            if (barcode.Length == 8)
            {
                int sum = 0;

                for (int i = 0; i < 7; i++)
                {
                    int digit = barcode[i] - '0';

                    sum += (i % 2 == 0)
                        ? digit * 3
                        : digit;
                }

                int checkDigit =
                    (10 - (sum % 10)) % 10;

                return checkDigit ==
                       (barcode[7] - '0');
            }

            return false;
        }
    }
}