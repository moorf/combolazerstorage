// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Platform;

public class Program
{

    [STAThread]
    static void Main(string[] args)
    {
        ResourceHelper.LoadResourcesDll();
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
}
