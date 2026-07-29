using CaptureTheSpire.CaptureTheSpireCode.Tools;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace CaptureTheSpire.CaptureTheSpireCode;

internal class CaptureCoordinator
{
    private static bool isCapturing;
    private static bool wasF8Pressed;

    public static void Initialize()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame += CheckHotkey;
    }

    private static void CheckHotkey()
    {
        var isF8Pressed = Input.IsKeyPressed(Key.F8);

        if (isF8Pressed && !wasF8Pressed)
            _ = TryCaptureAsync();

        wasF8Pressed = isF8Pressed;
    }

    internal static async Task TryCaptureAsync()
    {
        if (isCapturing)
        {
            ModLogger.Info("Capture already in progress.");
            return;
        }

        isCapturing = true;

        try
        {
            var capture = await CaptureCurrentScreenAsync();

            if (capture is null)
            {
                ModLogger.Error("Capture did not produce an image.");
                return;
            }

            var (image, captureName, fileName) = capture.Value;

            if (WindowsClipboard.TryCopy(image, out var clipboardError))
            {
                ModLogger.Info(
                    $"Copied {captureName} capture to clipboard. Godot sees image: {DisplayServer.ClipboardHasImage()}",
                    logToMain: false);
            }
            else
                ModLogger.Error($"Failed to copy to clipboard: {clipboardError}");

            var outputPath = ImageExporter.ExportPng(image, fileName);

            ModLogger.Info($"Saved {captureName} capture to {outputPath}.", logToMain: false);
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Capture failed: {ex}");
        }
        finally
        {
            isCapturing = false;
        }
    }

    private static async Task<(Image Image, string CaptureName, string FileName)?> CaptureCurrentScreenAsync()
    {
        if (FindVisibleNode<NDeckViewScreen>() is not null)
        {
            ModLogger.Info("Starting full-deck capture.", logToMain: false);

            var image = await DeckCapture.CaptureAsync();

            return image is null
                ? null
                : (image, "full-deck", "deck_subviewport_full.png");
        }

        if (NMapScreen.Instance?.IsVisibleInTree() == true)
        {
            ModLogger.Info("Starting full-map capture.", logToMain: false);

            var image = await MapCapture.CaptureAsync();

            return image is null
                ? null
                : (image, "full-map", "map_subviewport_full.png");
        }

        ModLogger.Info("Starting current-screen capture.", logToMain: false);

        var currentScreenImage = await CurrentScreenCapture.CaptureAsync();

        return currentScreenImage is null
            ? null
            : (currentScreenImage, "current-screen", "current_screen.png");
    }

    private static T? FindVisibleNode<T>() where T : CanvasItem
    {
        var tree = (SceneTree)Engine.GetMainLoop();

        return FindVisibleNode<T>(tree.Root);
    }

    private static T? FindVisibleNode<T>(Node node) where T : CanvasItem
    {
        if (node is T matchingNode && matchingNode.IsVisibleInTree())
            return matchingNode;

        foreach (var child in node.GetChildren())
        {
            var result = FindVisibleNode<T>(child);

            if (result is not null)
                return result;
        }

        return null;
    }
}