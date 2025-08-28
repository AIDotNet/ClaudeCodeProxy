using System.Text;
using System.Text.Json;
using ClaudeCodeProxy.Abstraction;

namespace ClaudeCodeProxy.Host.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// 设置响应为 text/event-stream 相关的头
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static void SetEventStreamHeaders(this HttpContext context)
    {
        context.Response.ContentType = "text/event-stream;charset=utf-8;";
        context.Response.Headers.TryAdd("Cache-Control", "no-cache");
        context.Response.Headers.TryAdd("Connection", "keep-alive");
    }

    public static string GetContentType(string extension)
    {
        return extension switch
        {
            ".html" => "text/html",
            ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".ogg" => "video/ogg",
            ".mp3" => "audio/mp3",
            ".wav" => "audio/wav",
            ".webp" => "image/webp",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "font/eot",
            ".otf" => "font/otf",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".7z" => "application/x-7z-compressed",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".xml" => "text/xml",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }
    //
    // /// <summary>
    // /// 往响应内容写入事件流数据,调用前需要先调用 <see cref="SetEventStreamHeaders"/>
    // /// </summary>
    // /// <param name="context"></param>
    // /// <param name="eventName"></param>
    // /// <param name="value"></param>
    // /// <returns></returns>
    // public static async ValueTask WriteAsEventStreamDataAsync(this HttpContext context,
    //     string eventName, string value)
    // {
    //     await context.WriteAsEventAsync($"{eventName}{value}\n\n");
    //     await context.Response.Body.FlushAsync();
    // }

    /// <summary>
    /// 往响应内容写入事件流数据,调用前需要先调用 <see cref="SetEventStreamHeaders"/>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="event"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static async ValueTask WriteAsEventStreamDataAsync(this HttpContext context, string @event, object value)
    {
        string jsonData;
        if (value is string stringValue)
        {
            jsonData = stringValue;
        }
        else
        {
            jsonData = JsonSerializer.Serialize(value, ThorJsonSerializer.DefaultOptions);
        }

        var eventData = $"event: {@event}\ndata: {jsonData}\n\n";

        await context.WriteAsEventAsync(eventData);
        await context.Response.Body.FlushAsync();
    }

    public static async ValueTask WriteAsEventAsync(this HttpContext context, string value)
    {
        await context.Response.WriteAsync(value, Encoding.UTF8);
        await context.Response.Body.FlushAsync();
    }

    public static async ValueTask WriteAsEventStreamAsync(this HttpContext context, string @event)
    {
        var eventData = $"{@event}\n";
        await context.Response.WriteAsync(eventData, Encoding.UTF8);
        await context.Response.Body.FlushAsync();
    }

    /// <summary>
    /// 往响应内容写入事件流结束数据
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async ValueTask WriteAsEventStreamEndAsync(this HttpContext context)
    {
        var endData = "data: [DONE]\n\n";

        await context.Response.WriteAsync(endData);
        await context.Response.Body.FlushAsync();
    }

    /// <summary>
    /// 获取IP地址
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetIpAddress(this HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        // 获取具体IP地址，不包括:ffff:，可能是IPv6
        if (address?.IsIPv4MappedToIPv6 == true)
        {
            address = address.MapToIPv4();
        }
        else if (address?.IsIPv6SiteLocal == true)
        {
            address = address.MapToIPv4();
        }
        else if (address?.IsIPv6Teredo == true)
        {
            address = address.MapToIPv4();
        }
        else if (address?.IsIPv6Multicast == true)
        {
            address = address.MapToIPv6();
        }
        else if (address?.IsIPv6UniqueLocal == true)
        {
            address = address.MapToIPv6();
        }
        else if (address?.IsIPv6LinkLocal == true)
        {
            address = address.MapToIPv6();
        }
        else
        {
            address = address?.MapToIPv4();
        }

        var ip = address?.ToString();

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var ips) && !string.IsNullOrWhiteSpace(ips))
        {
            ip = ips.ToString();
        }

        return ip;
    }

    /// <summary>
    /// 获取userAgent
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetUserAgent(this HttpContext context)
    {
        // 获取UserAgent，提取有用信息
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();

        // 提取有用信息
        if (userAgent != null)
        {
            var index = userAgent.IndexOf('(');
            if (index > 0)
            {
                userAgent = userAgent[..index];
            }
            else
            {
                userAgent = userAgent switch
                {
                    not null when userAgent.Contains("Windows") => "Windows",
                    not null when userAgent.Contains("Mac") => "Mac",
                    not null when userAgent.Contains("Linux") => "Linux",
                    not null when userAgent.Contains("Android") => "Android",
                    not null when userAgent.Contains("iPhone") => "iPhone",
                    not null when userAgent.Contains("iPad") => "iPad",
                    _ => "未知"
                };
            }
        }

        return userAgent ?? "未知";
    }
}