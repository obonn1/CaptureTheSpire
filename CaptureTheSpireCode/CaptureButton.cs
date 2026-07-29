using CaptureTheSpire.CaptureTheSpireCode.Tools;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace CaptureTheSpire.CaptureTheSpireCode;

internal static class CaptureButton
{
    private const string ButtonArtPath = "res://CaptureTheSpire/button_art.png";
    private const float ButtonSize = 75f;
    private const float RightOffset = 530f;
    private const float TopOffset = 2f;

    private static TextureButton? button;
    private static bool failedToLoadTexture;

    public static void Initialize()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame += TryInstall;
    }

    private static void TryInstall()
    {
        if (button is not null && GodotObject.IsInstanceValid(button))
            return;

        if (failedToLoadTexture)
            return;

        var globalUi = FindNode<NGlobalUi>();

        if (globalUi?.TopBar is not Control topBar || !topBar.IsInsideTree())
            return;

        var texture = GD.Load<Texture2D>(ButtonArtPath);

        if (texture is null)
        {
            failedToLoadTexture = true;
            ModLogger.Error($"Could not load capture button art from {ButtonArtPath}.");
            return;
        }

        button = CreateButton(texture);
        topBar.AddChild(button);

        ModLogger.Info("Added capture button to the top bar.", logToMain: false);
    }

    private static TextureButton CreateButton(Texture2D texture)
    {
        var captureButton = new TextureButton
        {
            Name = "CaptureTheSpireButton",
            TextureNormal = texture,
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(ButtonSize, ButtonSize),
            Size = new Vector2(ButtonSize, ButtonSize),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            TooltipText = "Capture current screen",
            FocusMode = Control.FocusModeEnum.None,
        };

        captureButton.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        captureButton.Position = new Vector2(-RightOffset, TopOffset);
        captureButton.Pressed += OnPressed;

        return captureButton;
    }

    private static async void OnPressed()
    {
        try
        {
            await CaptureCoordinator.TryCaptureAsync();
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Capture button failed: {ex}");
        }
    }

    private static T? FindNode<T>() where T : Node
    {
        var tree = (SceneTree)Engine.GetMainLoop();

        return FindNode<T>(tree.Root);
    }

    private static T? FindNode<T>(Node node) where T : Node
    {
        if (node is T matchingNode)
            return matchingNode;

        foreach (var child in node.GetChildren())
        {
            var result = FindNode<T>(child);

            if (result is not null)
                return result;
        }

        return null;
    }
}