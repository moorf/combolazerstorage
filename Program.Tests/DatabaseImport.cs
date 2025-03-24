using osu.Game.Rulesets.Osu.Skinning.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realms;
using osu.Game.Beatmaps;

namespace combolazerstorage.Tests
{
    internal class DatabaseImport
    {

        void InitDatabase(string Path)
        {

        }
        static void CompareDatabases(string Path1, string Path2, string schema_ver)
        {
            var sourceConfig = new RealmConfiguration(Path1)
            {
                SchemaVersion = (ulong)Int64.Parse(schema_ver),
                MigrationCallback = Program.onMigration,
                FallbackPipePath = Path.Combine(Path.GetTempPath(), @"lazer"),
            };
            var sourceConfig2 = new RealmConfiguration(Path2)
            {
                SchemaVersion = (ulong)Int64.Parse(schema_ver),
                MigrationCallback = Program.onMigration,
                FallbackPipePath = Path.Combine(Path.GetTempPath(), @"lazer"),
            };
            using (var sourceRealm = Realm.GetInstance(sourceConfig))
            using (var sourceRealm2 = Realm.GetInstance(sourceConfig2))
            {
                var dbOne = sourceRealm.All<BeatmapSetInfo>().ToList().Select(b => b.Hash).ToList();
                var dbTwo = sourceRealm2.All<BeatmapSetInfo>().ToList().Select(b => b.Hash).ToList();

                for (int i = 0; i < dbOne.Count; i++)
                {
                    if (!dbTwo.Contains(dbOne[i]))
                    {
                        Console.WriteLine(dbOne[i]+ " is not in the old Database.");
                    }
                    else
                    {
                        Console.WriteLine(dbOne[i] + " is in the old Database! yay");
                    };
                }
                for (int i = 0; i < dbTwo.Count; i++)
                {
                    if (!dbOne.Contains(dbTwo[i]))
                    {
                        Console.WriteLine(dbTwo[i] + " is not in the new Database.");
                    }
                    else
                    {
                        Console.WriteLine(dbTwo[i] + " is in the new Database! yay");
                    };
                }
            }
        }


        static public void Test()
        {
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tests/test2.realm")))
            {
                File.Create("./TestFiles/test2.realm");
                var config = new RealmConfiguration("./TestFiles/test2.realm")
                {
                    IsReadOnly = false
                };
                using (var realm = Realm.GetInstance(config))
                {
                    Console.WriteLine($"Initialized empty Realm database at: {"./TestFiles/test2.realm"}");
                }
            }
            //string legacyFolder, string lazerFilesFolder, string realmPathFile, string schema_ver
            //Program.LazerToLegacy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/Static/Songs"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/Static/lazer"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/Static/test1.realm"), "48");
            Program.UpdateDatabase(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/Static/Songs"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/Static/lazer"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/test2.realm"), "48");
            //test duping
            Program.UpdateDatabase(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/Static/Songs"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/Static/lazer"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/test2.realm"), "48");

            CompareDatabases(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/Static/test1.realm"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tests/test2.realm"), "48");
        }
    }
}
