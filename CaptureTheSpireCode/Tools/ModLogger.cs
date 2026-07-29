using MegaCrit.Sts2.Core.Logging;

namespace CaptureTheSpire.CaptureTheSpireCode.Tools;

internal static class ModLogger
{
    private static readonly object syncRoot = new();
    private static readonly Logger mainLogger = new(MainFile.ModId, LogType.Generic);

    private static readonly string modDirectory =
        Path.GetDirectoryName(typeof(MainFile).Assembly.Location)
        ?? throw new InvalidOperationException("Could not determine the CaptureTheSpire mod directory.");

    private static readonly string logPath = Path.Combine(modDirectory, "CaptureTheSpire.log");

    internal static void Info(string message, bool logToMain = true)
    {
        Write("INFO", message);

        if (logToMain)
            mainLogger.Info(message);
    }

    internal static void Warning(string message, bool logToMain = true)
    {
        Write("WARNING", message);

        if (logToMain)
            mainLogger.Warn(message);
    }

    internal static void Error(string message)
    {
        Write("ERROR", message);
        mainLogger.Error(message);
    }

    internal static void Error(string message, Exception exception)
    {
        Error($"{message}: {exception}");
    }

    internal static void Clear()
    {
        lock (syncRoot)
            File.WriteAllText(logPath, string.Empty);
    }

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";

        lock (syncRoot)
            File.AppendAllText(logPath, line);
    }
}