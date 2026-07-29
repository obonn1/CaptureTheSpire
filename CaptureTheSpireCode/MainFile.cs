using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using CaptureTheSpire.CaptureTheSpireCode.Tools;

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
    }
}