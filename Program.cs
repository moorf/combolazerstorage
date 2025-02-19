﻿using osu.Framework.Extensions;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects.Types;
using Realms;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Diagnostics;

public class Program
{

    private static void onMigration(Migration migration, ulong lastSchemaVersion) { }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, uint dwFlags);

    static private List<BeatmapInfo> createBeatmapDifficulties(BeatmapSetInfo beatmapSet, Realm realm, string basePath)
    {
        var beatmaps = new List<BeatmapInfo>();

        foreach (var file in beatmapSet.Files.Where(f => f.Filename.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)))
        {
            byte[] fileBytes = File.ReadAllBytes(basePath + file.File.Hash.Substring(0, 1) + "/" + file.File.Hash.Substring(0, 2) + "/" + file.File.Hash);
            using (var memoryStream = new MemoryStream(fileBytes)) // we need a memory stream so we can seek file.File.GetStoragePath()
            {
                IBeatmap decoded;

                using (var lineReader = new LineBufferedReader(memoryStream, true))
                {
                    if (lineReader.PeekLine() == null)
                    {
                        continue;
                    }

                    decoded = Decoder.GetDecoder<Beatmap>(lineReader).Decode(lineReader);
                }

                string hash = memoryStream.ComputeSHA2Hash();

                var decodedInfo = decoded.BeatmapInfo;
                var decodedDifficulty = decodedInfo.Difficulty;

                var ruleset = realm.All<RulesetInfo>().FirstOrDefault(r => r.OnlineID == decodedInfo.Ruleset.OnlineID);

                if (ruleset?.Available != true)
                {
                    continue;
                }

                var difficulty = new BeatmapDifficulty
                {
                    DrainRate = decodedDifficulty.DrainRate,
                    CircleSize = decodedDifficulty.CircleSize,
                    OverallDifficulty = decodedDifficulty.OverallDifficulty,
                    ApproachRate = decodedDifficulty.ApproachRate,
                    SliderMultiplier = decodedDifficulty.SliderMultiplier,
                    SliderTickRate = decodedDifficulty.SliderTickRate
                };

                var metadata = new BeatmapMetadata
                {
                    Title = decoded.Metadata.Title,
                    TitleUnicode = decoded.Metadata.TitleUnicode,
                    Artist = decoded.Metadata.Artist,
                    ArtistUnicode = decoded.Metadata.ArtistUnicode,
                    Author =
                        {
                            OnlineID = decoded.Metadata.Author.OnlineID,
                            Username = decoded.Metadata.Author.Username
                        },
                    Source = decoded.Metadata.Source,
                    Tags = decoded.Metadata.Tags,
                    PreviewTime = decoded.Metadata.PreviewTime,
                    AudioFile = decoded.Metadata.AudioFile,
                    BackgroundFile = decoded.Metadata.BackgroundFile,
                };

                var beatmap = new BeatmapInfo(ruleset, difficulty, metadata)
                {
                    Hash = hash,
                    DifficultyName = decodedInfo.DifficultyName,
                    OnlineID = decodedInfo.OnlineID,
                    BeatDivisor = decodedInfo.BeatDivisor,
                    MD5Hash = memoryStream.ComputeMD5Hash(),
                    EndTimeObjectCount = decoded.HitObjects.Count(h => h is IHasDuration),
                    TotalObjectCount = decoded.HitObjects.Count
                };

                beatmaps.Add(beatmap);
            }
        }

        if (!beatmaps.Any())
            throw new ArgumentException("No valid beatmap files found in the beatmap archive.");

        return beatmaps;
    }
    public static string WithSlash(string path)
    {
        if (!string.IsNullOrEmpty(path) && !path.EndsWith("\\"))
        {
            return path + "/";
        }
        return path;
    }

    static void ToLegacy(string legacyFilesFolder, string lazerFilesFolder, string realmPathFile, string schema_ver)
    {

        var sourceConfig = new RealmConfiguration(realmPathFile)
        {
            SchemaVersion = (ulong)Int64.Parse(schema_ver),
            MigrationCallback = onMigration,
            FallbackPipePath = Path.Combine(Path.GetTempPath(), @"lazer"),
        };
        {
            try
            {
                var sourceRealm = Realm.GetInstance(sourceConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            using (var sourceRealm = Realm.GetInstance(sourceConfig))
            {
                var allObjectsBeatmapInfo = sourceRealm.All<BeatmapSetInfo>().ToList();
                int i = 0;
                foreach (var beatmapFiles in allObjectsBeatmapInfo)
                {
                    i++;
                    string cumhash = i.ToString();
                    Console.WriteLine(i);
                    foreach (var beatmapFile in beatmapFiles.Files)
                    {
                        Console.WriteLine(beatmapFile.Filename);
                        string hash = beatmapFile.File.Hash;
                        string fullsrc = WithSlash(lazerFilesFolder) + hash.Substring(0, 1) + "/" + hash.Substring(0, 2) + "/" + hash;
                        string dest = WithSlash(legacyFilesFolder) + beatmapFiles.Hash + "/" + beatmapFile.Filename;
                        try
                        {
                            // Check if source file exists
                            if (System.IO.File.Exists(fullsrc) && !string.IsNullOrEmpty(dest))
                            {
                                // Ensure destination directory exists
                                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));

                                // Copy the file from source to destination
                                System.IO.File.Copy(fullsrc, dest, false);
                            }
                        }
                        catch (IOException ioEx)
                        {
                            // Log the error but continue
                            Console.WriteLine($"File access error (IO) while processing {fullsrc}: {ioEx.Message}");
                        }
                        catch (UnauthorizedAccessException authEx)
                        {
                            // Log the error but continue
                            Console.WriteLine($"Unauthorized access error while processing {fullsrc}: {authEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            // Catch any other unexpected errors and continue
                            Console.WriteLine($"Unexpected error while processing {fullsrc}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
    public static HashSet<string> ListDirectoriesWithOsuFiles(string sourcePath)
    {
        HashSet<string> a = new HashSet<string>(1000);
        string[] directories = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories);

        foreach (string dir in directories)
        {
            string[] osuFiles = Directory.GetFiles(dir, "*.osu");

            if (osuFiles.Length > 0)
            {
                a.Add(dir);
            }
        }
        return a;
    }
    static void LegacyToSym(string legacyFilesFolder, string lazerFilesFolder)
    {
        string[] files = Directory.GetFiles(legacyFilesFolder, "*", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    byte[] hashBytes = sha256.ComputeHash(fileStream);
                    string hashStr = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    string fullsrc_sym = WithSlash(lazerFilesFolder) + hashStr.Substring(0, 1) + "/" + hashStr.Substring(0, 2) + "/" + hashStr;
                    if (fullsrc_sym == null) continue;
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullsrc_sym));
                    bool result = CreateSymbolicLink(fullsrc_sym, file, 0);
                    if (!result) Console.WriteLine("Failed to create symbolic link. Error code: " + Marshal.GetLastWin32Error());
                }
            }
        }
        Console.WriteLine("Done LegacyToSym.");
    }

    static void UpdateDatabase(string legacyFolder, string lazerFilesFolder, string realmPathFile, string schema_ver)
    {
        var sourceConfig = new RealmConfiguration(realmPathFile)
        {
            SchemaVersion = (ulong)Int64.Parse(schema_ver),
            MigrationCallback = onMigration,
            FallbackPipePath = Path.Combine(Path.GetTempPath(), @"lazer"),
        };

        var realm = Realm.GetInstance(sourceConfig);
        foreach (var beatmap in realm.All<BeatmapSetInfo>())
        {
            Console.WriteLine(beatmap.Hash);
        }
        foreach (var songdirs in ListDirectoriesWithOsuFiles(legacyFolder))
        {
            string hashStr = Path.GetFileName(songdirs);
            List<RealmNamedFileUsage> files = new List<RealmNamedFileUsage>();
            int flag = 0;
            Console.WriteLine(songdirs);
            using (MemoryStream hashable = new MemoryStream())
            {

                var ola = (string.IsNullOrEmpty(hashStr) ? null : realm.All<BeatmapSetInfo>().OrderBy(b => b.DeletePending).FirstOrDefault(b => b.Hash == hashStr));
                if (ola != null)
                {
                    Console.WriteLine("Beatmap already exists in database."); flag = 1;
                    Console.WriteLine(hashStr.ToString());
                    Console.WriteLine(hashStr.Length);
                    Console.WriteLine(ola.Hash);
                }
                else
                {
                    Console.WriteLine("Beatmap not in database."); flag = 0;
                    Console.WriteLine(hashStr.ToString());
                }
                if (hashStr == "System.Byte[]")
                {
                    Console.WriteLine("man?");
                    System.Environment.Exit(1);
                }
                if (flag == 1)
                {
                    continue;
                }
                if (songdirs != null)
                {
                    foreach (var filenames in Directory.GetFiles(songdirs))
                    {
                        using (SHA256 sha256 = SHA256.Create())
                        {
                            using (FileStream fileStream = new FileStream(filenames, FileMode.Open, FileAccess.Read))
                            {
                                byte[] fileBytes = File.ReadAllBytes(filenames);
                                using (MemoryStream memoryStream = new MemoryStream(fileBytes))
                                {
                                    memoryStream.CopyTo(hashable);
                                    //Console.WriteLine("1");
                                }
                                byte[] hashBytes = sha256.ComputeHash(fileStream);
                                string _hashStr = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                                //Console.WriteLine(_hashStr);
                                RealmFile realmfile = new RealmFile();
                                realmfile.Hash = _hashStr;
                                string truncName = Path.GetFileName(filenames);
                                files.Add(new RealmNamedFileUsage(realmfile, truncName));
                            }
                        }
                    }
                }

                using (var transaction = realm.BeginWrite())
                {
                    foreach (var file in files)
                    {
                        if (!file.File.IsManaged)
                            realm.Add(file.File, true);

                    }
                    transaction.Commit();
                }
                BeatmapSetInfo item = new BeatmapSetInfo();
                item.Files.AddRange(files);

                using (SHA256 _sha256 = SHA256.Create())
                {
                    if (hashable == null || hashable.Length < 1) continue;
                    hashable.Position = 0;
                    byte[] _hashBytes = _sha256.ComputeHash(hashable);
                    //Console.WriteLine(BitConverter.ToString(hashable.ToArray().Take(20).ToArray()));
                    string a = BitConverter.ToString(_hashBytes).Replace("-", "").ToLower();
                    item.Hash = BitConverter.ToString(_hashBytes).Replace("-", "").ToLower();
                    Console.WriteLine("item hash is:  " + a);
                }
                using (var transaction = realm.BeginWrite())
                {
                    Populate(item, realm, WithSlash(lazerFilesFolder));
                    realm.Add(item);

                    transaction.Commit();
                }
            }
        }
    }

    static void Populate(BeatmapSetInfo beatmapSet, Realm realm, string basePath)
    {
        beatmapSet.Beatmaps.AddRange(createBeatmapDifficulties(beatmapSet, realm, basePath));

        beatmapSet.DateAdded = new DateTimeOffset(File.GetCreationTime(beatmapSet.Files[0].Filename));

        foreach (BeatmapInfo b in beatmapSet.Beatmaps)
        {
            b.BeatmapSet = beatmapSet;

            if (!b.Ruleset.IsManaged)
                b.Ruleset = realm.Find<RulesetInfo>(b.Ruleset.ShortName) ?? throw new ArgumentNullException(nameof(b.Ruleset));
        }

        bool hadOnlineIDs = beatmapSet.Beatmaps.Any(b => b.OnlineID > 0);
        if (hadOnlineIDs && !beatmapSet.Beatmaps.Any(b => b.OnlineID > 0))
        {
            if (beatmapSet.OnlineID > 0)
            {
                beatmapSet.OnlineID = -1;
            }
        }
    }

    static void Main(string[] args)
        {

            if (args.Length < 2)
            {
                Console.WriteLine("Error: Not enough arguments provided.");
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/C python main.py"; // Runs and closes CMD
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = false; // Runs without showing CMD window

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    Console.WriteLine("Output:\n" + output);
                    Console.WriteLine("Error:\n" + error);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                Console.ReadLine();
                return;
            }

            int mode;
            if (!int.TryParse(args[0], out mode) || mode < 1 || mode > 3)
            {
                Console.WriteLine("Error: Invalid mode.");
                return;
            }

            Console.WriteLine($"Selected Mode: {mode}");

            for (int i = 1; i < args.Length; i++)
            {
                Console.WriteLine($"Path {i}: {args[i]}");
                if (args[i] == null || args[i].Length < 1)
                {
                    Console.WriteLine("Some arguments passed are null. Aborting"); System.Environment.Exit(1);
                }
            }

            switch (mode)
            {
                case 1:
                    ToLegacy(args[1], args[2], args[3], args[4]); break;
                case 2:
                    LegacyToSym(args[1], args[2]); break;
                case 3:
                    UpdateDatabase(args[1], args[2], args[3], args[4]); break;
                default:
                    Console.WriteLine("Something is wrong with the selected operation mode"); break;
            }
        }
    }
