using System;
using System.IO;

public static class Logger
{
    static readonly string logFilePath = "latest.log";
    static readonly string prefix = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] ";

    static Logger()
    {
        if (File.Exists(logFilePath))
            File.Delete(logFilePath);
    }

    public static void LogInfo(string message)
    {
        Console.WriteLine($"{prefix}[INFO] " + message);
        File.AppendAllText(logFilePath, "[INFO] " + message + Environment.NewLine);
    }

    public static void LogWarning(string message)
    {
        Console.WriteLine($"{prefix}[WARN] " + message);
        File.AppendAllText(logFilePath, "[WARN] " + message + Environment.NewLine);
    }

    public static void LogError(string message)
    {
        Console.WriteLine($"{prefix}[ERROR] " + message);
        File.AppendAllText(logFilePath, "[ERROR] " + message + Environment.NewLine);
    }
}