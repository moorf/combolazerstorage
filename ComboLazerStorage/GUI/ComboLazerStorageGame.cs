using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Input.Handlers.Tablet;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Graphics.Cursor;
using osu.Game.Overlays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class ComboLazerStorageGame : OsuGameBase
{
    private Bindable<WindowMode>? windowMode;
    private DependencyContainer? dependencies;

    [Cached]
    [Cached(typeof(IBindable<IReadOnlyList<Mod>>))]
    private readonly Bindable<IReadOnlyList<Mod>> mods = new Bindable<IReadOnlyList<Mod>>(Array.Empty<Mod>());

    [Resolved]
    private FrameworkConfigManager frameworkConfig { get; set; }

    protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
        dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

    protected override IDictionary<FrameworkSetting, object> GetFrameworkConfigDefaults() => new Dictionary<FrameworkSetting, object>
        {
            { FrameworkSetting.VolumeUniversal, 0.0d },
        };

    [BackgroundDependencyLoader]
    private void load()
    {

        Ruleset.Value = new OsuRuleset().RulesetInfo;

        var dialogOverlay = new DialogOverlay();
        dependencies.CacheAs(dialogOverlay);

        AddRange(new Drawable[]
        {
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new ComboLazerStorageSceneManager()
                },
                dialogOverlay
        });
    }

    public override void SetHost(GameHost host)
    {
        base.SetHost(host);

        host.Window.CursorState |= CursorState.Hidden;

        var tabletInputHandler = host.AvailableInputHandlers.FirstOrDefault(x => x is OpenTabletDriverHandler && x.IsActive);

        if (tabletInputHandler != null)
        {
            tabletInputHandler.Enabled.Value = false;
        }
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        windowMode = frameworkConfig.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
        windowMode.BindValueChanged(mode => windowMode.Value = WindowMode.Windowed, true);
    }
}

