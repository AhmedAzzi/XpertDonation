using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XpertPharm5Donation.Data;
using XpertPharm5Donation.Models;
using XpertPharm5Donation.ViewModels;
using XpertPharm5Donation.Views;

namespace XpertPharm5Donation
{
    public partial class App : Application
    {
        // P/Invoke to allocate console window for WPF app
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        private ServiceProvider? _serviceProvider;
        private ILogger<App>? _logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Register global exception handlers
            AppDomain.CurrentDomain.UnhandledException += (s, ev) => ShowError(ev.ExceptionObject as Exception, "AppDomain.UnhandledException");
            DispatcherUnhandledException += (s, ev) => { ShowError(ev.Exception, "DispatcherUnhandledException"); ev.Handled = true; };
            TaskScheduler.UnobservedTaskException += (s, ev) => ShowError(ev.Exception, "TaskScheduler.UnobservedTaskException");

            base.OnStartup(e);

            // Allocate console window for logging
            AllocConsole();

            // Enable Enter key navigation
            XpertPharm5Donation.Helpers.FocusExtensions.RegisterGlobalEnterNavigation();

            // Load configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // DI container
            var services = new ServiceCollection();

            // Configure logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            });

            // DB Context
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)), ServiceLifetime.Transient);

            // ViewModels
            services.AddTransient<HomeViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<HistoryViewModel>();
            services.AddTransient<ManageDonationsViewModel>();
            services.AddTransient<DonationVoucherViewModel>();
            services.AddTransient<DonationJournalViewModel>();
            services.AddTransient<StockLotsViewModel>();
            services.AddTransient<SalesCounterViewModel>();
            services.AddTransient<SalesJournalViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ReceptionDocumentViewModel>();

            // Main window
            services.AddTransient<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            // Get logger
            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("═══════════════════════════════════════════════════════════");
            _logger.LogInformation("Application started at {StartTime}", DateTime.Now);
            _logger.LogInformation("═══════════════════════════════════════════════════════════");

            // Run EF ensure created and seed data
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                _logger.LogInformation("Initializing database...");
                db.Database.EnsureCreated();
                
                // Check if we need to seed
                if (!System.Linq.Enumerable.Any(db.Drugs))
                {
                    _logger.LogInformation("Database is empty. Starting seeding process...");
                    
                    var medicamentFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "medicament.json");
                    if (System.IO.File.Exists(medicamentFilePath))
                    {
                        _logger.LogInformation("Seeding drugs from {FilePath}...", medicamentFilePath);
                        var jsonText = System.IO.File.ReadAllText(medicamentFilePath);
                        using var doc = System.Text.Json.JsonDocument.Parse(jsonText);
                        var root = doc.RootElement;
                        
                        if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() >= 2)
                        {
                            var dataNode = root[1].GetProperty("data");
                            var drugs = new System.Collections.Generic.List<Drug>();
                            
                            string? GetStringSafe(System.Text.Json.JsonElement el, string propName)
                            {
                                return el.TryGetProperty(propName, out var prop) && prop.ValueKind != System.Text.Json.JsonValueKind.Null ? prop.GetString() : null;
                            }

                            foreach (var item in dataNode.EnumerateArray())
                            {
                                var dci = GetStringSafe(item, "DENOMINATION_COMMUNE_INTERNATIONALE");
                                var nomMarque = GetStringSafe(item, "NOM_DE_MARQUE");
                                var forme = GetStringSafe(item, "FORME");
                                var dosage = GetStringSafe(item, "DOSAGE");
                                
                                var drugName = string.IsNullOrWhiteSpace(nomMarque) ? "Unknown" : nomMarque;
                                if (!string.IsNullOrWhiteSpace(dosage))
                                {
                                    drugName += $" {dosage}";
                                }

                                drugs.Add(new Drug
                                {
                                    Name = drugName.Length > 300 ? drugName.Substring(0, 300) : drugName,
                                    Dci = dci?.Length > 300 ? dci.Substring(0, 300) : dci,
                                    Form = forme?.Length > 100 ? forme.Substring(0, 100) : forme,
                                    Barcode = null 
                                });
                            }
                            
                            var distinctDrugs = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(System.Linq.Enumerable.GroupBy(drugs, d => d.Name), g => g.First()));
                            
                            db.Drugs.AddRange(distinctDrugs);
                            db.SaveChanges();
                            _logger.LogInformation("Successfully seeded {Count} unique drugs.", distinctDrugs.Count);
                        }
                    }
                }

                // Dynamic schema updates (Barcode column, etc.)
                try 
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF COL_LENGTH('Drugs', 'Barcode') IS NULL
                        BEGIN
                            ALTER TABLE Drugs ADD Barcode NVARCHAR(100) NULL;
                        END
                    ");
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Drugs_Barcode' AND object_id = OBJECT_ID('Drugs'))
                        BEGIN
                            CREATE UNIQUE NONCLUSTERED INDEX IX_Drugs_Barcode ON Drugs(Barcode) WHERE Barcode IS NOT NULL;
                        END
                    ");

                    db.Database.ExecuteSqlRaw(@"
                        IF COL_LENGTH('StockBatches', 'IsBlocked') IS NULL
                        BEGIN
                            ALTER TABLE StockBatches ADD IsBlocked BIT NOT NULL DEFAULT 0;
                        END
                    ");
                    db.Database.ExecuteSqlRaw(@"
                        IF COL_LENGTH('StockBatches', 'Store') IS NULL
                        BEGIN
                            ALTER TABLE StockBatches ADD Store NVARCHAR(100) NULL;
                        END
                    ");
                    db.Database.ExecuteSqlRaw(@"
                        IF COL_LENGTH('StockBatches', 'IsPsychotrope') IS NULL
                        BEGIN
                            ALTER TABLE StockBatches ADD IsPsychotrope BIT NOT NULL DEFAULT 0;
                        END
                    ");
                } 
                catch (Exception sqlEx)
                {
                    _logger.LogWarning(sqlEx, "Non-critical schema update warning.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur de connexion à la base de données :\n\n{ex.Message}\n\n" +
                    "Vérifiez que SQL Server LocalDB est installé.\n\n" +
                    $"Connexion utilisée : {config.GetConnectionString("DefaultConnection")}",
                    "Erreur de base de données",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }

        private void ShowError(Exception? ex, string source)
        {
            if (ex == null) return;
            
            var message = $"Une erreur critique est survenue ({source}) :\n\n{ex.Message}\n\n{ex.StackTrace}";
            if (ex.InnerException != null)
                message += $"\n\nInner Exception :\n{ex.InnerException.Message}";

            _logger?.LogError(ex, "CRITICAL ERROR from {Source}", source);
            
            MessageBox.Show(message, "Erreur Critique - XpertPharm5", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
