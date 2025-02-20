// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Input.Handlers.Tablet;
using osu.Game;
using osu.Game.Graphics.Cursor;
using osu.Game.Overlays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.Chat;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osuTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Toolbar;
using osu.Game.Input.Bindings;
using osu.Framework.Extensions;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Rulesets;
using osu.Framework.Platform;
using osu.Game.Graphics.Backgrounds;


public partial class ComboLazerStorageGame : OsuGameBase
{
    private Bindable<WindowMode> windowMode;
    private DependencyContainer dependencies;

    // This overwrites OsuGameBase's SelectedMods to make sure it can't tweak mods when we don't want it to
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

        var notificationDisplay = new NotificationDisplay();
        dependencies.CacheAs(notificationDisplay);

        AddRange(new Drawable[]
        {
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new ComboLazerStorageSceneManager()
                },
                dialogOverlay,
                notificationDisplay
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

public partial class ComboLazerStorageSceneManager : CompositeDrawable
{
    private ScreenStack screenStack;


    private Box hoverGradientBox;

    public const float CONTROL_AREA_HEIGHT = 45;

    [Resolved]
    private Bindable<RulesetInfo> ruleset { get; set; }

    [Resolved]
    private DialogOverlay dialogOverlay { get; set; }

    [Cached]
    private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

    public ComboLazerStorageSceneManager()
    {
        RelativeSizeAxes = Axes.Both;
    }

    [BackgroundDependencyLoader]
    private void load(OsuColour colours)
    {
        InternalChildren = new Drawable[]
        {
                new PopoverContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ColumnDimensions = new[] { new Dimension() },
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize), new Dimension() },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new HoverHandlingContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = CONTROL_AREA_HEIGHT,
                                    Hovered = e =>
                                    {
                                        hoverGradientBox.FadeIn(100);
                                        return false;
                                    },
                                    Unhovered = e =>
                                    {
                                        hoverGradientBox.FadeOut(100);
                                    },
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            Colour = OsuColour.Gray(0.1f),
                                            RelativeSizeAxes = Axes.Both,
                                        },
                                        new OsuSpriteText
                                        {
                                            Colour = Colour4.Wheat,
                                            Text = " Combo Lazer Storage",
                                            Font = OsuFont.Default.With(size: 40, weight: "bold")

                                        },
                                        new FillFlowContainer
                                        {
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                            Direction = FillDirection.Horizontal,
                                            RelativeSizeAxes = Axes.Y,
                                            AutoSizeAxes = Axes.X,
                                            Spacing = new Vector2(5),
                                            Children = new Drawable[]
                                            {
                                                new SettingsButton()
                                            }
                                        },
                                    },
                                }
                            },
                            new Drawable[]
                            {
                                new ScalingContainer(ScalingMode.Everything)
                                {
                                    Depth = 1,
                                    Children = new Drawable[]
                                    {
                                        screenStack = new ScreenStack
                                        {
                                            RelativeSizeAxes = Axes.Both
                                        },
                                        hoverGradientBox = new Box
                                        {
                                            Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(1.0f), Color4.Black.Opacity(1)),
                                            RelativeSizeAxes = Axes.X,
                                            Height = 100,
                                            Alpha = 0
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

        };
        setScreen(new SimulateScreen());
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
    }

    private void setScreen(Screen screen)
    {
        if (screenStack.CurrentScreen != null)
        {
            if (screenStack.CurrentScreen is ComboLazerStorageScreen { ShouldShowConfirmationDialogOnSwitch: true })
            {
                dialogOverlay.Push(new ConfirmDialog("Are you sure?", () =>
                {
                    screenStack.Exit();
                    screenStack.Push(screen);
                }));
                return;
            }

            screenStack.Exit();
        }

        screenStack.Push(screen);
    }
}
public partial class SimulateScreen : ComboLazerStorageScreen
{
    private Container mainContent = null!;
    readonly List<string> optionList = new List<string> { "Lazer to Legacy", "Legacy to Symlinks", "Symlinks to Database" };
    
    LabelledDropdown<string> dropdown;
    private RoundedButton legacyFileButton;
    private RoundedButton lazerFileButton;
    private RoundedButton startButton;
    private RoundedButton realmFileButton;
    private OsuDirectorySelector legacyPathSelector;
    private OsuDirectorySelector lazerPathSelector;
    private OsuFileSelector realmFileSelector;
    private LabelledNumberBox schema_version_input;
    private Bindable<string> currentSelector = new Bindable<string>("No Selector Opened");

    public string schema_version;
    public Bindable<string> mode = new Bindable<string>("1");
    private Bindable<string> legacyDirPath = new Bindable<string>("");
    private Bindable<string> lazerDirPath = new Bindable<string>("");
    public Bindable<string> realmFilePath = new Bindable<string>("None Selected");
    public override bool ShouldShowConfirmationDialogOnSwitch => true;

    [BackgroundDependencyLoader]
    private void load()
    {
        var legacyDirPathText = new OsuSpriteText
        {
            RelativeSizeAxes = Axes.X,
            Position = new Vector2(250f, 112f ),
            Font = OsuFont.Default.With(size: 24)
        };
        var lazerDirPathText = new OsuSpriteText
        {
            RelativeSizeAxes = Axes.X,
            Position = new Vector2(250f, 212f),
            Font = OsuFont.Default.With(size: 24),
        };
        var realmFilePathText = new OsuSpriteText
        {
            RelativeSizeAxes = Axes.X,
            Position = new Vector2(250f, 312f),
            Font = OsuFont.Default.With(size: 24)
        };
        var dropdownText = new OsuSpriteText
        {
            Font = OsuFont.Default.With(size: 36),
            AllowMultiline = true,
            MaxWidth = 600,
        };
        var currentSelectorText = new OsuSpriteText
        {
            Font = OsuFont.Default.With(size: 36),
            Position = new Vector2(0f, 50f),
            AllowMultiline = true,
            MaxWidth = 600,
        };
        legacyDirPathText.Current = legacyDirPath;
        lazerDirPathText.Current = lazerDirPath;
        realmFilePathText.Current = realmFilePath;
        dropdownText.Current = mode;
        currentSelectorText.Current = currentSelector;
        legacyFileButton = new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Position = new Vector2(0, 100f),
            Width = 0.2f,
            Text = @"Select Legacy File Path",
            Enabled = { Value = true },
            Action = () =>
            {
                legacyPathSelector.Show();
                lazerPathSelector.Hide();
                realmFileSelector.Hide();
                currentSelector.Value = "Selecting: Legacy Directory";
            },
        };

        lazerFileButton = new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Position = new Vector2(0, 200f),
            Width = 0.2f,
            Text = @"Select Lazer File Path",
            Enabled = { Value = true },
            Action = () =>
            {
                legacyPathSelector.Hide();
                lazerPathSelector.Show();
                realmFileSelector.Hide();
                currentSelector.Value = "Selecting: Lazer Directory";
            },
        };

        realmFileButton = new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Position = new Vector2(0, 300f),
            Width = 0.2f,
            Text = @"Select Realm File",
            Enabled = { Value = true },
            Action = () =>
            {
                legacyPathSelector.Hide();
                lazerPathSelector.Hide();
                realmFileSelector.Show();
                currentSelector.Value = "Selecting: Realm File";
            },
        };

        startButton = new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Position = new Vector2(0, 600f),
            BackgroundColour = Colour4.BlueViolet,
            Width = 0.5f,
            Text = @"Start",
            Enabled = { Value = true },
            Action = () =>
            {
                startButtonAction();
            },
        };
        schema_version_input = new LabelledNumberBox
        {
            RelativeSizeAxes = Axes.X,
            Position = new Vector2(0, 500f),
            Width = 0.3f,
            Text = @"47",
            Label = @"Schema version"
        };
        dropdown = new LabelledDropdown<string>
        {
            RelativeSizeAxes = Axes.X,
            Position = new Vector2(0, 400f),
            Width = 0.5f,
            Label = "Select Operation",
            Items = optionList
        };
        InternalChildren = new Drawable[]
        {
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colour4.DarkOrchid,
                Alpha = 0.2f,
                
            },
            new TrianglesV2
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colour4.LightGoldenrodYellow,
                Alpha = 0.1f,
            },
            legacyDirPathText,
            lazerDirPathText,
            realmFilePathText,
            dropdownText,
            currentSelectorText,
            legacyPathSelector = new OsuDirectorySelector()
            {
                RelativeSizeAxes = Axes.Both,
                RelativeAnchorPosition = new Vector2(0.5f, 0.0f),
                Width = 0.5f,
                Name = "legacyPathSelector",
            },
            lazerPathSelector = new OsuDirectorySelector()
            {
                RelativeSizeAxes = Axes.Both,
                RelativeAnchorPosition = new Vector2(0.5f, 0.0f),
                Width = 0.5f,
                Name = "lazerPathSelector",
            },
            realmFileSelector = new OsuFileSelector(validFileExtensions: new[] { ".realm" })
            {
                RelativeSizeAxes = Axes.Both,
                RelativeAnchorPosition = new Vector2(0.5f, 0.0f),
                Width = 0.5f,
                Name = "realmFileSelector",
            },

            dropdown,
            legacyFileButton,
            lazerFileButton,
            realmFileButton,
            schema_version_input,
            startButton,
        };
        legacyPathSelector.Hide();
        lazerPathSelector.Hide();
        realmFileSelector.Hide();
        dropdown.Current.BindValueChanged(dropdownChanged, true);
        schema_version_input.Current.BindValueChanged(schema_version_inputChanged, true);
        realmFileSelector.CurrentFile.BindValueChanged(realmFileChanged, true);
        legacyPathSelector.CurrentPath.BindValueChanged(legacyPathChanged, true);
        lazerPathSelector.CurrentPath.BindValueChanged(lazerPathChanged, true);
    }
    private void schema_version_inputChanged(ValueChangedEvent<string> e)
    {
        schema_version = e.NewValue;
    }
    private void startButtonAction()
    {
        string command = (optionList.IndexOf(mode.Value) + 1).ToString() + " " + legacyDirPath.Value + " " + lazerDirPath.Value + " " + realmFilePath.Value + " " + schema_version + " ";
        Console.WriteLine(command);
        List<string> arguments = new List<string> { (optionList.IndexOf(mode.Value) + 1).ToString(), legacyDirPath.Value, lazerDirPath.Value, realmFilePath.Value, schema_version };
        Program.MainApp(arguments.ToArray());
    }
        private void dropdownChanged(ValueChangedEvent<string> e)
    {
        if (e.NewValue == optionList[1])
        {
            realmFileButton.Alpha = 0;
            realmFileButton.Enabled.Value = false;
        }
        else
        {
            realmFileButton.Alpha = 1;
            realmFileButton.Enabled.Value = true;
        }
        if (e.NewValue != null)
        {
            mode.Value = "Mode: " + dropdown.Current.Value;
        }
    }
    private void legacyPathChanged(ValueChangedEvent<DirectoryInfo> e)
    {
        if (e.NewValue != null)
        {
            legacyDirPath.Value = e.NewValue.FullName;
        }
    }
    private void lazerPathChanged(ValueChangedEvent<DirectoryInfo> e)
    {
        if (e.NewValue != null)
        {
            lazerDirPath.Value = e.NewValue.FullName;
        }
    }
    private void realmFileChanged(ValueChangedEvent<FileInfo> selectedFile)
    {
        if (selectedFile.NewValue != null)
        {
            realmFilePath.Value = selectedFile.NewValue.FullName;
        }
    }
    string selectFile()
    {
        return "D:\\osu!\\file1.txt";
    }
}

public abstract partial class ComboLazerStorageScreen : Screen
{
    public abstract bool ShouldShowConfirmationDialogOnSwitch { get; }
}
public partial class SettingsButton : ToolbarButton, IHasPopover
{
    protected override Anchor TooltipAnchor => Anchor.TopRight;

    public SettingsButton()
    {
        Hotkey = GlobalAction.ToggleSettings;
        TooltipMain = "Settings";

        SetIcon(new ScreenSelectionButtonIcon(FontAwesome.Solid.Cog) { IconSize = new Vector2(70) });
    }

    public Popover GetPopover() => new SettingsPopover();

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

public partial class SettingsPopover : OsuPopover
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


public enum Settings
{
    ClientId,
    ClientSecret,
    DefaultPath,
    CachePath
}

public class SettingsManager : IniConfigManager<Settings>
{
    protected override string Filename => "combolazerstorage-config.ini";

    public SettingsManager(Storage storage)
        : base(storage)
    {
    }

    protected override void InitialiseDefaults()
    {
        SetDefault(Settings.ClientId, string.Empty);
        SetDefault(Settings.ClientSecret, string.Empty);
        SetDefault(Settings.DefaultPath, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        SetDefault(Settings.CachePath, Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "cache"));
    }
}

public partial class NotificationDisplay : Container
{
    private readonly FillFlowContainer content;

    public NotificationDisplay()
    {
        RelativeSizeAxes = Axes.Both;

        Children = new Drawable[]
        {
                content = new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    Origin = Anchor.TopRight,
                    Anchor = Anchor.TopRight,
                    RelativeSizeAxes = Axes.Y,
                    Height = 1,
                    Width = 350,
                    Padding = new MarginPadding(20),
                    Spacing = new Vector2(0, 10)
                }
        };
    }

    public void Display(Notification notification) => Schedule(() =>
    {
        content.Add(notification);

        notification.FadeIn(1500, Easing.OutQuint)
                    .Delay(5000)
                    .FadeOut(1500, Easing.OutQuint)
                    .Finally(_ => content.Remove(notification, true));
    });
}

public partial class Notification : Container
{
    public Notification(LocalisableString text)
    {
        Anchor = Anchor.BottomCentre;
        Origin = Anchor.BottomCentre;
        AutoSizeAxes = Axes.Y;
        RelativeSizeAxes = Axes.X;
        Alpha = 0;
        Masking = true;
        CornerRadius = 10;

        InternalChildren = new Drawable[]
        {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.DarkSlateGray,
                    Alpha = 0.95f
                },
                new OsuSpriteText
                {
                    Padding = new MarginPadding(10),
                    Name = "Description",
                    AllowMultiline = true,
                    RelativeSizeAxes = Axes.X,
                    Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Text = text
                }
        };
    }
}
