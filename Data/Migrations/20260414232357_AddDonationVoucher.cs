using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace XDonation.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF COL_LENGTH('Drugs', 'Dci') IS NULL
    ALTER TABLE [Drugs] ADD [Dci] nvarchar(300) NULL;
""");

            migrationBuilder.Sql("""
IF COL_LENGTH('Drugs', 'Form') IS NULL
    ALTER TABLE [Drugs] ADD [Form] nvarchar(100) NULL;
""");

            migrationBuilder.Sql("""
IF OBJECT_ID(N'[DonationVouchers]', N'U') IS NULL
BEGIN
    CREATE TABLE [DonationVouchers] (
        [Id] int NOT NULL IDENTITY,
        [VoucherNumber] nvarchar(30) NOT NULL,
        [DonorName] nvarchar(200) NOT NULL,
        [DonorType] nvarchar(100) NULL,
        [ReceiptDate] datetime2 NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ValidatedAt] datetime2 NULL,
        CONSTRAINT [PK_DonationVouchers] PRIMARY KEY ([Id])
    );
END
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonationVouchers_VoucherNumber' AND object_id = OBJECT_ID(N'[DonationVouchers]'))
    CREATE UNIQUE INDEX [IX_DonationVouchers_VoucherNumber] ON [DonationVouchers]([VoucherNumber]);
""");

            migrationBuilder.Sql("""
IF OBJECT_ID(N'[DonationVoucherLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [DonationVoucherLines] (
        [Id] int NOT NULL IDENTITY,
        [DonationVoucherId] int NOT NULL,
        [DrugId] int NULL,
        [DrugName] nvarchar(300) NOT NULL,
        [Dci] nvarchar(300) NULL,
        [Barcode] nvarchar(100) NULL,
        [BatchNumber] nvarchar(100) NULL,
        [ExpirationDate] datetime2 NULL,
        [Quantity] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        [StockBatchId] int NULL,
        CONSTRAINT [PK_DonationVoucherLines] PRIMARY KEY ([Id])
    );
END
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonationVoucherLines_DonationVoucherId' AND object_id = OBJECT_ID(N'[DonationVoucherLines]'))
    CREATE INDEX [IX_DonationVoucherLines_DonationVoucherId] ON [DonationVoucherLines]([DonationVoucherId]);
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonationVoucherLines_DrugId' AND object_id = OBJECT_ID(N'[DonationVoucherLines]'))
    CREATE INDEX [IX_DonationVoucherLines_DrugId] ON [DonationVoucherLines]([DrugId]);
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonationVoucherLines_StockBatchId' AND object_id = OBJECT_ID(N'[DonationVoucherLines]'))
    CREATE INDEX [IX_DonationVoucherLines_StockBatchId] ON [DonationVoucherLines]([StockBatchId]);
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DonationVoucherLines_DonationVouchers_DonationVoucherId')
BEGIN
    ALTER TABLE [DonationVoucherLines]
    ADD CONSTRAINT [FK_DonationVoucherLines_DonationVouchers_DonationVoucherId]
    FOREIGN KEY ([DonationVoucherId]) REFERENCES [DonationVouchers]([Id]) ON DELETE CASCADE;
END
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DonationVoucherLines_Drugs_DrugId')
BEGIN
    ALTER TABLE [DonationVoucherLines]
    ADD CONSTRAINT [FK_DonationVoucherLines_Drugs_DrugId]
    FOREIGN KEY ([DrugId]) REFERENCES [Drugs]([Id]) ON DELETE SET NULL;
END
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DonationVoucherLines_StockBatches_StockBatchId')
BEGIN
    ALTER TABLE [DonationVoucherLines]
    ADD CONSTRAINT [FK_DonationVoucherLines_StockBatches_StockBatchId]
    FOREIGN KEY ([StockBatchId]) REFERENCES [StockBatches]([Id]) ON DELETE NO ACTION;
END
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[DonationVoucherLines]', N'U') IS NOT NULL
    DROP TABLE [DonationVoucherLines];
""");

            migrationBuilder.Sql("""
IF OBJECT_ID(N'[DonationVouchers]', N'U') IS NOT NULL
    DROP TABLE [DonationVouchers];
""");
        }
    }
}
