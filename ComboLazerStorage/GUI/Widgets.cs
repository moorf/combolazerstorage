// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;
using osu.Framework.Allocation;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Overlays.Toolbar;
using osu.Game.Input.Bindings;
using osu.Framework.Extensions;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
public partial class AboutButton : ToolbarButton, IHasPopover
{
    protected override Anchor TooltipAnchor => Anchor.TopRight;

    public AboutButton()
    {
        Hotkey = GlobalAction.ToggleSettings;
        TooltipMain = "About";

        SetIcon(new ScreenSelectionButtonIcon(FontAwesome.Solid.Cog) { IconSize = new Vector2(70) });
    }

    public Popover GetPopover() => new AboutPopover();

    protected override bool OnClick(ClickEvent e)
    {
        this.ShowPopover();
        return base.OnClick(e);
    }
}

public partial class ScreenSelectionButton : ToolbarButton
{
    public ScreenSelectionButton(string title, IconUsage? icon = null, GlobalAction? hotkey = null)
    {
        Hotkey = hotkey;
        TooltipMain = title;

        SetIcon(new ScreenSelectionButtonIcon(icon) { IconSize = new Vector2(25) });
    }
}

public partial class ScreenSelectionButtonIcon : IconPill
{
    public ScreenSelectionButtonIcon(IconUsage? icon = null)
        : base(icon ?? FontAwesome.Solid.List)
    {
    }

    public override LocalisableString TooltipText => string.Empty;
}

public partial class AboutPopover : OsuPopover
{

    [BackgroundDependencyLoader]
    private void load()
    {
        Add(new Container
        {
            AutoSizeAxes = Axes.Y,
            Width = 1000,
            Children = new Drawable[]
            {
                new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(18),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                        {
                            Text = @"Made by moorf, credits to peppy. Copyright (c) ppy Pty Ltd <contact@ppy.sh>.",
                            Font = OsuFont.Default.With(size: 24)
                        },
                    }
                }
            }
        });
    }
}


