using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureTheSpire.CaptureTheSpireCode.Tools;

internal static class SceneTreeLogger
{
    internal static void LogAncestorsAndSiblings(Node node)
    {
        var current = node;

        while (current is not null)
        {
            MainFile.Logger.Info($"Ancestor: {current.Name} [{current.GetType().FullName}]");

            foreach (var child in current.GetChildren())
            {
                if (child is Control control)
                {
                    MainFile.Logger.Info($"  Child: {control.Name} [{control.GetType().FullName}] position={control.Position}, size={control.Size}, visible={control.Visible}");
                }
                else
                {
                    MainFile.Logger.Info($"  Child: {child.Name} [{child.GetType().FullName}]");
                }
            }

            current = current.GetParent();
        }
    }
}
