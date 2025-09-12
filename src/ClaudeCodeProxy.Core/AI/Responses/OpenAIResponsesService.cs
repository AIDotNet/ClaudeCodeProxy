using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Linq;
using ClaudeCodeProxy.Abstraction;
using ClaudeCodeProxy.Core.Extensions;
using ClaudeCodeProxy.Domain;
using Microsoft.Extensions.Logging;
using Thor.Abstractions;
using Thor.Abstractions.Chats.Dtos;
using Thor.Abstractions.Exceptions;
using Thor.Abstractions.Responses;
using Thor.Abstractions.Responses.Dto;

namespace ClaudeCodeProxy.Core.AI.Responses;

public sealed class OpenAIResponsesService(ILogger<OpenAIResponsesService> logger) : IThorResponsesService
{
    public async Task<ResponsesDto> GetResponseAsync(ResponsesInput input,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var openai =
            Activity.Current?.Source.StartActivity("OpenAI Responses 对话补全");

        var response = await HttpClientFactory.GetHttpClient(options.Address, config).HttpRequestRaw(
            options?.Address.TrimEnd('/') + "/responses",
            input, options.ApiKey, headers).ConfigureAwait(false);

        openai?.SetTag("Model", input.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        if (response.StatusCode == HttpStatusCode.Unauthorized) throw new BusinessException("渠道未登录,请联系管理人员", "401");

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("OpenAI对话异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}", options.Address,
                response.StatusCode, error);

            throw new BusinessException("OpenAI对话异常", response.StatusCode.ToString());
        }

        var result =
            await response.Content.ReadFromJsonAsync<ResponsesDto>(
                cancellationToken).ConfigureAwait(false);

        return result;
    }

    public async IAsyncEnumerable<(string @event, ResponsesSSEDto<ResponsesDto> responses)> GetResponsesAsync(
        ResponsesInput input,
        Dictionary<string, string> headers, ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var openai =
            Activity.Current?.Source.StartActivity("OpenAI 对话补全");

        var response = await HttpClientFactory.GetHttpClient(options.Address, config).PostJsonAsync(
            options?.Address.TrimEnd('/') + "/responses",
            input, options.ApiKey, headers).ConfigureAwait(false);

        openai?.SetTag("Model", input.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        if (response.StatusCode == HttpStatusCode.Unauthorized) throw new BusinessException("渠道未登录,请联系管理人员", "401");

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("OpenAI对话异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}", options.Address,
                response.StatusCode, error);

            throw new BusinessException("OpenAI对话异常", response.StatusCode.ToString());
        }


        using var stream = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));

        using StreamReader reader = new(await response.Content.ReadAsStreamAsync(cancellationToken));
        var line = string.Empty;
        var first = true;
        var isThink = false;

        var @event = string.Empty;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("event: "))
            {
                @event = line[6..].Trim();
                continue;
            }

            if (line.StartsWith("data: ")) line = line[6..].Trim();


            var result = JsonSerializer.Deserialize<ResponsesSSEDto<ResponsesDto>>(line,
                ThorJsonSerializer.DefaultOptions);
            yield return (@event, result);
        }
    }

    public async Task<ResponsesDto> GetStreamToResponseAsync(ResponsesInput input,
        Dictionary<string, string> headers, ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // 强制以流式方式请求，然后聚合为同步结果（文本、推理、工具调用、图片等）
        input.Stream = true;

        // 累积容器
        var fullTextBuilder = new StringBuilder(); // 汇总所有输出文本
        var reasoningBuilder = new StringBuilder(); // 汇总thinking/reasoning

        // outputIndex -> ResponsesOutputDto
        var outputsByIndex = new Dictionary<int, ResponsesOutputDto>();

        // (outputIndex, contentIndex) -> (type, textBuffer)
        var contentTypeByKey = new Dictionary<(int o, int c), string>();
        var contentBuffers = new Dictionary<(int o, int c), StringBuilder>();
        var contentAnnotations = new Dictionary<(int o, int c), object[]?>();

        // function_call 聚合：itemId -> outputIndex（便于关联）
        var funcItemToOutput = new Dictionary<string, int>();
        var funcRawBuffersByItem = new Dictionary<string, StringBuilder>();

        // queries / results 聚合
        var queriesByOutput = new Dictionary<int, List<object>>();
        var resultsByOutput = new Dictionary<int, List<ResponsesOutputFileContentResults>>();

        // 使用统计/错误/完成标记
        ResponsesUsageDto? lastUsage = null;
        object? lastError = null;
        var isCompleted = false;

        ResponsesDto? lastResponse = null;

        await foreach (var (eventName, sse) in GetResponsesAsync(input, headers, config, options, cancellationToken))
        {
            if (sse == null) continue;

            var outIdx = sse.OutputIndex ?? 0;
            var cntIdx = sse.ContentIndex ?? 0;

            // 保存服务端返回的完整响应（通常在 response.done/response.completed 事件中带回）
            if (sse.Response != null)
            {
                lastResponse = sse.Response;
                if (sse.Response.Usage != null) lastUsage = sse.Response.Usage;
                if (!string.IsNullOrEmpty(sse.Response.Status) && sse.Response.Status is "completed" or "failed")
                    isCompleted = true;
                if (sse.Response.Error != null) lastError = sse.Response.Error;
            }

            // 若收到完成事件，直接返回完整响应，避免增量聚合
            if ((string.Equals(eventName, "response.completed", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(eventName, "response.done", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(eventName, "response.complete", StringComparison.OrdinalIgnoreCase))
                && sse.Response != null)
            {
                sse.Response.Status ??= "completed";
                return sse.Response;
            }

            // 错误事件即时抛出，避免继续聚合无意义数据
            if (string.Equals(eventName, "response.error", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventName, "error", StringComparison.OrdinalIgnoreCase))
            {
                var err = sse.Response?.Error ?? (object?)(sse.Text ?? sse.Parts?.Text ?? "error");
                throw new BusinessException(err?.ToString() ?? "OpenAI对话异常", "500");
            }

            // 处理工具调用（function_call / response.function_call.delta）
            var itemType = sse.Item?.Type?.ToLowerInvariant();
            if (itemType == "function_call" || string.Equals(eventName, "response.function_call.delta", StringComparison.OrdinalIgnoreCase))
            {
                // 以 itemId 或回退到 outputIndex 标识一次函数调用
                var itemId = sse.Item?.Id ?? sse.ItemId ?? $"func-{outIdx}";

                if (!funcItemToOutput.ContainsKey(itemId)) funcItemToOutput[itemId] = outIdx;

                if (!funcRawBuffersByItem.TryGetValue(itemId, out var raw))
                {
                    raw = new StringBuilder();
                    funcRawBuffersByItem[itemId] = raw;
                }

                // 聚合可能出现在 Item.Content/Text/Delta/Parts.Text 的增量
                if (sse.Item?.Content != null)
                    foreach (var p in sse.Item.Content)
                        if (!string.IsNullOrEmpty(p?.Text)) raw.Append(p.Text);

                if (!string.IsNullOrEmpty(sse.Delta)) raw.Append(sse.Delta);
                if (!string.IsNullOrEmpty(sse.Text)) raw.Append(sse.Text);
                if (!string.IsNullOrEmpty(sse.Parts?.Text)) raw.Append(sse.Parts.Text);

                // 跳过其它处理，等待最终汇总
                continue;
            }

            // 处理图片/多模态内容（根据 item/parts 的 type 粗略推断）
            var partType = sse.Parts?.Type?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(partType) && (partType.Contains("image") || partType.Contains("media") || partType.Contains("file")))
            {
                var key = (outIdx, cntIdx);
                if (!contentTypeByKey.ContainsKey(key)) contentTypeByKey[key] = "image";
                if (!contentBuffers.TryGetValue(key, out var imgBuf))
                {
                    imgBuf = new StringBuilder();
                    contentBuffers[key] = imgBuf;
                }
                if (!string.IsNullOrEmpty(sse.Text)) imgBuf.Append(sse.Text);
                if (!string.IsNullOrEmpty(sse.Parts?.Text)) imgBuf.Append(sse.Parts.Text);
                // 捕获注解
                if (!contentAnnotations.ContainsKey(key)) contentAnnotations[key] = sse.Parts?.Annotations;

                // 尝试将图片/文件作为结果收集（文本内容作为描述）
                if (!resultsByOutput.TryGetValue(outIdx, out var resList))
                {
                    resList = new List<ResponsesOutputFileContentResults>();
                    resultsByOutput[outIdx] = resList;
                }
                resList.Add(new ResponsesOutputFileContentResults
                {
                    Text = (sse.Parts?.Text ?? sse.Text) ?? string.Empty,
                    Type = partType
                });

                continue;
            }

            // 处理reasoning（推理/思考内容）
            if (itemType == "reasoning")
            {
                if (sse.Item?.Content != null)
                    foreach (var p in sse.Item.Content)
                        if (!string.IsNullOrEmpty(p?.Text)) reasoningBuilder.Append(p.Text);
            }

            if (sse.Response?.Reasoning?.Summary != null)
                try
                {
                    // 尝试提取为字符串
                    var summary = sse.Response.Reasoning.Summary?.ToString();
                    if (!string.IsNullOrEmpty(summary)) reasoningBuilder.Append(summary);
                }
                catch
                {
                    // ignore
                }

            // 处理普通文本/查询增量（response.output_text.delta / response.text.delta 等）
            if (!string.IsNullOrEmpty(sse.Delta) || !string.IsNullOrEmpty(sse.Text) || !string.IsNullOrEmpty(sse.Parts?.Text))
            {
                var key = (outIdx, cntIdx);
                if (!contentTypeByKey.ContainsKey(key)) contentTypeByKey[key] = "text";
                if (!contentBuffers.TryGetValue(key, out var buf))
                {
                    buf = new StringBuilder();
                    contentBuffers[key] = buf;
                }

                if (!string.IsNullOrEmpty(sse.Delta)) buf.Append(sse.Delta);
                if (!string.IsNullOrEmpty(sse.Text)) buf.Append(sse.Text);
                if (!string.IsNullOrEmpty(sse.Parts?.Text)) buf.Append(sse.Parts.Text);
                // 捕获注解
                if (!contentAnnotations.ContainsKey(key)) contentAnnotations[key] = sse.Parts?.Annotations;

                // 总文本
                if (contentTypeByKey[key] == "text")
                {
                    if (!string.IsNullOrEmpty(sse.Delta)) fullTextBuilder.Append(sse.Delta);
                    if (!string.IsNullOrEmpty(sse.Text)) fullTextBuilder.Append(sse.Text);
                    if (!string.IsNullOrEmpty(sse.Parts?.Text)) fullTextBuilder.Append(sse.Parts.Text);
                }

                // 将查询内容单独收集
                if (!string.IsNullOrEmpty(partType) && partType.Contains("query"))
                {
                    if (!queriesByOutput.TryGetValue(outIdx, out var ql))
                    {
                        ql = new List<object>();
                        queriesByOutput[outIdx] = ql;
                    }
                    ql.Add(new { text = sse.Parts?.Text ?? sse.Text ?? sse.Delta });
                }
            }

            // 处理错误/完成事件（基于事件名兜底）
            if (string.Equals(eventName, "response.error", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(eventName, "error", StringComparison.OrdinalIgnoreCase))
            {
                lastError ??= (object)(sse.Text ?? sse.Parts?.Text ?? "error");
            }

            if (string.Equals(eventName, "response.done", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(eventName, "response.completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(eventName, "response.complete", StringComparison.OrdinalIgnoreCase))
            {
                isCompleted = true;
            }
        }

        // 以服务端最终响应作为基础；没有则自建一个
        var result = lastResponse ?? new ResponsesDto
        {
            Id = Guid.NewGuid().ToString(),
            Object = "response",
            CreatedAt = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = input.Model,
            Status = "completed"
        };

        // 合成输出：先处理 function_call，再处理文本/图片
        var usedOutputSlots = new HashSet<int>();
        foreach (var kv in funcItemToOutput)
        {
            var outIdx = kv.Value;
            // 解析对应 item 的累积JSON，尽力提取 name / arguments
            var itemId = kv.Key;
            funcRawBuffersByItem.TryGetValue(itemId, out var raw);
            var rawStr = raw?.ToString();

            string? funcName = null;
            string? funcArgs = null;

            if (!string.IsNullOrEmpty(rawStr))
            {
                try
                {
                    using var doc = JsonDocument.Parse(rawStr);
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("name", out var nameEl)) funcName = nameEl.GetString();
                        if (root.TryGetProperty("arguments", out var argsEl))
                        {
                            funcArgs = argsEl.ValueKind == JsonValueKind.String
                                ? argsEl.GetString()
                                : argsEl.GetRawText();
                        }
                    }
                    else
                    {
                        funcArgs = root.GetRawText();
                    }
                }
                catch
                {
                    // 不是完整JSON，作为原始参数字符串返回
                    funcArgs = rawStr;
                }
            }

            // 确保不覆盖已有占位，必要时寻找下一个空位
            var targetIdx = outIdx;
            while (outputsByIndex.ContainsKey(targetIdx) && outputsByIndex[targetIdx] != null)
                targetIdx++;
            usedOutputSlots.Add(targetIdx);

            outputsByIndex[targetIdx] = new ResponsesOutputDto
            {
                Type = "function",
                Id = itemId,
                CallId = itemId,
                Name = funcName,
                Arguments = funcArgs,
                Role = "assistant",
                Status = "completed"
            };
        }

        // 文本/图片内容
        foreach (var entry in contentBuffers)
        {
            var (outIdx, cntIdx) = entry.Key;
            var text = entry.Value.ToString();
            var ctype = contentTypeByKey.GetValueOrDefault(entry.Key, "text");

            var originalOutIdx = outIdx;
            if (!outputsByIndex.TryGetValue(outIdx, out var outDto) || outDto == null || outDto.Type == "function")
            {
                // 如果已是 function，则在下一个可用索引补充文本；否则新建文本输出
                if (outDto == null || outDto.Type == "function")
                {
                    // 把文本内容放到一个新的输出槽位（避免覆盖function）
                    var newIdx = outIdx;
                    while (usedOutputSlots.Contains(newIdx) || (outputsByIndex.ContainsKey(newIdx) && outputsByIndex[newIdx]?.Type == "function"))
                        newIdx++;
                    outIdx = newIdx;
                }

                outDto = new ResponsesOutputDto
                {
                    Role = "assistant",
                    Content = Array.Empty<ResponsesContent>()
                };
                outputsByIndex[outIdx] = outDto;
                usedOutputSlots.Add(outIdx);

                // 如果发生了索引迁移，将已收集的 queries/results 从原索引迁移到新索引
                if (originalOutIdx != outIdx)
                {
                    if (queriesByOutput.TryGetValue(originalOutIdx, out var movedQ))
                    {
                        queriesByOutput.Remove(originalOutIdx);
                        if (movedQ?.Count > 0)
                            queriesByOutput[outIdx] = movedQ;
                    }
                    if (resultsByOutput.TryGetValue(originalOutIdx, out var movedR))
                    {
                        resultsByOutput.Remove(originalOutIdx);
                        if (movedR?.Count > 0)
                            resultsByOutput[outIdx] = movedR;
                    }
                }
            }

            var contents = outDto.Content?.ToList() ?? new List<ResponsesContent>();
            while (contents.Count <= cntIdx) contents.Add(new ResponsesContent());
            contents[cntIdx] = new ResponsesContent
            {
                Type = ctype,
                Text = text,
                Annotations = contentAnnotations.TryGetValue((outIdx, cntIdx), out var ann) ? ann : null
            };
            outDto.Content = contents.ToArray();

            // queries / results 注入到输出级别
            if (queriesByOutput.TryGetValue(outIdx, out var qlist) && qlist.Count > 0)
                outDto.Queries = qlist.ToArray();
            if (resultsByOutput.TryGetValue(outIdx, out var rlist) && rlist.Count > 0)
                outDto.Results = rlist.ToArray();
        }

        // reasoning 汇总
        var reasoningText = reasoningBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(reasoningText))
        {
            result.Reasoning ??= new Reasoning();
            // 尽量以字符串形式保存
            result.Reasoning.Summary = reasoningText;
        }

        // OutputText 汇总
        var fullText = fullTextBuilder.ToString();
        if (!string.IsNullOrEmpty(fullText)) result.OutputText = fullText;

        // 写回输出（如服务端已经提供输出且非空，优先保留服务端；否则使用我们聚合的）
        if (result.Output == null || result.Output.Length == 0)
        {
            result.Output = outputsByIndex
                .OrderBy(k => k.Key)
                .Select(k => k.Value)
                .ToArray();
        }

        // 使用统计：若服务端未提供或不完整，使用最后一次看到的usage
        if (result.Usage == null && lastUsage != null)
            result.Usage = lastUsage;
        if (result.Usage != null && result.Usage.TotalTokens == 0)
            result.Usage.TotalTokens = result.Usage.InputTokens + result.Usage.OutputTokens;

        // 错误/状态
        if (lastError != null)
        {
            result.Error = lastError;
            result.Status = "failed";
        }
        else if (isCompleted && string.IsNullOrEmpty(result.Status))
        {
            result.Status = "completed";
        }

        // 确保状态
        if (string.IsNullOrEmpty(result.Status)) result.Status = "completed";

        return result;
    }
}
