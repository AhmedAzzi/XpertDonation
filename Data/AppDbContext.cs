using Microsoft.EntityFrameworkCore;
using XpertPharm5Donation.Models;

namespace XpertPharm5Donation.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Drug> Drugs { get; set; } = null!;
        public DbSet<StockBatch> StockBatches { get; set; } = null!;
        public DbSet<Dispensation> Dispensations { get; set; } = null!;
        public DbSet<DonationVoucher> DonationVouchers { get; set; } = null!;
        public DbSet<DonationVoucherLine> DonationVoucherLines { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Drug ─────────────────────────────────────────────────────────
            modelBuilder.Entity<Drug>(e =>
            {
                e.ToTable("Drugs");
                e.HasKey(d => d.Id);
                e.Property(d => d.Name).IsRequired().HasMaxLength(300);
                e.Property(d => d.Dci).HasMaxLength(300);
                e.Property(d => d.Form).HasMaxLength(100);
                e.Property(d => d.Barcode).HasMaxLength(100);
                e.HasIndex(d => d.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            });

            // ── StockBatch ───────────────────────────────────────────────────
            modelBuilder.Entity<StockBatch>(e =>
            {
                e.ToTable("StockBatches");
                e.HasKey(b => b.Id);
                e.Property(b => b.BatchNumber).HasMaxLength(100);
                e.Property(b => b.Barcode).HasMaxLength(100);

                e.HasMany(b => b.Dispensations)
                 .WithOne(d => d.StockBatch)
                 .HasForeignKey(d => d.StockBatchId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Dispensation ─────────────────────────────────────────────────
            modelBuilder.Entity<Dispensation>(e =>
            {
                e.ToTable("Dispensations");
                e.HasKey(d => d.Id);
            });

            // ── DonationVoucher ──────────────────────────────────────────────
            modelBuilder.Entity<DonationVoucher>(e =>
            {
                e.ToTable("DonationVouchers");
                e.HasKey(v => v.Id);
                e.Property(v => v.VoucherNumber).IsRequired().HasMaxLength(30);
                e.HasIndex(v => v.VoucherNumber).IsUnique();
                e.Property(v => v.DonorName).HasMaxLength(200);
                e.Property(v => v.DonorType).HasMaxLength(100);
                e.Property(v => v.Notes).HasMaxLength(1000);
                e.Property(v => v.Status).HasConversion<int>();

                e.HasMany(v => v.Lines)
                 .WithOne(l => l.DonationVoucher)
                 .HasForeignKey(l => l.DonationVoucherId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── DonationVoucherLine ──────────────────────────────────────────
            modelBuilder.Entity<DonationVoucherLine>(e =>
            {
                e.ToTable("DonationVoucherLines");
                e.HasKey(l => l.Id);
                e.Property(l => l.DrugName).IsRequired().HasMaxLength(300);
                e.Property(l => l.Dci).HasMaxLength(300);
                e.Property(l => l.Barcode).HasMaxLength(100);
                e.Property(l => l.BatchNumber).HasMaxLength(100);
                e.Property(l => l.Notes).HasMaxLength(500);

                // Drug → VoucherLines (restrict delete if drug has lines)
                e.HasOne(l => l.Drug)
                 .WithMany(d => d.VoucherLines)
                 .HasForeignKey(l => l.DrugId)
                 .OnDelete(DeleteBehavior.SetNull);

                // StockBatch link (no cascade — stock batch survives voucher deletion)
                e.HasOne(l => l.StockBatch)
                 .WithMany()
                 .HasForeignKey(l => l.StockBatchId)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
