using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using System.Reflection;

namespace CaptureTheSpire.CaptureTheSpireCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "CaptureTheSpire";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    private static bool isCapturing;
    private static bool wasF8Pressed;

    public static void Initialize()
    {
        Logger.Info("CaptureTheSpire initialized.");

        var harmony = new Harmony(ModId);
        harmony.PatchAll();

        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame += CheckHotkey;

        Logger.Info("Hotkey polling enabled. Press F8 while the map is open.");
    }

    private static void CheckHotkey()
    {
        var isF8Pressed = Input.IsKeyPressed(Key.F8);

        if (isF8Pressed && !wasF8Pressed)
            TryStartCapture();

        wasF8Pressed = isF8Pressed;
    }

    private static void TryStartCapture()
    {
        if (isCapturing)
        {
            Logger.Info("Capture already in progress.");
            return;
        }

        var mapScreen = NMapScreen.Instance;

        if (mapScreen is null)
        {
            Logger.Info("F8 pressed, but the map is null.");
            return;
        }

        Logger.Info("F8 pressed. Starting full-map SubViewport capture.");
        _ = CaptureSubViewportAsync();
        
    }

    private static async Task CaptureSubViewportAsync()
    {
        isCapturing = true;

        SubViewport? viewport = null;

        try
        {
            var mapScreen = NMapScreen.Instance;
            if (mapScreen is null)
            {
                Logger.Error("NMapScreen.Instance was null.");
                return;
            }

            var globalUi = mapScreen.GetParent();
            if (globalUi is null)
            {
                Logger.Error("Could not find GlobalUi.");
                return;
            }

            var liveTopBar = globalUi.GetNodeOrNull<Control>("TopBar");
            if (liveTopBar is null)
            {
                Logger.Error("Could not find GlobalUi/TopBar.");
                return;
            }


            var liveMap = mapScreen.GetNodeOrNull<Control>("TheMap");
            if (liveMap is null)
            {
                Logger.Error("Could not find TheMap.");
                return;
            }

            var liveMapBg = liveMap.GetNodeOrNull<Control>("MapBg");
            if (liveMapBg is null)
            {
                Logger.Error("Could not find TheMap/MapBg.");
                return;
            }

            var duplicatedMap = liveMap.Duplicate() as Control;
            if (duplicatedMap is null)
            {
                Logger.Error("Could not duplicate TheMap.");
                return;
            }

            var duplicatedTopBar = liveTopBar.Duplicate() as Control;
            if (duplicatedTopBar is null)
            {
                Logger.Error("Could not duplicate TopBar.");
                return;
            }

            var mapBounds = new Rect2(
                liveMapBg.Position,
                liveMapBg.Size);

            var outputSize = new Vector2I(
                Math.Max(
                    1,
                    (int)Math.Ceiling(mapBounds.Size.X)),
                Math.Max(
                    1,
                    (int)Math.Ceiling(mapBounds.Size.Y)));

            Logger.Info(
                $"Live TheMap position: {liveMap.Position}, size: {liveMap.Size}");

            Logger.Info(
                $"MapBg position: {liveMapBg.Position}, size: {liveMapBg.Size}");

            Logger.Info(
                $"Output size: {outputSize}");

            viewport = new SubViewport
            {
                Name = "CaptureTheSpireViewport",
                Size = outputSize,
                TransparentBg = true,
                Disable3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };

            /*
             * Keep the duplicated map in a 1920x1080-style layout context,
             * matching its live parent size. This prevents its anchored
             * children from responding directly to the tall SubViewport.
             */
            var layoutRoot = new Control
            {
                Name = "FixedLayoutRoot",
                Position = -mapBounds.Position,
                Size = liveMap.Size,
                CustomMinimumSize = liveMap.Size,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            layoutRoot.SetAnchorsPreset(Control.LayoutPreset.TopLeft);

            var topBarRoot = new Control
            {
                Name = "TopBarRoot",
                Position = liveTopBar.Position,
                Size = liveTopBar.Size,
                CustomMinimumSize = liveTopBar.Size,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            layoutRoot.SetAnchorsPreset(Control.LayoutPreset.TopLeft);

            duplicatedMap.Position = Vector2.Zero;

            var tree = (SceneTree)Engine.GetMainLoop();

            tree.Root.AddChild(viewport);
            viewport.AddChild(layoutRoot);
            layoutRoot.AddChild(duplicatedMap);

            viewport.AddChild(topBarRoot);
            topBarRoot.AddChild(duplicatedTopBar);
            duplicatedTopBar.Position = Vector2.Zero;

            await tree.ToSignal(
                tree,
                SceneTree.SignalName.ProcessFrame);

            await tree.ToSignal(
                tree,
                SceneTree.SignalName.ProcessFrame);

            await tree.ToSignal(
                tree,
                SceneTree.SignalName.ProcessFrame);

            var image = viewport
                .GetTexture()
                .GetImage();

            var exportDir = Path.Combine(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location)!,
                "exports");

            Directory.CreateDirectory(exportDir);

            var outputPath = Path.Combine(
                exportDir,
                "map_subviewport_full.png");

            var error = image.SavePng(outputPath);

            Logger.Info(
                $"Saved full-map capture to {outputPath}. Save result: {error}");
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"SubViewport capture failed: {ex}");
        }
        finally
        {
            if (viewport is not null &&
                GodotObject.IsInstanceValid(viewport))
            {
                viewport.QueueFree();
            }

            isCapturing = false;
        }
    }

    // Temporary exploratory tool
    private static void LogAncestorsAndSiblings(Node node)
    {
        var current = node;

        while (current is not null)
        {
            Logger.Info(
                $"Ancestor: {current.Name} [{current.GetType().FullName}]");

            foreach (var child in current.GetChildren())
            {
                if (child is Control control)
                {
                    Logger.Info(
                        $"  Child: {control.Name} " +
                        $"[{control.GetType().FullName}] " +
                        $"position={control.Position}, " +
                        $"size={control.Size}, " +
                        $"visible={control.Visible}");
                }
                else
                {
                    Logger.Info(
                        $"  Child: {child.Name} [{child.GetType().FullName}]");
                }
            }

            current = current.GetParent();
        }
    }
}