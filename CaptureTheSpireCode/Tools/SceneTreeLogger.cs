using Godot;

namespace CaptureTheSpire.CaptureTheSpireCode.Tools;

internal static class SceneTreeLogger
{
    internal static void LogAncestorsAndSiblings(Node node)
    {
        var current = node;

        while (current is not null)
        {
            ModLogger.Info($"Ancestor: {current.Name} [{current.GetType().FullName}]", logToMain: false);

            foreach (var child in current.GetChildren())
            {
                if (child is Control control)
                {
                    ModLogger.Info($"  Child: {control.Name} [{control.GetType().FullName}] position={control.Position}, size={control.Size}, visible={control.Visible}", logToMain: false);
                }
                else
                {
                    ModLogger.Info($"  Child: {child.Name} [{child.GetType().FullName}]", logToMain: false);
                }
            }

            current = current.GetParent();
        }
    }
}
