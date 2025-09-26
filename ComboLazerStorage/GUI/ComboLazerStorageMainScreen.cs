using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osuTK;
using System.Reflection;

public partial class ComboLazerStorageMainScreen : Screen
{
    private Container mainContent = null!;
    readonly List<string> optionList = new List<string> { "Lazer to Legacy", "Legacy to Lazer" };

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

    [BackgroundDependencyLoader]
    private void load()
    {
        var legacyDirPathText = new OsuSpriteText
        {
            RelativeSizeAxes = Axes.X,
            RelativePositionAxes = Axes.Both,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.2f, 0.2f),
            Font = OsuFont.Default.With(size: 24),
            AllowMultiline = true,
            MaxWidth = 0.3f,
        };
        var lazerDirPathText = new OsuSpriteText
        {
            RelativeSizeAxes = Axes.X,
            RelativePositionAxes = Axes.Both,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.2f, 0.3f),
            Font = OsuFont.Default.With(size: 24),
            AllowMultiline = true,
            MaxWidth = 0.3f,
        };
        var realmFilePathText = new OsuSpriteText
        {
            RelativeSizeAxes = Axes.X,
            RelativePositionAxes = Axes.Both,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.2f, 0.4f),
            Font = OsuFont.Default.With(size: 24),
            AllowMultiline = true,
            MaxWidth = 0.3f,
        };
        var dropdownText = new OsuSpriteText
        {
            Font = OsuFont.Default.With(size: 36),
            RelativeSizeAxes = Axes.X,
            RelativePositionAxes = Axes.Both,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.0f, 0.0f),
            AllowMultiline = true,
            MaxWidth = 600,
        };
        var currentSelectorText = new OsuSpriteText
        {
            Font = OsuFont.Default.With(size: 36),
            RelativeSizeAxes = Axes.X,
            RelativePositionAxes = Axes.Both,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.0f, 0.1f),
            Height = 20,
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
            RelativePositionAxes = Axes.Both,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.0f, 0.2f),
            Width = 0.2f,
            Text = @"Select Legacy File Path",
            Enabled = { Value = true },
            Action = () =>
            {
                legacyPathSelector.Show();
                lazerPathSelector.Hide();
                realmFileSelector.Hide();
                currentSelector.Value = "Selecting: Legacy Directory";
            }
        };

        lazerFileButton = new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            RelativePositionAxes = Axes.Both,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.0f, 0.3f),
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
            RelativePositionAxes = Axes.Both,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.0f, 0.4f),
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
            RelativePositionAxes = Axes.None,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.0f, 0.7f),
            BackgroundColour = Colour4.BlueViolet,
            Width = 0.5f,
            Text = @"Start",
            Enabled = { Value = true },
            Action = () =>
            {
                Task.Run(() => startButtonAction());
            },
        };
        dropdown = new LabelledDropdown<string>
        {
            RelativeSizeAxes = Axes.X,
            RelativePositionAxes = Axes.None,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.0f, 0.5f),
            Width = 0.5f,
            Label = "Select Operation",
            Items = optionList
        };
        schema_version_input = new LabelledNumberBox
        {
            RelativeSizeAxes = Axes.X,
            RelativePositionAxes = Axes.None,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.0f, 0.6f),
            Width = 0.3f,
            Text = getLatestSchemaVersion().ToString(),
            Label = @"Schema version"
        };
        var settingsContainer = new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            RelativePositionAxes = Axes.Both,
            Direction = FillDirection.Vertical,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(0.0f, 0.5f),
            Spacing = new Vector2(0, 10),
            Children = new Drawable[]
            {
            dropdown,
            schema_version_input,
            startButton
            }
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
            legacyPathSelector = new OsuDirectorySelector(initialPath: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
            {
                RelativeSizeAxes = Axes.Both,
                RelativeAnchorPosition = new Vector2(0.5f, 0.0f),
                Width = 0.5f,
                Name = "legacyPathSelector",
            },
            lazerPathSelector = new OsuDirectorySelector(initialPath: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
            {
                RelativeSizeAxes = Axes.Both,
                RelativeAnchorPosition = new Vector2(0.5f, 0.0f),
                Width = 0.5f,
                Name = "lazerPathSelector",
            },
            realmFileSelector = new OsuFileSelector(initialPath: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), validFileExtensions: new[] { ".realm" })
            {
                RelativeSizeAxes = Axes.Both,
                RelativeAnchorPosition = new Vector2(0.5f, 0.0f),
                Width = 0.5f,
                Name = "realmFileSelector",
            },
            settingsContainer,
            legacyFileButton,
            lazerFileButton,
            realmFileButton,
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
    private ulong getLatestSchemaVersion()
    {
        var type = typeof(RealmAccess);
        FieldInfo fi = type.GetField("schema_version", BindingFlags.Static | BindingFlags.NonPublic);

        if (fi == null)
            throw new Exception("Field not found!");

        ulong value = (ulong)Convert.ToInt64(fi.GetValue(null));
        return value;
    }
    private void startButtonAction()
    {
        List<string> arguments = new List<string> { (optionList.IndexOf(mode.Value) + 1).ToString(), legacyDirPath.Value, lazerDirPath.Value, realmFilePath.Value, schema_version };
        ConversionProcessor.ProcessArgsAndExecute(arguments.ToArray());
    }
    private void schema_version_inputChanged(ValueChangedEvent<string> e)
    {
        schema_version = e.NewValue;
    }
    private void dropdownChanged(ValueChangedEvent<string> e)
    {
        if (e.NewValue != null)
        {
            mode.Value = dropdown.Current.Value;
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
}
