using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace XDonation.Models
{
    public enum VoucherStatus
    {
        Draft = 0,
        Validated = 1
    }

    public class DonationVoucher
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Numéro unique du bon — ex: BON-2026-0001</summary>
        [Required, MaxLength(30)]
        public string VoucherNumber { get; set; } = string.Empty;

        /// <summary>Nom du donneur (conservé pour des raisons de compatibilité base de données)</summary>
        [MaxLength(200)]
        public string DonorName { get; set; } = string.Empty;

        /// <summary>Type de donneur : Particulier, Association, Institution, Autre</summary>
        [MaxLength(100)]
        public string? DonorType { get; set; }

        /// <summary>Date de réception physique des médicaments</summary>
        public DateTime ReceiptDate { get; set; } = DateTime.Today;

        /// <summary>Notes / observations libres</summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        public VoucherStatus Status { get; set; } = VoucherStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ValidatedAt { get; set; }

        // Navigation
        public ICollection<DonationVoucherLine> Lines { get; set; } = [];

        // Computed
        [NotMapped]
        public int TotalLines => Lines.Count;

        [NotMapped]
        public int TotalUnits => Lines.Sum(l => l.Quantity);

        [NotMapped]
        public string StatusLabel => Status == VoucherStatus.Validated ? "Validé" : "Nouveau";

        [NotMapped]
        public string StatusIcon => Status == VoucherStatus.Validated ? "✅" : "📝";
    }
}
