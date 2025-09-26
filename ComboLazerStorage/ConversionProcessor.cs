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

using static Helper;
/// <summary>
/// Core of the program: necessary functions related to osu! file structures and osu!lazer database.
/// </summary>
public class ConversionProcessor
{
    public enum ConversionMode
    {
        LazerToLegacy = 1,
        LegacyToSymbolic = 2,
        UpdateDatabase = 3,
        LegacyToSymbolicAndUpdate = 4
    }
    public record AppArguments(ConversionMode Mode, string LegacyPath, string LazerPath, string RealmFile, string SchemaVersion);

    private static readonly Dictionary<ConversionMode, Action<AppArguments>> ModeHandlers = new()
        {
        { ConversionMode.LazerToLegacy, args => LazerToLegacy(args.LegacyPath, args.LazerPath, args.RealmFile, args.SchemaVersion) },
                { ConversionMode.LegacyToSymbolicAndUpdate, args =>
            {   LegacyToSymbolic(args.LegacyPath, args.LazerPath);
                UpdateDatabase(args.LegacyPath, args.LazerPath, args.RealmFile, args.SchemaVersion); }
        },
        //{ ConversionMode.LegacyToSymbolic, args => LegacyToSymbolic(args.LegacyPath, args.LazerPath) },
        //{ ConversionMode.UpdateDatabase, args => UpdateDatabase(args.LegacyPath, args.LazerPath, args.RealmFile, args.SchemaVersion) }
    };

    /// <summary>
    /// Checks if the arguments passed are correct. If so, carries out the task according to ConversionMode.    
    /// </summary>
    /// <param name="args"> args, according to AppArguments record.</param>
    public static void ProcessArgsAndExecute(string[] args)
    {
        if (args.Length < 2 || !Enum.TryParse<ConversionMode>(args[0], out var mode) || !ModeHandlers.TryGetValue(mode, out Action<AppArguments>? value))
        {
            Console.WriteLine("Error.");
            return;
        }
        
        var appArgs = new AppArguments(
            mode,
            args.ElementAtOrDefault(1) ?? string.Empty,
            args.ElementAtOrDefault(2) ?? string.Empty,
            args.ElementAtOrDefault(3) ?? string.Empty,
            args.ElementAtOrDefault(4) ?? string.Empty
        );

        if (new[] { appArgs.LegacyPath, appArgs.LazerPath }.Any(string.IsNullOrWhiteSpace))
        {
            Console.WriteLine("Some required arguments are missing. Aborting.");
            return;
        }

        Console.WriteLine($"Selected Mode: {mode}");
        for (int i = 1; i < args.Length; i++)
            Console.WriteLine($"Path {i}: {args[i]}");
        Console.WriteLine("Starting.");
        ModeHandlers[mode](appArgs);
        Console.WriteLine("Completed.");
    }
    private static List<BeatmapInfo> createBeatmapDifficulties(BeatmapSetInfo beatmapSet, Realm realm, string basePath)
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
                System.Environment.Exit(1);
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
                            if (System.IO.File.Exists(fullsrc) && !string.IsNullOrEmpty(dest))
                            {
                                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));

                                System.IO.File.Copy(fullsrc, dest, false);
                            }
                        }
                        catch (IOException ioEx)
                        {
                            Console.WriteLine($"File access error (IO) while processing {fullsrc}: {ioEx.Message}");
                        }
                        catch (UnauthorizedAccessException authEx)
                        {
                            Console.WriteLine($"Unauthorized access error while processing {fullsrc}: {authEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Unexpected error while processing {fullsrc}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    public static void LegacyToSymbolic(string legacyFilesFolder, string lazerFilesFolder)
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

                    if (File.Exists(fullsrc_sym) && !File.Exists(fullsrc_sym_backup))
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
        try
        {
            var sourceRealm = Realm.GetInstance(sourceConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            System.Environment.Exit(1);
        }
        using (var realm = Realm.GetInstance(sourceConfig))
        {
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
                                        }
                                    }
                                    byte[] hashBytes = sha256.ComputeHash(fileStream);
                                    string _hashStr = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
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
    }

    private static void Populate(BeatmapSetInfo beatmapSet, Realm realm, string basePath)
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
}
