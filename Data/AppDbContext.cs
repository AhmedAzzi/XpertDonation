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

                // Seed Drugs
                e.HasData(
                    new Drug { Id = 1, Name = "AUGMENTIN 1G COMPRIME", Dci = "Amoxicilline / Acide clavulanique", Form = "Comprimé", CreatedAt = new System.DateTime(2026, 1, 1) },
                    new Drug { Id = 2, Name = "PARACETAMOL 500MG COMPRIME", Dci = "Paracétamol", Form = "Comprimé", CreatedAt = new System.DateTime(2026, 1, 1) },
                    new Drug { Id = 3, Name = "ASPIRINE 500MG COMPRIME", Dci = "Acide acétylsalicylique", Form = "Comprimé", CreatedAt = new System.DateTime(2026, 1, 1) },
                    new Drug { Id = 4, Name = "DOLIPRANE 1000MG COMPRIME", Dci = "Paracétamol", Form = "Comprimé", CreatedAt = new System.DateTime(2026, 1, 1) },
                    new Drug { Id = 5, Name = "AMOXICILLINE 500MG GELULE", Dci = "Amoxicilline", Form = "Gélule", CreatedAt = new System.DateTime(2026, 1, 1) }
                );
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

                // Seed StockBatches
                e.HasData(
                    new StockBatch { Id = 1, DrugId = 1, BatchNumber = "LOT001", Barcode = "3400921845603", ExpirationDate = new System.DateTime(2027, 6, 30), InitialQuantity = 500, CreatedAt = new System.DateTime(2026, 1, 1) },
                    new StockBatch { Id = 2, DrugId = 2, BatchNumber = "LOT002", Barcode = "3400936563017", ExpirationDate = new System.DateTime(2026, 12, 31), InitialQuantity = 1000, CreatedAt = new System.DateTime(2026, 1, 1) },
                    new StockBatch { Id = 3, DrugId = 3, BatchNumber = "LOT003", Barcode = "3400935337700", ExpirationDate = new System.DateTime(2026, 8, 15), InitialQuantity = 300, CreatedAt = new System.DateTime(2026, 1, 1) },
                    new StockBatch { Id = 4, DrugId = 4, BatchNumber = "LOT004", Barcode = "3400929200506", ExpirationDate = new System.DateTime(2027, 3, 31), InitialQuantity = 200, CreatedAt = new System.DateTime(2026, 1, 1) },
                    new StockBatch { Id = 5, DrugId = 5, BatchNumber = "LOT005", Barcode = "3400932832022", ExpirationDate = new System.DateTime(2026, 10, 31), InitialQuantity = 400, CreatedAt = new System.DateTime(2026, 1, 1) },
                    new StockBatch { Id = 6, DrugId = 5, BatchNumber = "LOT006", Barcode = "LOT006BCODE", ExpirationDate = new System.DateTime(2025, 12, 31), InitialQuantity = 150, CreatedAt = new System.DateTime(2026, 1, 1) },
                    new StockBatch { Id = 7, DrugId = 5, BatchNumber = "LOT007", Barcode = "LOT007BCODE", ExpirationDate = new System.DateTime(2027, 9, 30), InitialQuantity = 600, CreatedAt = new System.DateTime(2026, 1, 1) }
                );
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
