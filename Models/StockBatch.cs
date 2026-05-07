using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace XDonation.Models
{
    public class StockBatch
    {
        [Key]
        public int Id { get; set; }

        public int DrugId { get; set; }
        [ForeignKey(nameof(DrugId))]
        public Drug Drug { get; set; } = null!;

        [MaxLength(100)]
        public string? BatchNumber { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public int InitialQuantity { get; set; }

        public bool IsBlocked { get; set; }

        [MaxLength(100)]
        public string? Store { get; set; }

        public bool IsPsychotrope { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Dispensation> Dispensations { get; set; } = [];

        // Computed
        [NotMapped]
        public int QuantityUsed => Dispensations?.Sum(d => d.Quantity) ?? 0;

        [NotMapped]
        public int QuantityRemaining => InitialQuantity - QuantityUsed;

        [NotMapped]
        public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value.Date < DateTime.Today;

        [NotMapped]
        public bool IsExpiringSoon => ExpirationDate.HasValue
            && ExpirationDate.Value.Date >= DateTime.Today
            && ExpirationDate.Value.Date <= DateTime.Today.AddDays(90);

        public override string ToString() => BatchNumber ?? "Nouveau Lot";
    }
}
