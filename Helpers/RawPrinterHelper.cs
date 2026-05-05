using System;
using System.Runtime.InteropServices;
using System.Text;

namespace XpertPharm5Donation.Helpers
{
    /// <summary>
    /// Sends raw TSPL commands directly to thermal printers via Windows printer spooler.
    /// Supports 203 DPI Xprinter thermal printers (8 dots/mm).
    /// </summary>
    public class RawPrinterHelper
    {
        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool OpenPrinterA(string szPrinter, out IntPtr hPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartDocPrinterA(IntPtr hPrinter, int level, ref DOCINFOA di);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, uint dwCount, out uint dwWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

        private const uint GMEM_MOVEABLE = 0x0002;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        /// <summary>
        /// Sends raw TSPL command string to the specified printer.
        /// </summary>
        /// <param name="printerName">Printer name (e.g., "Xprinter").</param>
        /// <param name="tsplCommands">Raw TSPL command string.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool SendRawTSPL(string printerName, string tsplCommands)
        {
            if (string.IsNullOrWhiteSpace(printerName) || string.IsNullOrWhiteSpace(tsplCommands))
                return false;

            IntPtr hPrinter = IntPtr.Zero;

            try
            {
                if (!OpenPrinterA(printerName, out hPrinter, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"OpenPrinterA failed. Error: {error}");
                }

                byte[] data = Encoding.ASCII.GetBytes(tsplCommands);
                IntPtr unmanagedBuffer = GlobalAlloc(GMEM_MOVEABLE, new UIntPtr((uint)data.Length));

                if (unmanagedBuffer == IntPtr.Zero)
                    throw new Exception("GlobalAlloc failed.");

                IntPtr pData = GlobalLock(unmanagedBuffer);

                if (pData == IntPtr.Zero)
                    throw new Exception("GlobalLock failed.");

                try
                {
                    Marshal.Copy(data, 0, pData, data.Length);

                    DOCINFOA di = new DOCINFOA
                    {
                        pDocName = "TSPL Label",
                        pDataType = "RAW"
                    };

                    if (!StartDocPrinterA(hPrinter, 1, ref di))
                        throw new Exception($"StartDocPrinterA failed. Error: {Marshal.GetLastWin32Error()}");

                    if (!StartPagePrinter(hPrinter))
                        throw new Exception($"StartPagePrinter failed. Error: {Marshal.GetLastWin32Error()}");

                    if (!WritePrinter(hPrinter, pData, (uint)data.Length, out uint dwWritten))
                        throw new Exception($"WritePrinter failed. Error: {Marshal.GetLastWin32Error()}");

                    if (!EndPagePrinter(hPrinter))
                        throw new Exception($"EndPagePrinter failed. Error: {Marshal.GetLastWin32Error()}");

                    if (!EndDocPrinter(hPrinter))
                        throw new Exception($"EndDocPrinter failed. Error: {Marshal.GetLastWin32Error()}");

                    return true;
                }
                finally
                {
                    GlobalUnlock(pData);
                    GlobalFree(unmanagedBuffer);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RawPrinterHelper Error: {ex.Message}");
                throw;
            }
            finally
            {
                if (hPrinter != IntPtr.Zero)
                    ClosePrinter(hPrinter);
            }
        }

        /// <summary>
        /// Gets list of available printers on the system.
        /// </summary>
        public static string[] GetPrinterNames()
        {
            var printerNames = System.Drawing.Printing.PrinterSettings.InstalledPrinters;
            return new System.Collections.Generic.List<string>(printerNames).ToArray();
        }
    }
}
