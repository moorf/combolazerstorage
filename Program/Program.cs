// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
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

using static OsuMain;

public class Program
{
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
    static void Main(string[] args)
    {
        ResourceHelper.LoadResourcesDll();
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
