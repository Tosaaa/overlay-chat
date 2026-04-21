using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using OverlayChat.Client.Models;
using OverlayChat.Client.Services;

namespace OverlayChat.Client;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const int WmHotKey = 0x0312;
    private const int WmNcHitTest = 0x0084;
    private const int ToggleHotKeyId = 0x5001;
    private const int FocusInputHotKeyId = 0x5002;
    private const uint VkReturn = 0x0D;
    private const int HtClient = 1;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const double ResizeBorderThickness = 8;

    private readonly ChatWebSocketClient _chatClient = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly ClientSettings _settings;
    private readonly string _settingsPath;

    private HwndSource? _hwndSource;
    private IntPtr _windowHandle;
    private bool _isClickThrough;
    private bool _toggleHotkeyRegistered;
    private bool _focusInputHotkeyRegistered;
    private bool _isSettingsOpen;
    private bool _focusInputWithEnterEnabled;
    private double _overlayOpacity = 0.9;
    private double _chatFontSize = 14;
    private string _chatTextColorHex = "#FFFFFFFF";
    private Brush _chatTextBrush = Brushes.White;
    private Brush _overlayBackgroundBrush = new SolidColorBrush(Color.FromArgb(153, 0, 0, 0));
    private double _chatColorR = 255;
    private double _chatColorG = 255;
    private double _chatColorB = 255;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set
        {
            if (_isSettingsOpen == value)
            {
                return;
            }

            _isSettingsOpen = value;
            OnPropertyChanged();
        }
    }

    public bool FocusInputWithEnterEnabled
    {
        get => _focusInputWithEnterEnabled;
        set
        {
            if (_focusInputWithEnterEnabled == value)
            {
                return;
            }

            _focusInputWithEnterEnabled = value;
            OnPropertyChanged();
        }
    }

    public double OverlayOpacity
    {
        get => _overlayOpacity;
        set
        {
            var clamped = Math.Clamp(value, 0.3, 1.0);
            if (Math.Abs(_overlayOpacity - clamped) < 0.001)
            {
                return;
            }

            _overlayOpacity = clamped;
            OnPropertyChanged();
            UpdateOverlayBackgroundBrush();
        }
    }

    public double ChatFontSize
    {
        get => _chatFontSize;
        set
        {
            var clamped = Math.Clamp(value, 10, 28);
            if (Math.Abs(_chatFontSize - clamped) < 0.001)
            {
                return;
            }

            _chatFontSize = clamped;
            OnPropertyChanged();
        }
    }

    public string ChatTextColorHex
    {
        get => _chatTextColorHex;
        private set
        {
            if (_chatTextColorHex == value)
            {
                return;
            }

            _chatTextColorHex = value;
            OnPropertyChanged();
        }
    }

    public Brush OverlayBackgroundBrush
    {
        get => _overlayBackgroundBrush;
        private set
        {
            _overlayBackgroundBrush = value;
            OnPropertyChanged();
        }
    }

    public double ChatColorR
    {
        get => _chatColorR;
        set
        {
            var clamped = Math.Clamp(value, 0, 255);
            if (Math.Abs(_chatColorR - clamped) < 0.001)
            {
                return;
            }

            _chatColorR = clamped;
            OnPropertyChanged();
            UpdateChatBrushFromRgb();
        }
    }

    public double ChatColorG
    {
        get => _chatColorG;
        set
        {
            var clamped = Math.Clamp(value, 0, 255);
            if (Math.Abs(_chatColorG - clamped) < 0.001)
            {
                return;
            }

            _chatColorG = clamped;
            OnPropertyChanged();
            UpdateChatBrushFromRgb();
        }
    }

    public double ChatColorB
    {
        get => _chatColorB;
        set
        {
            var clamped = Math.Clamp(value, 0, 255);
            if (Math.Abs(_chatColorB - clamped) < 0.001)
            {
                return;
            }

            _chatColorB = clamped;
            OnPropertyChanged();
            UpdateChatBrushFromRgb();
        }
    }

    public Brush ChatTextBrush
    {
        get => _chatTextBrush;
        private set
        {
            _chatTextBrush = value;
            OnPropertyChanged();
        }
    }

    public MainWindow()
    {
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _settings = SettingsLoader.Load(_settingsPath);
        ApplyAppearanceSettings(_settings.Appearance);

        InitializeComponent();
        DataContext = this;
        Messages.CollectionChanged += OnMessagesCollectionChanged;
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var uri = BuildConnectionUri(_settings.Connection);

        try
        {
            await _chatClient.ConnectAsync(uri, _cts.Token);
            Messages.Add(new ChatMessage
            {
                Name = "system",
                Text = $"Connected as {_settings.Connection.Name} (room: {_settings.Connection.Room})",
            });

            _ = _chatClient.ReceiveLoopAsync(async msg =>
            {
                await Dispatcher.InvokeAsync(() => Messages.Add(msg));
            }, _cts.Token);
        }
        catch
        {
            Messages.Add(new ChatMessage { Name = "system", Text = "Could not connect to server." });
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(_windowHandle);
        _hwndSource?.AddHook(WndProc);

        if (!TryParseHotkey(_settings.Overlay.ToggleHotkey, out var modifiers, out var virtualKey))
        {
            _settings.Overlay.ToggleHotkey = "Ctrl+Shift+O";
            TryParseHotkey(_settings.Overlay.ToggleHotkey, out modifiers, out virtualKey);
        }

        _toggleHotkeyRegistered = RegisterHotKey(_windowHandle, ToggleHotKeyId, modifiers, virtualKey);

        FocusInputWithEnterEnabled = _settings.Overlay.FocusInputWithEnter;
        UpdateFocusInputHotkeyRegistration();

        if (_settings.Overlay.StartClickThrough)
        {
            SetClickThrough(true);
        }

        if (!_toggleHotkeyRegistered)
        {
            Messages.Add(new ChatMessage { Name = "system", Text = "Hotkey registration failed." });
            return;
        }

        Messages.Add(new ChatMessage
        {
            Name = "system",
            Text = $"Click-through hotkey: {_settings.Overlay.ToggleHotkey}",
        });
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _cts.Cancel();
        Messages.CollectionChanged -= OnMessagesCollectionChanged;

        if (_toggleHotkeyRegistered && _windowHandle != IntPtr.Zero)
        {
            _ = UnregisterHotKey(_windowHandle, ToggleHotKeyId);
        }

        if (_focusInputHotkeyRegistered && _windowHandle != IntPtr.Zero)
        {
            _ = UnregisterHotKey(_windowHandle, FocusInputHotKeyId);
        }

        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = Dispatcher.InvokeAsync(() => MessagesScrollViewer.ScrollToEnd());
    }

    private static Uri BuildConnectionUri(ConnectionSettings connection)
    {
        var baseUrl = string.IsNullOrWhiteSpace(connection.ServerUrl)
            ? "ws://127.0.0.1:8080/ws"
            : connection.ServerUrl;

        var separator = baseUrl.Contains('?') ? '&' : '?';
        var builder = new StringBuilder(baseUrl);
        builder.Append(separator);
        builder.Append("name=");
        builder.Append(Uri.EscapeDataString(connection.Name ?? "player"));
        builder.Append("&room=");
        builder.Append(Uri.EscapeDataString(connection.Room ?? "default"));

        if (!string.IsNullOrWhiteSpace(connection.RoomKey))
        {
            builder.Append("&key=");
            builder.Append(Uri.EscapeDataString(connection.RoomKey));
        }

        return new Uri(builder.ToString());
    }

    private async void InputBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        await SendCurrentInputAsync();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmNcHitTest && !_isClickThrough)
        {
            var hit = HitTestForResize(lParam);
            if (hit != HtClient)
            {
                handled = true;
                return new IntPtr(hit);
            }
        }

        if (msg == WmHotKey && wParam.ToInt32() == ToggleHotKeyId)
        {
            ToggleClickThrough();
            handled = true;
        }

        if (msg == WmHotKey && wParam.ToInt32() == FocusInputHotKeyId)
        {
            EnterChatInputMode();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private int HitTestForResize(IntPtr lParam)
    {
        var screenX = (short)(lParam.ToInt64() & 0xFFFF);
        var screenY = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        var point = PointFromScreen(new Point(screenX, screenY));

        var onLeft = point.X >= 0 && point.X < ResizeBorderThickness;
        var onRight = point.X <= ActualWidth && point.X > ActualWidth - ResizeBorderThickness;
        var onTop = point.Y >= 0 && point.Y < ResizeBorderThickness;
        var onBottom = point.Y <= ActualHeight && point.Y > ActualHeight - ResizeBorderThickness;

        if (onTop && onLeft)
        {
            return HtTopLeft;
        }
        if (onTop && onRight)
        {
            return HtTopRight;
        }
        if (onBottom && onLeft)
        {
            return HtBottomLeft;
        }
        if (onBottom && onRight)
        {
            return HtBottomRight;
        }
        if (onLeft)
        {
            return HtLeft;
        }
        if (onRight)
        {
            return HtRight;
        }
        if (onTop)
        {
            return HtTop;
        }
        if (onBottom)
        {
            return HtBottom;
        }

        return HtClient;
    }

    private void ToggleClickThrough()
    {
        SetClickThrough(!_isClickThrough);
    }

    private void SetClickThrough(bool enabled)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        var exStyle = GetWindowLongPtr(_windowHandle, GwlExStyle).ToInt64();
        exStyle |= WsExLayered;

        if (enabled)
        {
            exStyle |= WsExTransparent;
        }
        else
        {
            exStyle &= ~((long)WsExTransparent);
        }

        _ = SetWindowLongPtr(_windowHandle, GwlExStyle, new IntPtr(exStyle));
        _ = SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate);

        _isClickThrough = enabled;
        Messages.Add(new ChatMessage
        {
            Name = "system",
            Text = enabled ? "Click-through enabled." : "Click-through disabled.",
        });
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isClickThrough)
        {
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isClickThrough)
        {
            Messages.Add(new ChatMessage { Name = "system", Text = "Disable click-through before opening settings." });
            return;
        }

        IsSettingsOpen = !IsSettingsOpen;
    }

    private void SaveSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        _settings.Appearance.Opacity = OverlayOpacity;
        _settings.Appearance.FontSize = ChatFontSize;
        _settings.Appearance.TextColor = ChatTextColorHex;
        _settings.Overlay.FocusInputWithEnter = FocusInputWithEnterEnabled;
        UpdateFocusInputHotkeyRegistration();

        SettingsLoader.Save(_settingsPath, _settings);
        Messages.Add(new ChatMessage { Name = "system", Text = "Appearance settings saved." });
        IsSettingsOpen = false;
    }

    private void CloseSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        IsSettingsOpen = false;
    }

    private void EnterChatInputMode()
    {
        if (InputBox.IsKeyboardFocusWithin)
        {
            _ = Dispatcher.InvokeAsync(async () => await SendCurrentInputAsync());
            return;
        }

        if (_isClickThrough)
        {
            SetClickThrough(false);
        }

        Dispatcher.Invoke(() =>
        {
            Activate();
            IsSettingsOpen = false;
            InputBox.Focus();
            InputBox.CaretIndex = InputBox.Text.Length;
        });
    }

    private async Task SendCurrentInputAsync()
    {
        var text = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            await _chatClient.SendChatAsync(text, _cts.Token);
            InputBox.Clear();
        }
        catch
        {
            Messages.Add(new ChatMessage { Name = "system", Text = "Send failed." });
        }
    }

    private void UpdateFocusInputHotkeyRegistration()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (FocusInputWithEnterEnabled)
        {
            if (_focusInputHotkeyRegistered)
            {
                return;
            }

            _focusInputHotkeyRegistered = RegisterHotKey(_windowHandle, FocusInputHotKeyId, 0, VkReturn);
            if (!_focusInputHotkeyRegistered)
            {
                FocusInputWithEnterEnabled = false;
                Messages.Add(new ChatMessage { Name = "system", Text = "Global Enter hotkey registration failed." });
                return;
            }

            Messages.Add(new ChatMessage { Name = "system", Text = "Global Enter focus enabled." });
            return;
        }

        if (_focusInputHotkeyRegistered)
        {
            _ = UnregisterHotKey(_windowHandle, FocusInputHotKeyId);
            _focusInputHotkeyRegistered = false;
            Messages.Add(new ChatMessage { Name = "system", Text = "Global Enter focus disabled." });
        }
    }

    private void ApplyAppearanceSettings(AppearanceSettings appearance)
    {
        OverlayOpacity = appearance.Opacity;
        ChatFontSize = appearance.FontSize;
        UpdateOverlayBackgroundBrush();

        if (TryCreateColor(appearance.TextColor, out var color))
        {
            ChatColorR = color.R;
            ChatColorG = color.G;
            ChatColorB = color.B;
            return;
        }

        ChatColorR = 255;
        ChatColorG = 255;
        ChatColorB = 255;
    }

    private void UpdateOverlayBackgroundBrush()
    {
        var alpha = (byte)Math.Round(OverlayOpacity * 255);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));
        brush.Freeze();
        OverlayBackgroundBrush = brush;
    }

    private void UpdateChatBrushFromRgb()
    {
        var color = Color.FromRgb((byte)Math.Round(ChatColorR), (byte)Math.Round(ChatColorG), (byte)Math.Round(ChatColorB));
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        ChatTextBrush = brush;
        ChatTextColorHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static bool TryCreateColor(string colorText, out Color color)
    {
        color = Colors.White;
        if (string.IsNullOrWhiteSpace(colorText))
        {
            return false;
        }

        try
        {
            var converted = ColorConverter.ConvertFromString(colorText);
            if (converted is Color parsedColor)
            {
                color = Color.FromRgb(parsedColor.R, parsedColor.G, parsedColor.B);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool TryParseHotkey(string hotkeyText, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        if (string.IsNullOrWhiteSpace(hotkeyText))
        {
            return false;
        }

        var parts = hotkeyText.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "alt":
                    modifiers |= ModAlt;
                    break;
                case "ctrl":
                case "control":
                    modifiers |= ModControl;
                    break;
                case "shift":
                    modifiers |= ModShift;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModWin;
                    break;
                default:
                    return false;
            }
        }

        if (!Enum.TryParse<Key>(parts[^1], true, out var key))
        {
            return false;
        }

        var vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0)
        {
            return false;
        }

        virtualKey = (uint)vk;
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
