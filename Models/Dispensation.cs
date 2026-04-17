using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XpertPharm5Donation.Models
{
    public class Dispensation
    {
        [Key]
        public int Id { get; set; }

        public int StockBatchId { get; set; }
        [ForeignKey(nameof(StockBatchId))]
        public StockBatch StockBatch { get; set; } = null!;

        public int Quantity { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public Guid SessionId { get; set; }
    }
}
