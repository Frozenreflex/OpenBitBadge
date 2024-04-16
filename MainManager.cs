using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OpenBitBadge;
using FileAccess = Godot.FileAccess;

public partial class MainManager : Node
{
    //TODO: cleanup
    [Export] public SpinBox DeviceWidthBox;
    [Export] public Label FontPathLabel;
    [Export] public Button FontSelectButton;
    [Export] public Button FontClearButton;
    [Export] public SpinBox FontScale;
    [Export] public SpinBox TextOffsetX;
    [Export] public SpinBox TextOffsetY;
    [Export] public PanelContainer DrawSpaceParent;
    [Export] public TextureRect DrawImage;
    [Export] public TextureRect ByteGrid;
    [Export] public PanelContainer DrawSpace;
    [Export] public TextureRect DeviceWidthSeparators;
    [Export] public TabBar Tabs;
    [Export] public CheckButton EnabledButton;
    [Export] public CheckButton FlashButton;
    [Export] public CheckButton LampButton;
    [Export] public SpinBox Speed;
    [Export] public OptionButton AnimationMode;
    [Export] public OptionButton ImageMode;
    [Export] public LineEdit TextEdit;
    [Export] public Control TextEntryRoot;
    [Export] public SubViewport TextRenderViewport;
    [Export] public Label TextRenderText;
    [Export] public ProgressBar ByteLimitBar;
    [Export] public Label ByteLimitText;
    [Export] public Button ExportButton;
    [Export] public Button SaveButton;
    [Export] public Button LoadButton;
    [Export] public Font DefaultFont;
    public int CurrentSelected => Tabs.CurrentTab;

    public static readonly List<Action> ActionQueue = new();

    public Message CurrentMessage => Packet.Messages[CurrentSelected];

    public BadgePacket Packet = new();

    public override void _Ready()
    {
        Tabs.TabChanged += TabsOnTabChanged;
        TextEdit.TextChanged += TextEditOnTextChanged;
        EnabledButton.Toggled += EnabledButtonOnToggled;
        ExportButton.Pressed += ExportButtonOnPressed;
        SaveButton.Pressed += SaveButtonOnPressed;
        LoadButton.Pressed += LoadButtonOnPressed;
        Speed.ValueChanged += SpeedOnValueChanged;
        LampButton.Toggled += LampButtonOnToggled;
        FlashButton.Toggled += FlashButtonOnToggled;
        AnimationMode.ItemSelected += AnimationModeOnItemSelected;
        ImageMode.ItemSelected += ImageModeOnItemSelected;
        DeviceWidthBox.ValueChanged += DeviceWidthBoxOnValueChanged;
        DeviceWidthBoxOnValueChanged(DeviceWidthBox.Value);
        FontSelectButton.Pressed += FontSelectButtonOnPressed;
        TextOffsetX.ValueChanged += TextOffsetXOnValueChanged;
        TextOffsetY.ValueChanged += TextOffsetYOnValueChanged;
        FontScale.ValueChanged += FontScaleOnValueChanged;
        FontClearButton.Pressed += FontClearButtonOnPressed;
        DrawSpace.GuiInput += DrawSpaceOnGuiInput;
        DeviceHeightChanged(Packet.ImageHeight);
        Update();
    }

    private void LoadButtonOnPressed()
    {
        var window = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            UseNativeDialog = true,
        };
        window.AddFilter("*.obbs", ".obbs");
        window.FileSelected += LoadWindowOnFileSelected;
        GetViewport().AddChild(window);
        window.PopupCentered();
    }

    private void LoadWindowOnFileSelected(string path)
    {
        var reader = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var data = reader.GetPascalString();
        reader.Close();
        Packet = JsonSerializer.Deserialize<BadgePacket>(data);
        Update();
    }

    private void SaveButtonOnPressed()
    {
        var window = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            UseNativeDialog = true,
        };
        window.AddFilter("*.obbs", ".obbs");
        window.FileSelected += SaveWindowOnFileSelected;
        GetViewport().AddChild(window);
        window.PopupCentered();
    }

    private void SaveWindowOnFileSelected(string path)
    {
        var data = JsonSerializer.Serialize(Packet);
        var correctPath = Path.ChangeExtension(path, "obbs");
        var writer = FileAccess.Open(correctPath, FileAccess.ModeFlags.Write);
        writer.StorePascalString(data);
        writer.Close();
    }

    private void ImageModeOnItemSelected(long index)
    {
        CurrentMessage.Draw = index != 0;
        Update();
    }

    private void DrawSpaceOnGuiInput(InputEvent @event)
    {
        if (!CurrentMessage.Draw) return;
        if (@event is not (InputEventMouseButton or InputEventMouseMotion)) return;
        
        var left = Input.IsMouseButtonPressed(MouseButton.Left);
        var right = Input.IsMouseButtonPressed(MouseButton.Right);
        if (!left && !right) return;
        
        var pos = (Vector2I)(((InputEventMouse)@event).Position / 8);
        DrawPixel(pos, left);
        DrawSpace.AcceptEvent();
        Update();
    }

    private void DrawPixel(Vector2I pos, bool state)
    {
        var current = CurrentMessage;
        var horizontalByte = pos.X / 8;
        var byteOffset = 7 - (pos.X % 8);

        //if we are somehow out of bounds, abort
        if (pos.Y >= current.Image.GetLength(0) || horizontalByte >= current.Image.GetLength(1)) return;
        
        var modifyingByte = current.Image[pos.Y, horizontalByte];
        if (state)
            modifyingByte = (byte)(modifyingByte | (1 << byteOffset));
        else
            modifyingByte = (byte)(modifyingByte & ~(1 << byteOffset));
        current.Image[pos.Y, horizontalByte] = modifyingByte;
    }

    private void EnabledButtonOnToggled(bool toggledon)
    {
        CurrentMessage.Used = toggledon;
        Update();
    }

    private void FontClearButtonOnPressed() => WindowOnFileSelected("");

    private void FontScaleOnValueChanged(double value)
    {
        CurrentMessage.FontScale = (float)value;
        Update();
    }

    private void TextOffsetXOnValueChanged(double value)
    {
        var current = CurrentMessage;
        current.TextOffset = current.TextOffset with { X = (float)value };
        Update();
    }

    private void TextOffsetYOnValueChanged(double value)
    {
        var current = CurrentMessage;
        current.TextOffset = current.TextOffset with { Y = (float)value };
        Update();
    }

    private void FontSelectButtonOnPressed()
    {
        var window = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            UseNativeDialog = true,
        };
        window.AddFilter("*.ttf,*.ttc,*.otf,*.otc,*.woff,*.woff2,*.pfb,*.pfm,*.fnt,*.font", "Font");
        window.FileSelected += WindowOnFileSelected;
        GetViewport().AddChild(window);
        window.PopupCentered();
    }

    private void WindowOnFileSelected(string path)
    {
        CurrentMessage.FontFile = path;
        FontPathLabel.Text = path;
        Update();
    }

    private void DeviceWidthBoxOnValueChanged(double value)
    {
        var val = (int)value;
        var width = val * 8;
        var height = Packet.ImageHeight * 8;
        var image = Image.Create(width, height, false, Image.Format.Rgba8);
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            image.SetPixel(x, y, x is 0 or 1 ? Colors.Green : Colors.Transparent);

        var texture = new ImageTexture();
        texture.SetImage(image);

        DeviceWidthSeparators.Texture = texture;
    }

    private void DeviceHeightChanged(int value)
    {
        const int width = 8 * 8;
        var height = value * 8;
        var image = Image.Create(width, height, false, Image.Format.Rgba8);
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            image.SetPixel(x, y, x == 0 ? Colors.Red : Colors.Transparent);

        var texture = new ImageTexture();
        texture.SetImage(image);

        ByteGrid.Texture = texture;
    }

    private void AnimationModeOnItemSelected(long index)
    {
        CurrentMessage.ShowMode = (ShowMode)index;
        Update();
    }

    private void FlashButtonOnToggled(bool toggledon) => CurrentMessage.Flash = toggledon;
    private void LampButtonOnToggled(bool toggledon) => CurrentMessage.Lamp = toggledon;
    private void SpeedOnValueChanged(double value) => CurrentMessage.Speed = (byte)(value - 1);

    private void ExportButtonOnPressed() => USBManager.PushToBadge(Packet);

    private void TextEditOnTextChanged(string newtext)
    {
        CurrentMessage.Text = newtext;
        Update();
    }

    private void TabsOnTabChanged(long tab) => CallDeferred(MethodName.Update);

    public override void _Process(double delta)
    {
        var queue = new List<Action>(ActionQueue);
        ActionQueue.Clear();
        foreach (var a in queue) a();
    }

    public Font Font
    {
        get
        {
            var current = CurrentMessage;
            if (current.FontFile != _previousFontPath)
            {
                _loadedFont = null;
                _previousFontPath = current.FontFile;
                if (FileAccess.FileExists(_previousFontPath))
                {
                    try
                    {
                        var fontObject = new FontFile();
                        var load = FileAccess.Open(_previousFontPath, FileAccess.ModeFlags.Read);
                        var buffer = load.GetBuffer((long)load.GetLength());
                        load.Close();
                        fontObject.Data = buffer;
                        _loadedFont = fontObject;
                    }
                    catch
                    {
                    }
                }
            }

            return _loadedFont ?? DefaultFont;
        }
    }

    private Font _loadedFont;
    private string _previousFontPath;

    public void Update()
    {
        var currentMessage = CurrentMessage;
        EnabledButton.ButtonPressed = currentMessage.Used;
        FlashButton.ButtonPressed = currentMessage.Flash;
        LampButton.ButtonPressed = currentMessage.Lamp;
        AnimationMode.Selected = (int)currentMessage.ShowMode;
        ImageMode.Selected = currentMessage.Draw ? 1 : 0;
        Speed.Value = currentMessage.Speed + 1;
        TextOffsetX.Value = currentMessage.TextOffset.X;
        TextOffsetY.Value = currentMessage.TextOffset.Y;
        FontScale.Value = currentMessage.FontScale;
        if (TextEdit.Text != currentMessage.Text) TextEdit.Text = currentMessage.Text;

        TextRenderText.Text = currentMessage.Text;
        TextRenderText.Set("theme_override_fonts/font", Font);
        TextRenderText.Scale = Vector2.One * currentMessage.FontScale * 0.5f;
        TextRenderText.Position = currentMessage.TextOffset;

        ExportButton.Disabled = !Packet.Valid;

        DrawSpaceParent.CustomMinimumSize = DrawSpaceParent.CustomMinimumSize with { Y = Packet.ImageHeight * 8 };
        TextEntryRoot.Visible = !currentMessage.Draw;
        if (!currentMessage.Draw) ActionQueue.Add(UpdateText);
        ActionQueue.Add(UpdateDisplay);
    }

    public void UpdateText() => ActionQueue.Add(RenderTextToImage);

    public void UpdateDisplay()
    {
        ActionQueue.Add(RenderImage);
        ActionQueue.Add(UpdateByteLimit);
    }

    public void UpdateByteLimit()
    {
        var bytes = Packet.ToByteArray().Length;
        ByteLimitBar.Value = bytes;
        ByteLimitText.Text = $"{bytes:0000} / 4096 Bytes";
    }

    public void RenderImage()
    {
        var texture = new ImageTexture();
        var height = Packet.ImageHeight;
        const int width = MagicConstants.MaxMessageLength * 8;
        var image = Image.Create(width, height, false, Image.Format.Rgba8);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < MagicConstants.MaxMessageLength; x++)
        {
            var value = CurrentMessage.Image[y, x];
            for (var i = 0; i < 8; i++)
            {
                var on = (value << i & 0b10000000) > 0;
                var pos = new Vector2I((x * 8) + i, y);
                image.SetPixelv(pos, on ? Colors.White : Colors.Black);
            }
        }

        texture.SetImage(image);

        DrawImage.Texture = texture;
    }

    public void RenderTextToImage()
    {
        var textRenderViewport = TextRenderViewport.GetTexture().GetImage();
        //textRenderViewport.SavePng("user://test.png");
        for (var y = 0; y < MagicConstants.MaxMessageHeight; y++)
        for (var x = 0; x < MagicConstants.MaxMessageLength; x++)
        {
            byte val = 0;
            var pos = new Vector2I(x * 8, y);
            for (var i = 0; i < 8; i++)
            {
                var grabPixel = textRenderViewport.GetPixelv(pos + new Vector2I(7 - i, 0));
                var value = (byte)(grabPixel.A >= 0.5f ? 0b10000000 : 0);
                val = (byte)(val >> 1 | value);
            }

            CurrentMessage.Image[y, x] = val;
        }
    }
}