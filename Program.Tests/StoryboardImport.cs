using osu.Game.Beatmaps;
using Realms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib.Ape;

using static OsuMain;
using static Helper;

namespace combolazerstorage.Tests
{
    internal class StoryboardImport
    {
        public static void Test()
        {
            Console.WriteLine("Start Storyboard Test.");
            //Program.LazerToLegacy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/StoryboardTest/Songs"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/StoryboardTest/files"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/StoryboardTest/client.realm"), "48");
            UpdateDatabase(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/StoryboardTest/Songs"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/StoryboardTest/files"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/StoryboardTest/test2.realm"), "48");
            var schema_ver = "48";
            var sourceConfig = new RealmConfiguration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/StoryboardTest/client.realm"))
            {
                SchemaVersion = (ulong)Int64.Parse(schema_ver),
                MigrationCallback = onMigration,
                FallbackPipePath = Path.Combine(Path.GetTempPath(), @"lazer"),
            };
            var sourceConfig2 = new RealmConfiguration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./TestFiles/StoryboardTest/test2.realm"))
            {
                SchemaVersion = (ulong)Int64.Parse(schema_ver),
                MigrationCallback = onMigration,
                FallbackPipePath = Path.Combine(Path.GetTempPath(), @"lazer"),
            };
            using (var sourceRealm = Realm.GetInstance(sourceConfig))
            using (var sourceRealm2 = Realm.GetInstance(sourceConfig2))
            {
                var dbOne = sourceRealm.All<BeatmapSetInfo>().ToList();
                var dbTwo = sourceRealm2.All<BeatmapSetInfo>().ToList();
                List<string> fn = new List<string>();
                List<string> fn2 = new List<string>();
                foreach (var db in dbOne)
                {
                    foreach(var ddd in db.Files)
                    {
                        fn.Add(ddd.Filename);
                    }
                }
                foreach (var db in dbTwo)
                {
                    foreach (var ddd in db.Files)
                    {
                        fn2.Add(ddd.Filename);
                    }
                }
                foreach(var i in fn.Except(fn2).ToList())
                {
                    Console.WriteLine("Diff not in Resulting db: " + i);
                }
                foreach (var i in fn2.Except(fn).ToList())
                {
                    Console.WriteLine("Diff not in Source db: " + i);
                }
                Console.WriteLine($"fn count: {fn.Count}, fn2 count: {fn2.Count}");
            }
        }
    }
}
