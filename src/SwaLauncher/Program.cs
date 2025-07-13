using System.Diagnostics;

namespace SwaLauncher;

internal static class Program
{
    private static void Main(string[] args)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/C swa start http://localhost:5233 --api-location http://localhost:7071/api",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false,
            WorkingDirectory = Directory.GetCurrentDirectory(),
        };

        using var process = new Process();
        process.StartInfo = processInfo;
        process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }
}