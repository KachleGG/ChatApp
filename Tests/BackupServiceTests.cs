using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;

namespace Tests
{
    [TestClass]
    public class BackupServiceTests
    {
        private string _tempDir = null!;

        [TestInitialize]
        public void Init()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "chatter_backup_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [TestMethod]
        public async Task CreateBackup_CreatesZipFile()
        {
            // arrange
            var services = new ServiceCollection();
            var dbFile = Path.Combine(_tempDir, "app.db");
            services.AddDbContext<Chatter.Data.ChatterDbContext>(options => options.UseSqlite($"Data Source={dbFile}"));
            var sp = services.BuildServiceProvider(true);

            // ensure DB exists
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<Chatter.Data.ChatterDbContext>();
                // Temporarily disable foreign key enforcement while running EnsureCreated so
                // model seeding (which may reference an OwnerId) doesn't fail in tests.
                var conn = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                    cmd.ExecuteNonQuery();
                }

                db.Database.EnsureCreated();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA foreign_keys = ON;";
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }

            var inMemory = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string,string?>("ServerSettings:BackupPath", _tempDir),
                    new KeyValuePair<string,string?>("ServerSettings:BackupRetention", "3"),
                    new KeyValuePair<string,string?>("ServerSettings:BackupEnabled", "true"),
                    new KeyValuePair<string,string?>("ServerSettings:BackupSchedule", "0 0 * * *")
                })
                .Build();

            var backupService = new Chatter.Services.BackupService(sp, inMemory, NullLogger<Chatter.Services.BackupService>.Instance);

            // act
            var created = await backupService.CreateBackupAsync(force: true);

            // assert
            Assert.IsTrue(created);
            var zips = Directory.EnumerateFiles(_tempDir, "*.zip").ToList();
            Assert.IsTrue(zips.Count >= 1, "Expected at least one zip backup file");
        }

        [TestMethod]
        public async Task ListAndDelete_BackupsWork()
        {
            // arrange: create a dummy zip file
            var zipPath = Path.Combine(_tempDir, "dummy.zip");
            using (var z = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create))
            {
                var e = z.CreateEntry("readme.txt");
                using var s = e.Open();
                using var sw = new StreamWriter(s);
                sw.Write("hello");
            }

            var services = new ServiceCollection();
            services.AddDbContext<Chatter.Data.ChatterDbContext>(options => options.UseSqlite($"Data Source={Path.Combine(_tempDir, "app.db")}"));
            var sp = services.BuildServiceProvider(true);

            var inMemory = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string,string?>("ServerSettings:BackupPath", _tempDir),
                })
                .Build();

            var backupService = new Chatter.Services.BackupService(sp, inMemory, NullLogger<Chatter.Services.BackupService>.Instance);

            // act
            var list = await backupService.ListBackupsAsync();
            Assert.IsTrue(list.Contains("dummy.zip"));

            var deleted = await backupService.DeleteBackupAsync("dummy.zip");
            Assert.IsTrue(deleted);
            Assert.IsFalse(File.Exists(zipPath));
        }
    }
}
