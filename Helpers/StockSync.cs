using System;

namespace XDonation.Helpers
{
    public static class StockSync
    {
        public static event Action? StockChanged;

        public static void NotifyStockChanged()
        {
            StockChanged?.Invoke();
        }
    }
}
