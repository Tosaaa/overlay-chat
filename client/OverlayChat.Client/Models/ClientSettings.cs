namespace OverlayChat.Client.Models;

public sealed class ClientSettings
{
    public ConnectionSettings Connection { get; set; } = new();
    public OverlaySettings Overlay { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
}

public sealed class ConnectionSettings
{
    public string ServerUrl { get; set; } = "ws://127.0.0.1:8080/ws";
    public string Name { get; set; } = "player";
    public string Room { get; set; } = "default";
    public string RoomKey { get; set; } = string.Empty;
}

public sealed class OverlaySettings
{
    public string ToggleHotkey { get; set; } = "Ctrl+Shift+O";
    public bool StartClickThrough { get; set; }
    public bool FocusInputWithEnter { get; set; }
}

public sealed class AppearanceSettings
{
    public double Opacity { get; set; } = 0.9;
    public double FontSize { get; set; } = 14;
    public string TextColor { get; set; } = "#FFFFFFFF";
}
