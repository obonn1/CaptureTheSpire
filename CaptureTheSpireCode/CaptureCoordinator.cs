using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    private static async Task TryCaptureAsync()
    {
        if (isCapturing)
        {
            MainFile.Logger.Info("Capture already in progress.");
            return;
        }

        var mapScreen = NMapScreen.Instance;

        if (mapScreen is null)
        {
            MainFile.Logger.Info("F8 pressed, but the map screen is unavailable.");
            return;
        }

        isCapturing = true;

        try
        {
            MainFile.Logger.Info("F8 pressed. Starting full-map capture.");

            var image = await MapCapture.CaptureAsync();

            if (image is null)
            {
                MainFile.Logger.Error("Map capture did not produce an image.");
                return;
            }

            var outputPath = ImageExporter.ExportPng(image, "map_subviewport_full.png");

            MainFile.Logger.Info($"Saved full-map capture to {outputPath}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Capture failed: {ex}");
        }
        finally
        {
            isCapturing = false;
        }
    }
}
