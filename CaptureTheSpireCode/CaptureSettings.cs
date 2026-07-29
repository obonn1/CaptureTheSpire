using Godot;

namespace CaptureTheSpire.CaptureTheSpireCode;

internal static class CaptureSettings
{
    internal const bool DefaultHotkeyEnabled = true;
    internal const bool DefaultButtonEnabled = true;
    internal const Key DefaultCaptureKey = Key.F8;

    private const string HotkeyEnabledKey = "hotkeyEnabled";
    private const string CaptureKeyKey = "captureKey";
    private const string ButtonEnabledKey = "buttonEnabled";

    internal static bool HotkeyEnabled { get; set; } = DefaultHotkeyEnabled;
    internal static Key CaptureKey { get; set; } = DefaultCaptureKey;
    internal static bool ButtonEnabled { get; set; } = DefaultButtonEnabled;

    internal static void Load()
    {
        HotkeyEnabled = ModConfigBridge.GetValue(
            HotkeyEnabledKey,
            DefaultHotkeyEnabled);

        CaptureKey = (Key)ModConfigBridge.GetValue(
            CaptureKeyKey,
            (long)DefaultCaptureKey);

        ButtonEnabled = ModConfigBridge.GetValue(
            ButtonEnabledKey,
            DefaultButtonEnabled);

        CaptureButton.SetEnabled(ButtonEnabled);
    }

    internal static void SetHotkeyEnabled(bool enabled, bool persistToModConfig = true)
    {
        HotkeyEnabled = enabled;

        if (persistToModConfig)
            ModConfigBridge.SetValue(HotkeyEnabledKey, enabled);
    }

    internal static void SetCaptureKey(Key key, bool persistToModConfig = true)
    {
        CaptureKey = key;

        if (persistToModConfig)
            ModConfigBridge.SetValue(CaptureKeyKey, (long)key);
    }

    internal static void SetButtonEnabled(bool enabled, bool persistToModConfig = true)
    {
        ButtonEnabled = enabled;
        CaptureButton.SetEnabled(enabled);

        if (persistToModConfig)
            ModConfigBridge.SetValue(ButtonEnabledKey, enabled);
    }
}