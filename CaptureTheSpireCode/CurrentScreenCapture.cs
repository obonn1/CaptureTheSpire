using CaptureTheSpire.CaptureTheSpireCode.Tools;
using Godot;

namespace CaptureTheSpire.CaptureTheSpireCode;

internal static class CurrentScreenCapture
{
    public static async Task<Image?> CaptureAsync()
    {
        try
        {
            var tree = (SceneTree)Engine.GetMainLoop();

            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            var viewport = tree.Root.GetViewport();
            var texture = viewport.GetTexture();
            var image = texture.GetImage();

            if (image.IsEmpty())
            {
                ModLogger.Error("Current-screen capture produced an empty image.");
                return null;
            }

            return image;
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Current-screen capture failed: {ex}");
            return null;
        }
    }
}