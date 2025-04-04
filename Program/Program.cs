﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions;
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
using osu.Framework;
using osu.Framework.Platform;
using osu.Framework.Testing;
using System.Reflection;
using NuGet.Protocol;


public class Program
{
    public static void onMigration(Migration migration, ulong lastSchemaVersion) { }

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
                Console.WriteLine("Import a beatmap with ruleset" + ruleset?.OnlineID);
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
    static void RecomputeDirectoryHashes(string legacyFolder)
    {

    }

    public static void LazerToLegacy(string legacyFilesFolder, string lazerFilesFolder, string realmPathFile, string schema_ver)
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

    static void LegacyToSymbolic(string legacyFilesFolder, string lazerFilesFolder)
    {
        string[] files = Directory.GetFiles(legacyFilesFolder, "*", SearchOption.AllDirectories);
        long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (string file in files)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    byte[] hashBytes = sha256.ComputeHash(fileStream);
                    string hashStr = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    string fullsrc_sym = WithSlash(lazerFilesFolder) + hashStr.Substring(0, 1) + "/" + hashStr.Substring(0, 2) + "/" + hashStr;

                    string fullsrc_sym_backup = WithSlash(lazerFilesFolder + (unixTimestamp).ToString()) + hashStr.Substring(0, 1) + "/" + hashStr.Substring(0, 2) + "/" + hashStr;
                    if (String.IsNullOrEmpty(fullsrc_sym) || String.IsNullOrEmpty(fullsrc_sym_backup))
                    {
                        Console.WriteLine("lazer files or backupfolders name is null or empty.");
                        continue;
                    }

                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullsrc_sym));
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullsrc_sym_backup));

                    if (File.Exists(fullsrc_sym))
                    {
                        File.Move(fullsrc_sym, fullsrc_sym_backup);
                    }
                    bool result = CreateSymbolicLink(fullsrc_sym, file, 0);
                    if (!result && Marshal.GetLastWin32Error() != 183) Console.WriteLine("Failed to create symbolic link. Error code: " + Marshal.GetLastWin32Error());
                }
            }
        }
        Console.WriteLine("Done LegacyToSymbolic.");
    }

    public static void UpdateDatabase(string legacyFolder, string lazerFilesFolder, string realmPathFile, string schema_ver)
    {
        var sourceConfig = new RealmConfiguration(realmPathFile)
        {
            SchemaVersion = (ulong)Int64.Parse(schema_ver),
            MigrationCallback = onMigration,
            FallbackPipePath = Path.Combine(Path.GetTempPath(), @"lazer"),
        };

        var realm = Realm.GetInstance(sourceConfig);

        foreach (var songdirs in ListDirectoriesWithOsuFiles(legacyFolder))
        {
            //string hashStr = Path.GetFileName(songdirs);
            BeatmapSetInfo item = new BeatmapSetInfo();
            List<RealmNamedFileUsage> files = new List<RealmNamedFileUsage>();
            int flag = 0;
            Console.WriteLine(songdirs);

            using (MemoryStream hashable = new MemoryStream())
            {
                //Making a chunk of data to be hashes, adds files to file list (realm needs hash and filename of all legacy files)
                if (songdirs != null)
                {
                    foreach (var filenames in Directory.GetFiles(songdirs, "*.*", SearchOption.AllDirectories))
                    {
                        using (SHA256 sha256 = SHA256.Create())
                        {
                            using (FileStream fileStream = new FileStream(filenames, FileMode.Open, FileAccess.Read))
                            {
                                if (filenames.EndsWith(".osu"))
                                {
                                    byte[] fileBytes = File.ReadAllBytes(filenames);
                                    using (MemoryStream memoryStream = new MemoryStream(fileBytes))
                                    {
                                        memoryStream.CopyTo(hashable);
                                        //Console.WriteLine("1");
                                    }
                                }
                                byte[] hashBytes = sha256.ComputeHash(fileStream);
                                string _hashStr = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                                //Console.WriteLine(_hashStr);
                                RealmFile realmfile = new RealmFile();
                                realmfile.Hash = _hashStr;
                                string truncName = Path.GetFileName(filenames);
                                truncName = filenames.Substring(songdirs.Length + 1).ToStandardisedPath();
                                files.Add(new RealmNamedFileUsage(realmfile, truncName));
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Legacy's one of song directories is null. ");
                    continue;
                }
                //Computes a hash of the chunk to be imported
                string beatmapSetHash = "";

                hashable.Seek(0, SeekOrigin.Begin);
                var gg = SHA256.HashData(hashable);

                item.Hash = string.Create(gg.Length * 2, gg, (span, b) =>
                {
                    for (int i = 0; i < b.Length; i++)
                        _ = b[i].TryFormat(span[(i * 2)..], out _, "x2");
                });

                hashable.Seek(0, SeekOrigin.Begin);
                beatmapSetHash = item.Hash;
                if (beatmapSetHash == "") continue;
                //Finding the hash in realm database, skip if there is (this is the only mechanism to avoid duplicate maps)
                var ola = (string.IsNullOrEmpty(beatmapSetHash) ? null : realm.All<BeatmapSetInfo>().OrderBy(b => b.DeletePending).FirstOrDefault(b => b.Hash == beatmapSetHash));
                if (ola != null)
                {
                    Console.WriteLine("Beatmap already exists in database."); flag = 1;
                    Console.WriteLine(beatmapSetHash.ToString());
                    Console.WriteLine(beatmapSetHash.Length);
                    Console.WriteLine(ola.Hash);
                }
                else
                {
                    Console.WriteLine("Beatmap not in database."); flag = 0;
                    Console.WriteLine(beatmapSetHash.ToString());
                }
                //sanity check for stupid mistakes
                if (beatmapSetHash == "System.Byte[]" || beatmapSetHash == "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")
                {
                    Console.WriteLine("man?");
                    continue;
                }
                if (flag == 1) continue;

                //realm will not be populated with anything if something goes wrong above, files List and item are cleaned each iteration
                using (var transaction = realm.BeginWrite())
                {
                    foreach (var file in files)
                    {
                        if (!file.File.IsManaged)
                            realm.Add(file.File, true);

                    }
                    transaction.Commit();
                }

                item.Files.AddRange(files);

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
    public static void MainApp(string[] args)
    {
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
                LazerToLegacy(args[1], args[2], args[3], args[4]); break;
            case 2:
                LegacyToSymbolic(args[1], args[2]); break;
            case 3:
                UpdateDatabase(args[1], args[2], args[3], args[4]); break;
            default:
                Console.WriteLine("Something is wrong with the selected operation mode"); break;
        }
    }
    public async static Task<string> DL()
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync("https://www.nuget.org/api/v2/package/ppy.osu.Game.Resources/2025.321.0");
            response.EnsureSuccessStatusCode();
            var packageBytes = await response.Content.ReadAsByteArrayAsync();

            var packagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "package.nupkg");
            await File.WriteAllBytesAsync(packagePath, packageBytes);

            Console.WriteLine("Package downloaded to " + packagePath);
            return packagePath;
        }
    }
    static void LoadResources()
    {
        string packageId = "ppy.osu.Game.Resources"; // Example package
        string packageVersion = "2025.321.0"; // Version of the package
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string librariesPath = baseDirectory;
        var a = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*", SearchOption.AllDirectories);
        var b = a.FirstOrDefault(s => s.Contains("osu.Game.Resources.dll") && !s.Contains("ppy.osu.Game.Resources.dll"));
        string dllPath = b;
        string destFilePath = dllPath;
        if (File.Exists(dllPath))
        {
            Console.WriteLine($"Using cached version of {packageId}.dll");
        }
        else
        {
            Console.WriteLine($"Downloading {packageId}...");
            var extractedFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extracted");
            Directory.CreateDirectory(extractedFolder);

            using (var reader = new NuGet.Packaging.PackageArchiveReader(DL().Result))
            {
                var libFolder = reader.GetLibItems().FirstOrDefault();
                if (libFolder != null)
                {
                    foreach (var file in libFolder.Items)
                    {
                        destFilePath = Path.Combine(extractedFolder, file);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
                        using (var fileStream = File.Create(destFilePath))
                        using (var packageStream = reader.GetStream(file))
                        {
                            packageStream.CopyTo(fileStream);
                        }
                    }
                    Console.WriteLine($"Package extracted to {extractedFolder}");
                }
            }
            a = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*", SearchOption.AllDirectories);
            b = a.FirstOrDefault(s => s.Contains("osu.Game.Resources.dll") && !s.Contains("ppy.osu.Game.Resources.dll"));
            dllPath = b;
            destFilePath = dllPath;
        }
        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "package.nupkg"));
        if (File.Exists(destFilePath))
        {
            // Load the DLL into the AppDomain if needed
            var assembly = Assembly.LoadFrom(destFilePath);
            Console.WriteLine($"Assembly Loaded: {assembly.FullName}");
        }
    }
    static void Main(string[] args)
    {
        LoadResources();
        if (args.Length < 2)
        {
            if (args.Length > 0 && args.ElementAt(0) != null)
            {
                if (args.ElementAt(0) == "4")
                {
                    Console.WriteLine("Testing Export-Import");
                    combolazerstorage.Tests.DatabaseImport.Test();
                    return;
                }
                if (args.ElementAt(0) == "5")
                {
                    Console.WriteLine("Testing Storyboard");
                    combolazerstorage.Tests.StoryboardImport.Test();
                    return;
                }

            }
            using DesktopGameHost host = Host.GetSuitableDesktopHost("ComboLazerStorage", new HostOptions
            {
                PortableInstallation = true,
                BypassCompositor = false,
                FriendlyGameName = "Combo Lazer Storage"
            });

            using var game = new ComboLazerStorageGame();

            host.Run(game);
            Console.ReadLine();
            return;
        }
        else
        {
            MainApp(args);
        }
    }
}
