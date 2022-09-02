using System.Diagnostics;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Releaser;

public static class Program
{
    private const string AppName = "Webview2Desktop";
    private const string BuildFolder = "../../../../build";
    private const string FrontendAppSuffix = ".Client";
    private const string FrontendReleaseAppSuffix = ".client";
    private const string BackendFolder = "../../../../" + AppName;
    private const string BackendReleaseFolder = BackendFolder + "/bin/Release/net6.0-windows";
    private const string FrontendFolder = "../../../../" + AppName + FrontendAppSuffix;
    private const string FrontendReleaseFolder = FrontendFolder + "/dist";
    private const string BackendBuildFolder = BuildFolder;
    private const string FrontendBuildFilePath = BuildFolder + "/" + AppName + FrontendReleaseAppSuffix;
    private const string AppIdToken = "#{APP_ID}";
    private const string WebViewBootstrapperFilePath = BackendFolder + "/WebView/" + "WebViewBootstrapper.cs";
    private static string AppId = string.Empty;

    public static int Main()
    {
        if (!GenerateAppId())
        {
            Console.WriteLine("Must contains at least a lower case, an upper case, a number and a symbol.");
            return 5;
        }

        if (!BuildBackend()) return 1;
        if (!BuildFrontend()) return 2;
        if (!PackageBackend()) return 3;
        if (!PackageFrontend()) return 4;

        return 0;
    }

    private static bool GenerateAppId()
    {
        Console.Write("Enter at least 8 random characters, with at least a lower case, an upper case, a number and a symbol: ");
        var userId = Console.ReadLine();
        if (
            string.IsNullOrEmpty(userId) ||
            userId.Length < 8 ||
            !Regex.IsMatch(userId, ".*[a-z]+.*") ||
            !Regex.IsMatch(userId, ".*[A-Z]+.*") ||
            !Regex.IsMatch(userId, ".*\\d+.*") ||
            !Regex.IsMatch(userId, ".*\\W+.*")
        ) return false;

        var guid = Guid.NewGuid().ToString();
        var random = new Random();
        AppId = string.Join(string.Empty,
            (guid + userId)
            .ToArray()
            .OrderBy(c => random.Next())
        );

        return true;
    }

    private static bool PackageBackend()
    {
        Console.WriteLine();
        Console.WriteLine("### PACKAGING BACKEND ###");

        if (Directory.Exists(BuildFolder)) Directory.Delete(BuildFolder, true);
        Directory.CreateDirectory(BuildFolder);

        var files = Directory.EnumerateFiles(BackendReleaseFolder);
        foreach (var file in files)
        {
            if (!file.EndsWith(".dll") && !file.EndsWith(".exe") && !file.EndsWith("deps.json") && !file.EndsWith("runtimeconfig.json")) continue;
            var fileName = Path.GetFileName(file);
            File.Copy(file, $"{BackendBuildFolder}/{fileName}");
        }

        var dirs = Directory.EnumerateDirectories(BackendReleaseFolder);
        foreach (var dir in dirs)
        {
            var parts = dir.Split(@"\");
            DirectoryHelper.CopyDirectory(dir, $"{BackendBuildFolder}/{parts.Last()}");
        }

        return true;
    }


    private static bool PackageFrontend()
    {
        Console.WriteLine();
        Console.WriteLine("### PACKAGING FRONTEND ###");

        var zip = new FastZip
        {
            Password = AppId,
            CompressionLevel = Deflater.CompressionLevel.BEST_COMPRESSION
        };
        zip.CreateZip(FrontendBuildFilePath, FrontendReleaseFolder, true, "");

        return true;
    }

    private static bool BuildFrontend()
    {
        Console.WriteLine();
        Console.WriteLine("### BUILDING FRONTEND ###");
        return RunCommand($"cd {FrontendFolder} && ng build");
    }

    private static bool BuildBackend()
    {
        Console.WriteLine();
        Console.WriteLine("### BUILDING BACKEND ###");

        var data = string.Empty;
        var ok = false;

        try
        {
            data = File.ReadAllText(WebViewBootstrapperFilePath);
            data = data.Replace(AppIdToken, AppId);
            File.WriteAllText(WebViewBootstrapperFilePath, data);

            ok = RunCommand($"dotnet build {BackendFolder} --configuration=Release");
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            data = File.ReadAllText(WebViewBootstrapperFilePath);
            data = data.Replace(AppId, AppIdToken);
            File.WriteAllText(WebViewBootstrapperFilePath, data);
        }

        return ok;
    }

    private static bool RunCommand(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo("cmd")
            {
                Arguments = $"/c \"{command}\""
            }
        };
        process.Start();
        process.WaitForExit();
        return process.ExitCode == 0;
    }
}