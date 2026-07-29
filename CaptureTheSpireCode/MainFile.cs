using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using CaptureTheSpire.CaptureTheSpireCode.Tools;
using BaseLib.Config;

namespace CaptureTheSpire.CaptureTheSpireCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "CaptureTheSpire";

    public static void Initialize()
    {
        ModLogger.Info("CaptureTheSpire initialized.");

        var harmony = new Harmony(ModId);
        harmony.PatchAll();

        CaptureCoordinator.Initialize();
        CaptureButton.Initialize();
        ModConfigRegistry.Register("CaptureTheSpire", new CaptureTheSpireConfig());
        ModConfigBridge.DeferredRegister();
    }
}