using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClaudeCodeProxy.Abstraction;
using Microsoft.Extensions.Logging;
using Thor.Abstractions.Anthropic;

namespace ClaudeCodeProxy.Host.Helper;

public class SessionHelper
{
    private readonly ILogger<SessionHelper> _logger;

    public SessionHelper(ILogger<SessionHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 生成会话哈希，用于sticky会话保持
    /// 基于Anthropic的prompt caching机制，优先使用cacheable内容
    /// </summary>
    /// <param name="requestBody">请求体</param>
    /// <returns>32字符的会话哈希，如果无法生成则返回null</returns>
    public string? GenerateSessionHash(AnthropicInput requestBody)
    {
        string cacheableContent = "";

        try
        {
            // 将对象序列化为JSON以便处理
            var json = JsonSerializer.Serialize(requestBody,ThorJsonSerializer.DefaultOptions);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // 1. 优先提取带有cache_control: {"type": "ephemeral"}的内容
            // 检查system中的cacheable内容

            if (requestBody.SystemCalculated is IList<AnthropicMessageContent> systems)
            {
                foreach (var content in systems)
                {
                    if (content.CacheControl?.Type == "ephemeral" &&
                        content.Type == "text" &&
                        !string.IsNullOrEmpty(content.Text))
                    {
                        cacheableContent += content.Text;
                    }
                }
            }

            foreach (var message in requestBody.Messages)
            {
                if (message.ContentCalculated is IList<AnthropicMessageContent> contents)
                {
                    foreach (var content in contents)
                    {
                        if (content.CacheControl?.Type == "ephemeral" &&
                            content.Type == "text" &&
                            !string.IsNullOrEmpty(content.Text))
                        {
                            cacheableContent += content.Text;
                        }
                    }
                }
                else if (message.ContentCalculated is string textContent &&
                         !string.IsNullOrEmpty(textContent))
                {
                    // 检查消息级别的cache_control
                    if (message.CacheControl?.Type == "ephemeral")
                    {
                        cacheableContent += textContent;
                    }
                }
            }

            // 2. 如果有cacheable内容，直接使用
            if (!string.IsNullOrEmpty(cacheableContent))
            {
                var hash = GenerateHash(cacheableContent);
                _logger.LogDebug("📋 Session hash generated from cacheable content: {Hash}", hash);
                return hash;
            }

            // 3. Fallback: 使用system内容
            if (root.TryGetProperty("system", out var systemFallback))
            {
                string systemText = "";

                if (systemFallback.ValueKind == JsonValueKind.String)
                {
                    systemText = systemFallback.GetString() ?? "";
                }
                else if (systemFallback.ValueKind == JsonValueKind.Array)
                {
                    var parts = new List<string>();
                    foreach (var part in systemFallback.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var text))
                        {
                            parts.Add(text.GetString() ?? "");
                        }
                    }

                    systemText = string.Join("", parts);
                }

                if (!string.IsNullOrEmpty(systemText))
                {
                    var hash = GenerateHash(systemText);
                    _logger.LogDebug("📋 Session hash generated from system content: {Hash}", hash);
                    return hash;
                }
            }

            foreach (var message in requestBody.Messages)
            {
                // 检查消息级别的cache_control
                if (message.CacheControl?.Type == "ephemeral")
                {
                    continue; // 已经处理过了
                }

                // 处理消息内容
                if (message.ContentCalculated is IList<AnthropicMessageContent> contents)
                {
                    foreach (var content in contents)
                    {
                        if (content.Type == "text" && !string.IsNullOrEmpty(content.Text))
                        {
                            var hash = GenerateHash(content.Text);
                            _logger.LogDebug("📋 Session hash generated from message content: {Hash}", hash);
                            return hash;
                        }
                    }
                }
                else if (message.ContentCalculated is string textContent &&
                         !string.IsNullOrEmpty(textContent))
                {
                    var hash = GenerateHash(textContent);
                    _logger.LogDebug("📋 Session hash generated from message content: {Hash}", hash);
                    return hash;
                }
            }

            // 无法生成会话哈希
            _logger.LogDebug("📋 Unable to generate session hash - no suitable content found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session hash");
            return null;
        }
    }

    /// <summary>
    /// 获取会话的Redis键名
    /// </summary>
    /// <param name="sessionHash">会话哈希</param>
    /// <returns>Redis键名</returns>
    public string GetSessionRedisKey(string sessionHash)
    {
        return $"sticky_session:{sessionHash}";
    }

    /// <summary>
    /// 验证会话哈希格式
    /// </summary>
    /// <param name="sessionHash">会话哈希</param>
    /// <returns>是否有效</returns>
    public bool IsValidSessionHash(string? sessionHash)
    {
        return !string.IsNullOrEmpty(sessionHash) &&
               sessionHash.Length == 32 &&
               System.Text.RegularExpressions.Regex.IsMatch(sessionHash, "^[a-f0-9]{32}$");
    }

    /// <summary>
    /// 生成SHA256哈希值的前32个字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>32字符的哈希值</returns>
    private string GenerateHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).Substring(0, 32).ToLowerInvariant();
    }
}