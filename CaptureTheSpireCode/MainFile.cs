using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace CaptureTheSpire.CaptureTheSpireCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "CaptureTheSpire";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("CaptureTheSpire initialized.");

        var harmony = new Harmony(ModId);
        harmony.PatchAll();

        CaptureCoordinator.Initialize();
    }
}