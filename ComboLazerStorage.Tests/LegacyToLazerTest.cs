using NUnit.Framework;
using osu.Game.Beatmaps;
using Realms;
using System.Security.Principal;

using static Helper;

namespace ComboLazerStorage.Tests
{
    [TestFixture]
    public class LegacyToLazerTest
    {
        private string baseDir;
        private string dynamicSongsDir;
        private string songsDir;
        private string staticFilesDir;
        private string realmPathFile;
        private string realmDynamicFile;
        private string realmFullFile;

        [SetUp]
        public void Setup()
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;

            dynamicSongsDir = Path.Combine(baseDir, "TestResources/LegacyToLazerTest/Dynamic/files");
            songsDir = Path.Combine(baseDir, "TestResources/Static/Songs");
            staticFilesDir = Path.Combine(baseDir, "TestResources/Static/files");
            realmPathFile = Path.Combine(baseDir, "TestResources/Static/emptyDB.realm");
            realmFullFile = Path.Combine(baseDir, "TestResources/Static/granat.realm");
            Directory.CreateDirectory(dynamicSongsDir);
            Directory.CreateDirectory(staticFilesDir);
            Directory.CreateDirectory(songsDir);
            
            realmDynamicFile = Path.Combine(dynamicSongsDir, "emptyDB.realm");
            File.Copy(realmPathFile, realmDynamicFile, true);
            if (!Directory.Exists(dynamicSongsDir))
            {
                Directory.CreateDirectory(dynamicSongsDir);
            }
            if (!File.Exists(realmPathFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(realmPathFile)!);
                using (File.Create(realmPathFile)) { }
            }

            foreach (var dir in Directory.GetDirectories(dynamicSongsDir))
                Directory.Delete(dir, true);
        }

        private static bool areEqualDatabases(string path1, string path2, string schemaVer)
        {
            var sourceConfig = new RealmConfiguration(path1)
            {
                SchemaVersion = (ulong)Int64.Parse(schemaVer),
                MigrationCallback = onMigration,
                FallbackPipePath = Path.Combine(Path.GetTempPath(), @"lazer"),
            };
            var sourceConfig2 = new RealmConfiguration(path2)
            {
                SchemaVersion = (ulong)Int64.Parse(schemaVer),
                MigrationCallback = onMigration,
                FallbackPipePath = Path.Combine(Path.GetTempPath(), @"lazer"),
            };

            using (var sourceRealm = Realm.GetInstance(sourceConfig))
            using (var sourceRealm2 = Realm.GetInstance(sourceConfig2))
            {
                var dbOne = sourceRealm.All<BeatmapSetInfo>().ToList().Select(b => b.Hash).ToList();
                var dbTwo = sourceRealm2.All<BeatmapSetInfo>().ToList().Select(b => b.Hash).ToList();

                foreach (var hash in dbOne)
                {
                    if (!dbTwo.Contains(hash))
                        return false;
                }

                foreach (var hash in dbTwo)
                {
                    if (!dbOne.Contains(hash))
                        return false;
                }
            }
            return true;
        }
        private bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        [Test]
        public void TestLegacyToLazerTest()
        {

            string schemaVer = "51";
            Assert.That(IsAdministrator(), $"Test not running as admin.");
            ConversionProcessor.LegacyToSymbolic(songsDir, dynamicSongsDir, backup: false);
            ConversionProcessor.UpdateDatabase(songsDir, dynamicSongsDir, realmDynamicFile, schemaVer);
            Assert.That(areEqualDatabases(realmDynamicFile, realmFullFile, schemaVer), $"Databases are not equal.");

        }
    }
}
