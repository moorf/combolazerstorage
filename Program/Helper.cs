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

public class Helper
{
    public static void onMigration(Migration migration, ulong lastSchemaVersion) { }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, uint dwFlags);

    public static string WithSlash(string path)
    {
        if (!string.IsNullOrEmpty(path) && !path.EndsWith("\\"))
        {
            return path + "/";
        }
        return path;
    }
}

