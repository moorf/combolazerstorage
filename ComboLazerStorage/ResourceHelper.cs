using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Provides osu.Game.Resources.dll to the rest of the program. 
/// </summary>
/// <remarks>
/// TODO: Give user the ability to locate existing libraries.
/// </remarks>
public class ResourceHelper
{
    const string packageVersion = "2025.321.0";
    public async static Task<string> DL()
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync("https://www.nuget.org/api/v2/package/ppy.osu.Game.Resources/" + packageVersion);
            response.EnsureSuccessStatusCode();
            var packageBytes = await response.Content.ReadAsByteArrayAsync();

            var packagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "package.nupkg");
            await File.WriteAllBytesAsync(packagePath, packageBytes);

            Console.WriteLine("Package downloaded to " + packagePath);
            return packagePath;
        }
    }

    public static void LoadResourcesDll()
    {
        string packageId = "ppy.osu.Game.Resources";

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string librariesPath = baseDirectory;
        var a = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*", SearchOption.AllDirectories);
        var b = a.FirstOrDefault(s => s.Contains("osu.Game.Resources.dll") && !s.Contains("ppy.osu.Game.Resources.dll"));
        string dllPath = b ?? "";
        string destFilePath = dllPath;
        if (dllPath.Length > 0 && File.Exists(dllPath))
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
                        var _path = Path.GetDirectoryName(destFilePath);
                        if (_path != null)
                        {
                            Directory.CreateDirectory(_path);
                        }
                        else
                        {
                            Console.WriteLine("Path is wrong.");
                            System.Environment.Exit(1);
                        }
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
            dllPath = b ?? "";
            destFilePath = dllPath;
        }
        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "package.nupkg"));
        if (destFilePath.Length > 0 && File.Exists(destFilePath))
        {
            var assembly = Assembly.LoadFrom(destFilePath);
            Console.WriteLine($"Assembly Loaded: {assembly.FullName}");
        }
    }
}

