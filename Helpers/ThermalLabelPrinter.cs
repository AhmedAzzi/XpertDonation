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