using Godot;
using System.Reflection;

namespace CaptureTheSpire.CaptureTheSpireCode;

internal class ImageExporter
{
    public static string ExportPng(Image image, string fileName)
    {
        var exportDirectory = GetExportDirectory();

        Directory.CreateDirectory(exportDirectory);

        var outputPath = Path.Combine(exportDirectory, fileName);

        var error = image.SavePng(outputPath);

        if (error is not Error.Ok)
            throw new IOException($"Could not save PNG to {outputPath}. " + $"Godot error: {error}");

        return outputPath;
    }

    private static string GetExportDirectory()
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (string.IsNullOrWhiteSpace(assemblyDirectory))
            throw new InvalidOperationException("Could not determine the mod assembly directory.");

        return Path.Combine(assemblyDirectory, "exports");
    }
}
