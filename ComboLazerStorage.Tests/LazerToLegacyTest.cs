using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace ComboLazerStorage.Tests
{
    [TestFixture]
    public class LazerToLegacyTest
    {
        private string baseDir;
        private string dynamicSongsDir;
        private string staticFilesDir;
        private string realmPathFile;

        [SetUp]
        public void Setup()
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;

            dynamicSongsDir = Path.Combine(baseDir, "TestResources/LazerToLegacyTest/Dynamic/Songs");
            staticFilesDir = Path.Combine(baseDir, "TestResources/Static/files");
            realmPathFile = Path.Combine(baseDir, "TestResources/Static/granat.realm");

            Directory.CreateDirectory(dynamicSongsDir);
            Directory.CreateDirectory(staticFilesDir);

            if (!File.Exists(realmPathFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(realmPathFile)!);
                using (File.Create(realmPathFile)) { }
            }

            foreach (var dir in Directory.GetDirectories(dynamicSongsDir))
                Directory.Delete(dir, true);
        }

        [Test]
        public void TestLazerToLegacy()
        {
            
            string schemaVer = "51";
            ConversionProcessor.LazerToLegacy(dynamicSongsDir, staticFilesDir, realmPathFile, schemaVer);
            
            string expectedFolderPath = Path.Combine(dynamicSongsDir, "2d24c756bfd204b9e5e5a097034baa6d199dc3c69ae0c685641a92757d94843a");
            Assert.That(Directory.Exists(expectedFolderPath), $"Expected folder '{expectedFolderPath}' was not created.");

            int fileCount = Directory.GetFiles(expectedFolderPath, "*", SearchOption.AllDirectories).Length;
            Assert.That(28, Is.EqualTo(fileCount), $"Expected 28 files in '{expectedFolderPath}', but found {fileCount}.");
        }
    }
}
