using BaseLib.Config;

namespace CaptureTheSpire.CaptureTheSpireCode;

public sealed class CaptureTheSpireConfig : SimpleModConfig
{
    public static bool EnableCaptureHotkey
    {
        get => CaptureSettings.HotkeyEnabled;
        set => CaptureSettings.SetHotkeyEnabled(value);
    }

    public static bool ShowCaptureButton
    {
        get => CaptureSettings.ButtonEnabled;
        set => CaptureSettings.SetButtonEnabled(value);
    }
}