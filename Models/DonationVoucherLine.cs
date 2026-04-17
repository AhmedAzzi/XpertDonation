using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XpertPharm5Donation.Models
{
    public class DonationVoucherLine
    {
        [Key]
        public int Id { get; set; }

        // FK → Bon de don
        public int DonationVoucherId { get; set; }
        [ForeignKey(nameof(DonationVoucherId))]
        public DonationVoucher DonationVoucher { get; set; } = null!;

        // FK → Drug (optionnel — on peut saisir un médicament non catalogué)
        public int? DrugId { get; set; }
        [ForeignKey(nameof(DrugId))]
        public Drug? Drug { get; set; }

        /// <summary>Nom du médicament saisi (copie dénormalisée)</summary>
        [Required, MaxLength(300)]
        public string DrugName { get; set; } = string.Empty;

        /// <summary>DCI saisie sur la ligne</summary>
        [MaxLength(300)]
        public string? Dci { get; set; }

        /// <summary>Code-barres du conditionnement</summary>
        [MaxLength(100)]
        public string? Barcode { get; set; }

        /// <summary>Numéro de lot du fabricant</summary>
        [MaxLength(100)]
        public string? BatchNumber { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public int Quantity { get; set; }

        /// <summary>Observations sur cette ligne</summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>Référence au StockBatch créé lors de la validation</summary>
        public int? StockBatchId { get; set; }
        [ForeignKey(nameof(StockBatchId))]
        public StockBatch? StockBatch { get; set; }

        // Computed
        [NotMapped]
        public string ExpirationLabel => ExpirationDate.HasValue
            ? ExpirationDate.Value.ToString("MM/yyyy")
            : "—";

        [NotMapped]
        public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value.Date < DateTime.Today;
    }
}
