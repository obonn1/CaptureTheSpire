using CaptureTheSpire.CaptureTheSpireCode.Tools;
using Godot;
using System.Reflection;

namespace CaptureTheSpire.CaptureTheSpireCode;

internal static class ModConfigBridge
{
    private const string ModId = "CaptureTheSpire";
    private const string DisplayName = "Capture the Spire";

    private static bool isAvailable;
    private static bool isRegistered;
    private static Type? apiType;
    private static Type? entryType;
    private static Type? configTypeEnum;

    internal static bool IsAvailable => isAvailable;

    internal static void DeferredRegister()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame += OnNextFrame;
    }

    private static void OnNextFrame()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame -= OnNextFrame;

        Detect();

        if (isAvailable)
            Register();
    }

    private static void Detect()
    {
        try
        {
            var allTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .ToArray();

            apiType = allTypes.FirstOrDefault(type => type.FullName == "ModConfig.ModConfigApi");
            entryType = allTypes.FirstOrDefault(type => type.FullName == "ModConfig.ConfigEntry");
            configTypeEnum = allTypes.FirstOrDefault(type => type.FullName == "ModConfig.ConfigType");

            isAvailable = apiType is not null
                && entryType is not null
                && configTypeEnum is not null;
        }
        catch
        {
            isAvailable = false;
        }
    }

    private static void Register()
    {
        if (isRegistered)
            return;

        isRegistered = true;

        try
        {
            var entries = BuildEntries();

            var displayNames = new Dictionary<string, string>
            {
                ["en"] = DisplayName,
            };

            var registerMethod = apiType!
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "Register")
                .OrderByDescending(method => method.GetParameters().Length)
                .First();

            if (registerMethod.GetParameters().Length == 4)
            {
                registerMethod.Invoke(
                    null,
                    new object[]
                    {
                        ModId,
                        DisplayName,
                        displayNames,
                        entries,
                    });
            }
            else
            {
                registerMethod.Invoke(
                    null,
                    new object[]
                    {
                        ModId,
                        DisplayName,
                        entries,
                    });
            }

            CaptureSettings.Load();
        }
        catch (Exception ex)
        {
            ModLogger.Error($"ModConfig registration failed: {ex}");
        }
    }

    internal static T GetValue<T>(string key, T fallback)
    {
        if (!isAvailable)
            return fallback;

        try
        {
            var getValueMethod = apiType!
                .GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static)?
                .MakeGenericMethod(typeof(T));

            var result = getValueMethod?.Invoke(null, new object[] { ModId, key });

            return result is T value
                ? value
                : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    internal static void SetValue(string key, object value)
    {
        if (!isAvailable)
            return;

        try
        {
            apiType!
                .GetMethod("SetValue", BindingFlags.Public | BindingFlags.Static)?
                .Invoke(null, new[] { ModId, key, value });
        }
        catch
        {
            // ModConfig is optional. Failure to persist a setting should not
            // prevent CaptureTheSpire from working.
        }
    }

    private static Array BuildEntries()
    {
        var entries = new List<object>
        {
            Entry(config =>
            {
                Set(config, "Label", "Capture Controls");
                Set(config, "Type", EnumValue("Header"));
            }),

            Entry(config =>
            {
                Set(config, "Key", "hotkeyEnabled");
                Set(config, "Label", "Enable Capture Hotkey");
                Set(config, "Type", EnumValue("Toggle"));
                Set(config, "DefaultValue", CaptureSettings.DefaultHotkeyEnabled);
                Set(config, "Description", "Enable capturing with the configured keyboard shortcut.");

                Set(config, "OnChanged", new Action<object>(value =>
                {
                    CaptureSettings.HotkeyEnabled = Convert.ToBoolean(value);
                }));
            }),

            Entry(config =>
            {
                Set(config, "Key", "captureKey");
                Set(config, "Label", "Capture Hotkey");
                Set(config, "Type", EnumValue("KeyBind"));
                Set(config, "DefaultValue", (long)CaptureSettings.DefaultCaptureKey);
                Set(config, "Description", "The keyboard shortcut used to capture the current screen.");

                Set(config, "OnChanged", new Action<object>(value =>
                {
                    CaptureSettings.CaptureKey = (Key)Convert.ToInt64(value);
                }));
            }),

            Entry(config =>
            {
                Set(config, "Key", "buttonEnabled");
                Set(config, "Label", "Show Capture Button");
                Set(config, "Type", EnumValue("Toggle"));
                Set(config, "DefaultValue", CaptureSettings.DefaultButtonEnabled);
                Set(config, "Description", "Show the capture button beside the map button.");

                Set(config, "OnChanged", new Action<object>(value =>
                {
                    CaptureSettings.ButtonEnabled = Convert.ToBoolean(value);
                    CaptureButton.SetEnabled(CaptureSettings.ButtonEnabled);
                }));
            }),
        };

        var result = Array.CreateInstance(entryType!, entries.Count);

        for (var index = 0; index < entries.Count; index++)
            result.SetValue(entries[index], index);

        return result;
    }

    private static object Entry(Action<object> configure)
    {
        var instance = Activator.CreateInstance(entryType!)
            ?? throw new InvalidOperationException("Could not create a ModConfig entry.");

        configure(instance);

        return instance;
    }

    private static void Set(object instance, string propertyName, object value)
    {
        var property = instance
            .GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        if (property is null)
            throw new MissingMemberException(instance.GetType().FullName, propertyName);

        property.SetValue(instance, value);
    }

    private static object EnumValue(string name)
    {
        return Enum.Parse(configTypeEnum!, name);
    }
}