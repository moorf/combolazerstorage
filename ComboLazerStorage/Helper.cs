using Realms;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

