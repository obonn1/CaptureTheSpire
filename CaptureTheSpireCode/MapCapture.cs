using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;


namespace CaptureTheSpire.CaptureTheSpireCode;

internal class MapCapture
{
    public static async Task<Image?> CaptureAsync()
    {
        var viewport = (SubViewport?)null;

        try
        {
            var mapScreen = NMapScreen.Instance;
            if (mapScreen is null)
            {
                MainFile.Logger.Error("NMapScreen.Instance was null.");
                return null;
            }

            var globalUi = mapScreen.GetParent();
            if (globalUi is null)
            {
                MainFile.Logger.Error("Could not find GlobalUi.");
                return null;
            }

            var liveTopBar = globalUi.GetNodeOrNull<Control>("TopBar");
            if (liveTopBar is null)
            {
                MainFile.Logger.Error("Could not find GlobalUi/TopBar.");
                return null;
            }

            var liveMap = mapScreen.GetNodeOrNull<Control>("TheMap");
            if (liveMap is null)
            {
                MainFile.Logger.Error("Could not find TheMap.");
                return null;
            }

            var liveMapBg =liveMap.GetNodeOrNull<Control>("MapBg");
            if (liveMapBg is null)
            {
                MainFile.Logger.Error("Could not find TheMap/MapBg.");
                return null;
            }

            if (liveMap.Duplicate() is not Control duplicatedMap)
            {
                MainFile.Logger.Error("Could not duplicate TheMap.");
                return null;
            }


            if (liveTopBar.Duplicate() is not Control duplicatedTopBar)
            {
                MainFile.Logger.Error("Could not duplicate TopBar.");
                return null;
            }

            var mapBounds = new Rect2(liveMapBg.Position,liveMapBg.Size);

            var outputSize = CalculateOutputSize(mapBounds);

            LogCaptureDimensions(
                liveMap,
                liveMapBg,
                outputSize);

            viewport = CreateViewport(outputSize);

            var mapRoot = CreateMapRoot(
                liveMap,
                mapBounds);

            var topBarRoot = CreateTopBarRoot(liveTopBar);

            duplicatedMap.Position = Vector2.Zero;
            duplicatedTopBar.Position = Vector2.Zero;

            var tree = (SceneTree)Engine.GetMainLoop();

            tree.Root.AddChild(viewport);

            viewport.AddChild(mapRoot);
            mapRoot.AddChild(duplicatedMap);

            viewport.AddChild(topBarRoot);
            topBarRoot.AddChild(duplicatedTopBar);

            await WaitForRenderAsync(tree);

            return viewport
                .GetTexture()
                .GetImage();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"SubViewport capture failed: {ex}");

            return null;
        }
        finally
        {
            if (viewport is not null && GodotObject.IsInstanceValid(viewport))
                viewport.QueueFree();
        }
    }

    private static Vector2I CalculateOutputSize(
        Rect2 mapBounds)
    {
        return new Vector2I(
            Math.Max(1, (int)Math.Ceiling(mapBounds.Size.X)),
            Math.Max(1, (int)Math.Ceiling(mapBounds.Size.Y)));
    }

    private static SubViewport CreateViewport(Vector2I outputSize) => new SubViewport
    {
        Name = "CaptureTheSpireViewport",
        Size = outputSize,
        TransparentBg = true,
        Disable3D = true,
        RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
    };

    private static Control CreateMapRoot(Control liveMap, Rect2 mapBounds)
    {
        var root = new Control
        {
            Name = "FixedLayoutRoot",
            Position = -mapBounds.Position,
            Size = liveMap.Size,
            CustomMinimumSize = liveMap.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        root.SetAnchorsPreset(
            Control.LayoutPreset.TopLeft);

        return root;
    }

    private static Control CreateTopBarRoot(Control liveTopBar)
    {
        var root = new Control
        {
            Name = "TopBarRoot",
            Position = liveTopBar.Position,
            Size = liveTopBar.Size,
            CustomMinimumSize = liveTopBar.Size,
            MouseFilter =
                Control.MouseFilterEnum.Ignore,
        };

        root.SetAnchorsPreset(
            Control.LayoutPreset.TopLeft);

        return root;
    }

    private static async Task WaitForRenderAsync(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static void LogCaptureDimensions(
        Control liveMap,
        Control liveMapBg,
        Vector2I outputSize)
    {
        MainFile.Logger.Info($"Live TheMap position: {liveMap.Position}, " + $"size: {liveMap.Size}");

        MainFile.Logger.Info($"MapBg position: {liveMapBg.Position}, " + $"size: {liveMapBg.Size}");

        MainFile.Logger.Info($"Output size: {outputSize}");
    }
}
