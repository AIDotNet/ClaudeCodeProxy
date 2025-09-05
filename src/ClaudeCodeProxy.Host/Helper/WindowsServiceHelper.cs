using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ClaudeCodeProxy.Host.Helper;

public static class WindowsServiceHelper
{
    private const string ServiceName = "ClaudeCodeProxyService";
    private const string ServiceDisplayName = "Claude Code Proxy Service";
    private const string ServiceDescription = "Claude Code Proxy API服务，用于代理Claude AI的API请求";

    /// <summary>
    ///     检查是否为Windows系统
    /// </summary>
    public static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    /// <summary>
    ///     检查是否以管理员权限运行
    /// </summary>
    public static bool IsRunningAsAdministrator()
    {
        if (!IsWindows())
            return false;

        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     安装Windows服务
    /// </summary>
    public static async Task<bool> InstallServiceAsync()
    {
        if (!IsWindows())
        {
            Console.WriteLine("当前系统不是Windows，无需安装服务。");
            return false;
        }

        if (!IsRunningAsAdministrator())
        {
            Console.WriteLine("需要管理员权限才能安装服务。请以管理员身份运行此程序。");
            return false;
        }

        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(executablePath))
            {
                Console.WriteLine("无法获取可执行文件路径。");
                return false;
            }

            // 检查服务是否已存在
            if (await IsServiceInstalledAsync())
            {
                Console.WriteLine($"服务 '{ServiceName}' 已存在。");
                return true;
            }

            // 使用sc命令安装服务
            var arguments =
                $"create \"{ServiceName}\" binpath= \"{executablePath}\" displayname= \"{ServiceDisplayName}\" start= auto";
            var result = await RunScCommandAsync(arguments);

            if (result.Success)
            {
                // 设置服务描述
                await RunScCommandAsync($"description \"{ServiceName}\" \"{ServiceDescription}\"");
                Console.WriteLine($"服务 '{ServiceDisplayName}' 安装成功！");
                Console.WriteLine("服务将在系统启动时自动启动。");
                return true;
            }

            Console.WriteLine($"服务安装失败：{result.Error}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"安装服务时发生错误：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     卸载Windows服务
    /// </summary>
    public static async Task<bool> UninstallServiceAsync()
    {
        if (!IsWindows())
        {
            Console.WriteLine("当前系统不是Windows，无需卸载服务。");
            return false;
        }

        if (!IsRunningAsAdministrator())
        {
            Console.WriteLine("需要管理员权限才能卸载服务。请以管理员身份运行此程序。");
            return false;
        }

        try
        {
            // 检查服务是否存在
            if (!await IsServiceInstalledAsync())
            {
                Console.WriteLine($"服务 '{ServiceName}' 不存在。");
                return true;
            }

            // 停止服务
            await StopServiceAsync();

            // 删除服务
            var result = await RunScCommandAsync($"delete \"{ServiceName}\"");

            if (result.Success)
            {
                Console.WriteLine($"服务 '{ServiceDisplayName}' 卸载成功！");
                return true;
            }

            Console.WriteLine($"服务卸载失败：{result.Error}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"卸载服务时发生错误：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     启动Windows服务
    /// </summary>
    public static async Task<bool> StartServiceAsync()
    {
        if (!IsWindows() || !await IsServiceInstalledAsync())
            return false;

        var result = await RunScCommandAsync($"start \"{ServiceName}\"");
        if (result.Success)
        {
            Console.WriteLine($"服务 '{ServiceDisplayName}' 启动成功！");
            return true;
        }

        Console.WriteLine($"服务启动失败：{result.Error}");
        return false;
    }

    /// <summary>
    ///     停止Windows服务
    /// </summary>
    public static async Task<bool> StopServiceAsync()
    {
        if (!IsWindows() || !await IsServiceInstalledAsync())
            return false;

        var result = await RunScCommandAsync($"stop \"{ServiceName}\"");
        if (result.Success)
        {
            Console.WriteLine($"服务 '{ServiceDisplayName}' 停止成功！");
            return true;
        }

        Console.WriteLine($"服务停止失败：{result.Error}");
        return false;
    }

    /// <summary>
    ///     检查服务是否已安装
    /// </summary>
    public static async Task<bool> IsServiceInstalledAsync()
    {
        if (!IsWindows())
            return false;

        var result = await RunScCommandAsync($"query \"{ServiceName}\"");
        return result.Success;
    }

    /// <summary>
    ///     运行sc命令
    /// </summary>
    private static async Task<(bool Success, string Output, string Error)> RunScCommandAsync(string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "sc";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return (process.ExitCode == 0, output, error);
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

    /// <summary>
    ///     显示服务状态
    /// </summary>
    public static async Task ShowServiceStatusAsync()
    {
        if (!IsWindows())
        {
            Console.WriteLine("当前系统不是Windows系统。");
            return;
        }

        Console.WriteLine($"服务名称: {ServiceName}");
        Console.WriteLine($"显示名称: {ServiceDisplayName}");

        var isInstalled = await IsServiceInstalledAsync();
        Console.WriteLine($"安装状态: {(isInstalled ? "已安装" : "未安装")}");

        if (isInstalled)
        {
            var result = await RunScCommandAsync($"query \"{ServiceName}\"");
            if (result.Success)
            {
                Console.WriteLine("服务详细信息:");
                Console.WriteLine(result.Output);
            }
        }
    }
}