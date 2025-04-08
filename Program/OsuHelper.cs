using osu.Framework.Extensions;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects.Types;
using Realms;

class OsuHelper
{
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
    public static void RecomputeDirectoryHashes(string legacyFolder)
    {

    }

}