using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XpertPharm5Donation.Data;
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

            // Run EF ensure created
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                // Apply dynamic schema updates without migrations
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
                } 
                catch (Exception sqlEx)
                {
                    _logger.LogWarning(sqlEx, "Failed to dynamically add Barcode column to Drugs. It may already exist or the syntax is wrong.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur de connexion à la base de données :\n\n{ex.Message}\n\n" +
                    "Vérifiez que SQL Server LocalDB est installé et que la chaîne de connexion est correcte.\n\n" +
                    "Connexion par défaut : (localdb)\\v11.0",
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
