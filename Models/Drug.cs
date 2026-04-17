using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace XpertPharm5Donation.Models
{
    public class Drug
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Dénomination Commune Internationale (DCI)</summary>
        [MaxLength(300)]
        public string? Dci { get; set; }

        /// <summary>Forme galénique : comprimé, gélule, sirop…</summary>
        [MaxLength(100)]
        public string? Form { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<StockBatch> StockBatches { get; set; } = [];
        public ICollection<DonationVoucherLine> VoucherLines { get; set; } = [];

        // Computed properties
        [NotMapped]
        public int TotalAvailableStock => StockBatches.Sum(b => b.QuantityRemaining);

        [NotMapped]
        public int TotalValidStock => StockBatches.Where(b => !b.IsExpired).Sum(b => b.QuantityRemaining);

        [NotMapped]
        public bool HasExpiredBatches => StockBatches.Any(b => b.IsExpired && b.QuantityRemaining > 0);

        [NotMapped]
        public string DisplayName => string.IsNullOrWhiteSpace(Dci) ? Name : $"{Name}  [{Dci}]";

        public override string ToString() => Name;
    }
}
