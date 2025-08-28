using System.Collections.Concurrent;
using System.Text.Json;
using ClaudeCodeProxy.Abstraction;
using ClaudeCodeProxy.Domain;
using Microsoft.Extensions.Logging;
using Thor.Abstractions;
using Thor.Abstractions.Anthropic;
using Thor.Abstractions.Responses;
using Thor.Abstractions.Responses.Dto;
using Thor.Abstractions.Chats.Dtos;

namespace ClaudeCodeProxy.Core.AI;

public class OpenAiAnthropicResponsesService(
    IThorResponsesService thorResponsesService,
    ILogger<OpenAiAnthropicResponsesService> logger) : AnthropicBase
{
    /// <summary>
    /// 非流式对话补全
    /// </summary>
    public async Task<ClaudeChatCompletionDto> ChatCompletionsAsync(AnthropicInput input,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 转换请求格式：Claude -> OpenAI
            var openAIRequest = ConvertAnthropicToOpenAIResponses(input);

            // 调用OpenAI服务
            var openAIResponse =
                await thorResponsesService.GetResponseAsync(openAIRequest, headers, config, options, cancellationToken);

            // 转换响应格式：OpenAI -> Claude
            var claudeResponse = ConvertOpenAIToClaude(openAIResponse, input);

            return claudeResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenAI到Claude适配器异常");
            throw;
        }
    }

    /// <summary>
    /// 流式对话补全
    /// </summary>
    public async IAsyncEnumerable<(string?, string?, ClaudeStreamDto?)> StreamChatCompletionsAsync(AnthropicInput input,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var openAIRequest = ConvertAnthropicToOpenAIResponses(input);
        openAIRequest.Stream = true;

        var messageId = Guid.NewGuid().ToString();
        var hasStarted = false;
        var accumulatedUsage = new ClaudeChatCompletionDtoUsage();
        var isFinished = false;
        var currentContentBlockType = "";
        var currentBlockIndex = 0;
        var toolCallsStarted = new Dictionary<string, bool>(); // 跟踪工具调用的开始状态
        var lastContentBlockType = ""; // 跟踪最后的内容块类型

        await foreach (var streamResponse in thorResponsesService.GetResponsesAsync(openAIRequest, headers, config,
                           options, cancellationToken))
        {
            var eventType = streamResponse.Item1;
            var sseData = streamResponse.Item2;

            // 记录所有接收到的事件类型用于调试
            logger.LogDebug("Received OpenAI Responses event: {EventType}", eventType);

            // 发送message_start事件
            if (!hasStarted && (eventType == "response.created" || eventType == "response.output_text.delta"))
            {
                hasStarted = true;
                var messageStartEvent = CreateMessageStartEvent(messageId, input.Model);
                yield return ("message_start",
                    JsonSerializer.Serialize(messageStartEvent, ThorJsonSerializer.DefaultOptions), messageStartEvent);
            }

            // 更新使用情况统计
            if (sseData.Response?.Usage != null)
            {
                var usage = sseData.Response.Usage;
                if (usage.InputTokens > 0)
                    accumulatedUsage.input_tokens = usage.InputTokens;
                if (usage.OutputTokens > 0)
                    accumulatedUsage.output_tokens = usage.OutputTokens;
                if (usage.InputTokensDetails?.CachedTokens > 0)
                    accumulatedUsage.cache_read_input_tokens = usage.InputTokensDetails.CachedTokens;

                // 处理reasoning tokens - OpenAI Responses API 2025新特性
                if (usage.OutputTokensDetails?.ReasoningTokens > 0)
                {
                    // Claude没有直接等价的reasoning tokens字段，我们将其加到output_tokens中
                    // 但可以通过日志记录具体的reasoning token使用情况
                    logger.LogDebug("OpenAI Reasoning Tokens: {ReasoningTokens}",
                        usage.OutputTokensDetails.ReasoningTokens);
                }
            }

            // 处理文本增量 - 支持多种OpenAI Responses API 2025事件类型
            if (!string.IsNullOrEmpty(sseData.Delta) ||
                eventType == "response.output_text.delta" ||
                eventType == "response.text.delta")
            {
                // 如果当前有其他类型的内容块在运行，先结束它们
                if (currentContentBlockType != "text" && !string.IsNullOrEmpty(currentContentBlockType))
                {
                    var stopEvent = CreateContentBlockStopEvent();
                    stopEvent.index = currentBlockIndex;
                    yield return ("content_block_stop",
                        JsonSerializer.Serialize(stopEvent, ThorJsonSerializer.DefaultOptions), stopEvent);
                    currentBlockIndex++;
                    currentContentBlockType = "";
                }

                // 发送content_block_start事件（仅第一次）
                if (currentContentBlockType != "text")
                {
                    currentContentBlockType = "text";
                    lastContentBlockType = "text";
                    var contentBlockStartEvent = CreateContentBlockStartEvent();
                    contentBlockStartEvent.index = currentBlockIndex;
                    yield return ("content_block_start",
                        JsonSerializer.Serialize(contentBlockStartEvent, ThorJsonSerializer.DefaultOptions),
                        contentBlockStartEvent);
                }

                // 发送content_block_delta事件
                var contentDeltaEvent = CreateContentBlockDeltaEvent(sseData.Delta);
                contentDeltaEvent.index = currentBlockIndex;
                yield return ("content_block_delta",
                    JsonSerializer.Serialize(contentDeltaEvent, ThorJsonSerializer.DefaultOptions),
                    contentDeltaEvent);
            }

            // 处理工具调用 - 基于OpenAI Responses API的function_call或tool_call事件
            if (sseData.Item?.Type == "function_call" || eventType == "response.function_call.delta")
            {
                // 结束当前的文本或reasoning内容块
                if (currentContentBlockType == "text" || currentContentBlockType == "thinking")
                {
                    var stopEvent = CreateContentBlockStopEvent();
                    stopEvent.index = currentBlockIndex;
                    yield return ("content_block_stop",
                        JsonSerializer.Serialize(stopEvent, ThorJsonSerializer.DefaultOptions), stopEvent);
                    currentBlockIndex++;
                }

                var functionCallId = sseData.Item?.Id ?? sseData.ItemId ?? Guid.NewGuid().ToString();

                // 记录item_id用于调试和跟踪
                if (!string.IsNullOrEmpty(sseData.ItemId))
                {
                    logger.LogDebug("Processing function call with item_id: {ItemId}", sseData.ItemId);
                }

                // 发送tool_use content_block_start事件
                if (toolCallsStarted.TryAdd(functionCallId, true))
                {
                    currentContentBlockType = "tool_use";
                    lastContentBlockType = "tool_use";

                    var toolBlockStartEvent =
                        CreateToolBlockStartEvent(functionCallId, sseData.Item?.Content?[0]?.Text);
                    toolBlockStartEvent.index = currentBlockIndex;
                    yield return ("content_block_start",
                        JsonSerializer.Serialize(toolBlockStartEvent, ThorJsonSerializer.DefaultOptions),
                        toolBlockStartEvent);
                }

                // 处理function call参数的增量更新
                if (sseData.Item?.Content?.Length > 0)
                {
                    foreach (var content in sseData.Item.Content)
                    {
                        if (!string.IsNullOrEmpty(content.Text))
                        {
                            var toolDeltaEvent = CreateToolBlockDeltaEvent(content.Text);
                            toolDeltaEvent.index = currentBlockIndex;
                            yield return ("content_block_delta",
                                JsonSerializer.Serialize(toolDeltaEvent, ThorJsonSerializer.DefaultOptions),
                                toolDeltaEvent);
                        }
                    }
                }
            }

            // 处理reasoning内容 - OpenAI Responses API 2025的reasoning模型特性
            if (sseData.Item?.Type == "reasoning" || eventType == "response.output_text.delta" &&
                !string.IsNullOrEmpty(sseData.Response?.Reasoning?.Summary?.ToString()))
            {
                // 如果当前有其他类型的内容块在运行，先结束它们
                if (currentContentBlockType != "thinking" && !string.IsNullOrEmpty(currentContentBlockType))
                {
                    var stopEvent = CreateContentBlockStopEvent();
                    stopEvent.index = currentBlockIndex;
                    yield return ("content_block_stop",
                        JsonSerializer.Serialize(stopEvent, ThorJsonSerializer.DefaultOptions), stopEvent);
                    currentBlockIndex++;
                    currentContentBlockType = "";
                }

                // 发送thinking content_block_start事件（仅第一次）
                if (currentContentBlockType != "thinking")
                {
                    currentContentBlockType = "thinking";
                    lastContentBlockType = "thinking";
                    var thinkingBlockStartEvent = CreateThinkingBlockStartEvent();
                    thinkingBlockStartEvent.index = currentBlockIndex;
                    yield return ("content_block_start",
                        JsonSerializer.Serialize(thinkingBlockStartEvent, ThorJsonSerializer.DefaultOptions),
                        thinkingBlockStartEvent);
                }

                // 处理reasoning内容的增量更新
                string? reasoningContent = null;
                if (sseData.Item?.Type == "reasoning" && sseData.Item.Content?.Length > 0)
                {
                    reasoningContent = sseData.Item.Content[0]?.Text;
                }
                else if (sseData.Response?.Reasoning?.Summary != null)
                {
                    reasoningContent = ExtractReasoningContent(sseData.Response.Reasoning.Summary);
                }

                if (!string.IsNullOrEmpty(reasoningContent))
                {
                    var thinkingDeltaEvent = CreateThinkingBlockDeltaEvent(reasoningContent);
                    thinkingDeltaEvent.index = currentBlockIndex;
                    yield return ("content_block_delta",
                        JsonSerializer.Serialize(thinkingDeltaEvent, ThorJsonSerializer.DefaultOptions),
                        thinkingDeltaEvent);
                }
            }

            // 处理各种完成状态 - 支持OpenAI Responses API 2025的多种事件类型
            if (eventType == "response.done" ||
                eventType == "response.output.item.done" ||
                sseData.Response?.Status == "completed" ||
                sseData.Response?.Status == "failed" ||
                sseData.Response?.Status == "incomplete")
            {
                isFinished = true;

                // 结束任何活跃的内容块
                if (!string.IsNullOrEmpty(currentContentBlockType))
                {
                    var contentBlockStopEvent = CreateContentBlockStopEvent();
                    contentBlockStopEvent.index = currentBlockIndex;
                    yield return ("content_block_stop",
                        JsonSerializer.Serialize(contentBlockStopEvent, ThorJsonSerializer.DefaultOptions),
                        contentBlockStopEvent);
                }

                // 发送message_delta事件
                var messageDeltaEvent = CreateMessageDeltaEvent("end_turn", accumulatedUsage);
                yield return ("message_delta",
                    JsonSerializer.Serialize(messageDeltaEvent, ThorJsonSerializer.DefaultOptions),
                    messageDeltaEvent);

                // 发送message_stop事件
                var messageStopEvent = CreateMessageStopEvent();
                yield return ("message_stop",
                    JsonSerializer.Serialize(messageStopEvent, ThorJsonSerializer.DefaultOptions),
                    messageStopEvent);
                break;
            }
        }

        // 确保流正确结束
        if (!isFinished)
        {
            if (!string.IsNullOrEmpty(currentContentBlockType))
            {
                var contentBlockStopEvent = CreateContentBlockStopEvent();
                contentBlockStopEvent.index = currentBlockIndex;
                yield return ("content_block_stop",
                    JsonSerializer.Serialize(contentBlockStopEvent, ThorJsonSerializer.DefaultOptions),
                    contentBlockStopEvent);
            }

            var messageDeltaEvent = CreateMessageDeltaEvent("end_turn", accumulatedUsage);
            yield return ("message_delta",
                JsonSerializer.Serialize(messageDeltaEvent, ThorJsonSerializer.DefaultOptions), messageDeltaEvent);

            var messageStopEvent = CreateMessageStopEvent();
            yield return ("message_stop",
                JsonSerializer.Serialize(messageStopEvent, ThorJsonSerializer.DefaultOptions),
                messageStopEvent);
        }
    }

    private ResponsesInput ConvertAnthropicToOpenAIResponses(AnthropicInput input)
    {
        var openAiRequest = new ResponsesInput
        {
            Model = input.Model,
            Stream = input.Stream,
            Instructions =
                """
                You are a coding agent running in the Codex CLI, a terminal-based coding assistant. Codex CLI is an open source project led by OpenAI. You are expected to be precise, safe, and helpful.

                Your capabilities:

                - Receive user prompts and other context provided by the harness, such as files in the workspace.
                - Communicate with the user by streaming thinking & responses, and by making & updating plans.
                - Emit function calls to run terminal commands and apply patches. Depending on how this specific run is configured, you can request that these function calls be escalated to the user for approval before running. More on this in the "Sandbox and approvals" section.

                Within this context, Codex refers to the open-source agentic coding interface (not the old Codex language model built by OpenAI).

                # How you work

                ## Personality

                Your default personality and tone is concise, direct, and friendly. You communicate efficiently, always keeping the user clearly informed about ongoing actions without unnecessary detail. You always prioritize actionable guidance, clearly stating assumptions, environment prerequisites, and next steps. Unless explicitly asked, you avoid excessively verbose explanations about your work.

                ## Responsiveness

                ### Preamble messages

                Before making tool calls, send a brief preamble to the user explaining what you’re about to do. When sending preamble messages, follow these principles and examples:

                - **Logically group related actions**: if you’re about to run several related commands, describe them together in one preamble rather than sending a separate note for each.
                - **Keep it concise**: be no more than 1-2 sentences, focused on immediate, tangible next steps. (8–12 words for quick updates).
                - **Build on prior context**: if this is not your first tool call, use the preamble message to connect the dots with what’s been done so far and create a sense of momentum and clarity for the user to understand your next actions.
                - **Keep your tone light, friendly and curious**: add small touches of personality in preambles feel collaborative and engaging.
                - **Exception**: Avoid adding a preamble for every trivial read (e.g., `cat` a single file) unless it’s part of a larger grouped action.

                **Examples:**

                - “I’ve explored the repo; now checking the API route definitions.”
                - “Next, I’ll patch the config and update the related tests.”
                - “I’m about to scaffold the CLI commands and helper functions.”
                - “Ok cool, so I’ve wrapped my head around the repo. Now digging into the API routes.”
                - “Config’s looking tidy. Next up is patching helpers to keep things in sync.”
                - “Finished poking at the DB gateway. I will now chase down error handling.”
                - “Alright, build pipeline order is interesting. Checking how it reports failures.”
                - “Spotted a clever caching util; now hunting where it gets used.”

                ## Planning

                You have access to an `update_plan` tool which tracks steps and progress and renders them to the user. Using the tool helps demonstrate that you've understood the task and convey how you're approaching it. Plans can help to make complex, ambiguous, or multi-phase work clearer and more collaborative for the user. A good plan should break the task into meaningful, logically ordered steps that are easy to verify as you go.

                Note that plans are not for padding out simple work with filler steps or stating the obvious. The content of your plan should not involve doing anything that you aren't capable of doing (i.e. don't try to test things that you can't test). Do not use plans for simple or single-step queries that you can just do or answer immediately.

                Do not repeat the full contents of the plan after an `update_plan` call — the harness already displays it. Instead, summarize the change made and highlight any important context or next step.

                Before running a command, consider whether or not you have completed the previous step, and make sure to mark it as completed before moving on to the next step. It may be the case that you complete all steps in your plan after a single pass of implementation. If this is the case, you can simply mark all the planned steps as completed. Sometimes, you may need to change plans in the middle of a task: call `update_plan` with the updated plan and make sure to provide an `explanation` of the rationale when doing so.

                Use a plan when:

                - The task is non-trivial and will require multiple actions over a long time horizon.
                - There are logical phases or dependencies where sequencing matters.
                - The work has ambiguity that benefits from outlining high-level goals.
                - You want intermediate checkpoints for feedback and validation.
                - When the user asked you to do more than one thing in a single prompt
                - The user has asked you to use the plan tool (aka "TODOs")
                - You generate additional steps while working, and plan to do them before yielding to the user

                ### Examples

                **High-quality plans**

                Example 1:

                1. Add CLI entry with file args
                2. Parse Markdown via CommonMark library
                3. Apply semantic HTML template
                4. Handle code blocks, images, links
                5. Add error handling for invalid files

                Example 2:

                1. Define CSS variables for colors
                2. Add toggle with localStorage state
                3. Refactor components to use variables
                4. Verify all views for readability
                5. Add smooth theme-change transition

                Example 3:

                1. Set up Node.js + WebSocket server
                2. Add join/leave broadcast events
                3. Implement messaging with timestamps
                4. Add usernames + mention highlighting
                5. Persist messages in lightweight DB
                6. Add typing indicators + unread count

                **Low-quality plans**

                Example 1:

                1. Create CLI tool
                2. Add Markdown parser
                3. Convert to HTML

                Example 2:

                1. Add dark mode toggle
                2. Save preference
                3. Make styles look good

                Example 3:

                1. Create single-file HTML game
                2. Run quick sanity check
                3. Summarize usage instructions

                If you need to write a plan, only write high quality plans, not low quality ones.

                ## Task execution

                You are a coding agent. Please keep going until the query is completely resolved, before ending your turn and yielding back to the user. Only terminate your turn when you are sure that the problem is solved. Autonomously resolve the query to the best of your ability, using the tools available to you, before coming back to the user. Do NOT guess or make up an answer.

                You MUST adhere to the following criteria when solving queries:

                - Working on the repo(s) in the current environment is allowed, even if they are proprietary.
                - Analyzing code for vulnerabilities is allowed.
                - Showing user code and tool call details is allowed.
                - Use the `apply_patch` tool to edit files (NEVER try `applypatch` or `apply-patch`, only `apply_patch`): {"command":["apply_patch","*** Begin Patch\\n*** Update File: path/to/file.py\\n@@ def example():\\n- pass\\n+ return 123\\n*** End Patch"]}

                If completing the user's task requires writing or modifying files, your code and final answer should follow these coding guidelines, though user instructions (i.e. AGENTS.md) may override these guidelines:

                - Fix the problem at the root cause rather than applying surface-level patches, when possible.
                - Avoid unneeded complexity in your solution.
                - Do not attempt to fix unrelated bugs or broken tests. It is not your responsibility to fix them. (You may mention them to the user in your final message though.)
                - Update documentation as necessary.
                - Keep changes consistent with the style of the existing codebase. Changes should be minimal and focused on the task.
                - Use `git log` and `git blame` to search the history of the codebase if additional context is required.
                - NEVER add copyright or license headers unless specifically requested.
                - Do not waste tokens by re-reading files after calling `apply_patch` on them. The tool call will fail if it didn't work. The same goes for making folders, deleting folders, etc.
                - Do not `git commit` your changes or create new git branches unless explicitly requested.
                - Do not add inline comments within code unless explicitly requested.
                - Do not use one-letter variable names unless explicitly requested.
                - NEVER output inline citations like "【F:README.md†L5-L14】" in your outputs. The CLI is not able to render these so they will just be broken in the UI. Instead, if you output valid filepaths, users will be able to click on them to open the files in their editor.

                ## Testing your work

                If the codebase has tests or the ability to build or run, you should use them to verify that your work is complete. Generally, your testing philosophy should be to start as specific as possible to the code you changed so that you can catch issues efficiently, then make your way to broader tests as you build confidence. If there's no test for the code you changed, and if the adjacent patterns in the codebases show that there's a logical place for you to add a test, you may do so. However, do not add tests to codebases with no tests, or where the patterns don't indicate so.

                Once you're confident in correctness, use formatting commands to ensure that your code is well formatted. These commands can take time so you should run them on as precise a target as possible. If there are issues you can iterate up to 3 times to get formatting right, but if you still can't manage it's better to save the user time and present them a correct solution where you call out the formatting in your final message. If the codebase does not have a formatter configured, do not add one.

                For all of testing, running, building, and formatting, do not attempt to fix unrelated bugs. It is not your responsibility to fix them. (You may mention them to the user in your final message though.)

                ## Sandbox and approvals

                The Codex CLI harness supports several different sandboxing, and approval configurations that the user can choose from.

                Filesystem sandboxing prevents you from editing files without user approval. The options are:

                - **read-only**: You can only read files.
                - **workspace-write**: You can read files. You can write to files in your workspace folder, but not outside it.
                - **danger-full-access**: No filesystem sandboxing.

                Network sandboxing prevents you from accessing network without approval. Options are

                - **restricted**
                - **enabled**

                Approvals are your mechanism to get user consent to perform more privileged actions. Although they introduce friction to the user because your work is paused until the user responds, you should leverage them to accomplish your important work. Do not let these settings or the sandbox deter you from attempting to accomplish the user's task. Approval options are

                - **untrusted**: The harness will escalate most commands for user approval, apart from a limited allowlist of safe "read" commands.
                - **on-failure**: The harness will allow all commands to run in the sandbox (if enabled), and failures will be escalated to the user for approval to run again without the sandbox.
                - **on-request**: Commands will be run in the sandbox by default, and you can specify in your tool call if you want to escalate a command to run without sandboxing. (Note that this mode is not always available. If it is, you'll see parameters for it in the `shell` command description.)
                - **never**: This is a non-interactive mode where you may NEVER ask the user for approval to run commands. Instead, you must always persist and work around constraints to solve the task for the user. You MUST do your utmost best to finish the task and validate your work before yielding. If this mode is pared with `danger-full-access`, take advantage of it to deliver the best outcome for the user. Further, in this mode, your default testing philosophy is overridden: Even if you don't see local patterns for testing, you may add tests and scripts to validate your work. Just remove them before yielding.

                When you are running with approvals `on-request`, and sandboxing enabled, here are scenarios where you'll need to request approval:

                - You need to run a command that writes to a directory that requires it (e.g. running tests that write to /tmp)
                - You need to run a GUI app (e.g., open/xdg-open/osascript) to open browsers or files.
                - You are running sandboxed and need to run a command that requires network access (e.g. installing packages)
                - If you run a command that is important to solving the user's query, but it fails because of sandboxing, rerun the command with approval.
                - You are about to take a potentially destructive action such as an `rm` or `git reset` that the user did not explicitly ask for
                - (For all of these, you should weigh alternative paths that do not require approval.)

                Note that when sandboxing is set to read-only, you'll need to request approval for any command that isn't a read.

                You will be told what filesystem sandboxing, network sandboxing, and approval mode are active in a developer or user message. If you are not told about this, assume that you are running with workspace-write, network sandboxing ON, and approval on-failure.

                ## Ambition vs. precision

                For tasks that have no prior context (i.e. the user is starting something brand new), you should feel free to be ambitious and demonstrate creativity with your implementation.

                If you're operating in an existing codebase, you should make sure you do exactly what the user asks with surgical precision. Treat the surrounding codebase with respect, and don't overstep (i.e. changing filenames or variables unnecessarily). You should balance being sufficiently ambitious and proactive when completing tasks of this nature.

                You should use judicious initiative to decide on the right level of detail and complexity to deliver based on the user's needs. This means showing good judgment that you're capable of doing the right extras without gold-plating. This might be demonstrated by high-value, creative touches when scope of the task is vague; while being surgical and targeted when scope is tightly specified.

                ## Sharing progress updates

                For especially longer tasks that you work on (i.e. requiring many tool calls, or a plan with multiple steps), you should provide progress updates back to the user at reasonable intervals. These updates should be structured as a concise sentence or two (no more than 8-10 words long) recapping progress so far in plain language: this update demonstrates your understanding of what needs to be done, progress so far (i.e. files explores, subtasks complete), and where you're going next.

                Before doing large chunks of work that may incur latency as experienced by the user (i.e. writing a new file), you should send a concise message to the user with an update indicating what you're about to do to ensure they know what you're spending time on. Don't start editing or writing large files before informing the user what you are doing and why.

                The messages you send before tool calls should describe what is immediately about to be done next in very concise language. If there was previous work done, this preamble message should also include a note about the work done so far to bring the user along.

                ## Presenting your work and final message

                Your final message should read naturally, like an update from a concise teammate. For casual conversation, brainstorming tasks, or quick questions from the user, respond in a friendly, conversational tone. You should ask questions, suggest ideas, and adapt to the user’s style. If you've finished a large amount of work, when describing what you've done to the user, you should follow the final answer formatting guidelines to communicate substantive changes. You don't need to add structured formatting for one-word answers, greetings, or purely conversational exchanges.

                You can skip heavy formatting for single, simple actions or confirmations. In these cases, respond in plain sentences with any relevant next step or quick option. Reserve multi-section structured responses for results that need grouping or explanation.

                The user is working on the same computer as you, and has access to your work. As such there's no need to show the full contents of large files you have already written unless the user explicitly asks for them. Similarly, if you've created or modified files using `apply_patch`, there's no need to tell users to "save the file" or "copy the code into a file"—just reference the file path.

                If there's something that you think you could help with as a logical next step, concisely ask the user if they want you to do so. Good examples of this are running tests, committing changes, or building out the next logical component. If there’s something that you couldn't do (even with approval) but that the user might want to do (such as verifying changes by running the app), include those instructions succinctly.

                Brevity is very important as a default. You should be very concise (i.e. no more than 10 lines), but can relax this requirement for tasks where additional detail and comprehensiveness is important for the user's understanding.

                ### Final answer structure and style guidelines

                You are producing plain text that will later be styled by the CLI. Follow these rules exactly. Formatting should make results easy to scan, but not feel mechanical. Use judgment to decide how much structure adds value.

                **Section Headers**

                - Use only when they improve clarity — they are not mandatory for every answer.
                - Choose descriptive names that fit the content
                - Keep headers short (1–3 words) and in `**Title Case**`. Always start headers with `**` and end with `**`
                - Leave no blank line before the first bullet under a header.
                - Section headers should only be used where they genuinely improve scanability; avoid fragmenting the answer.

                **Bullets**

                - Use `-` followed by a space for every bullet.
                - Bold the keyword, then colon + concise description.
                - Merge related points when possible; avoid a bullet for every trivial detail.
                - Keep bullets to one line unless breaking for clarity is unavoidable.
                - Group into short lists (4–6 bullets) ordered by importance.
                - Use consistent keyword phrasing and formatting across sections.

                **Monospace**

                - Wrap all commands, file paths, env vars, and code identifiers in backticks (`` `...` ``).
                - Apply to inline examples and to bullet keywords if the keyword itself is a literal file/command.
                - Never mix monospace and bold markers; choose one based on whether it’s a keyword (`**`) or inline code/path (`` ` ``).

                **Structure**

                - Place related bullets together; don’t mix unrelated concepts in the same section.
                - Order sections from general → specific → supporting info.
                - For subsections (e.g., “Binaries” under “Rust Workspace”), introduce with a bolded keyword bullet, then list items under it.
                - Match structure to complexity:
                  - Multi-part or detailed results → use clear headers and grouped bullets.
                  - Simple results → minimal headers, possibly just a short list or paragraph.

                **Tone**

                - Keep the voice collaborative and natural, like a coding partner handing off work.
                - Be concise and factual — no filler or conversational commentary and avoid unnecessary repetition
                - Use present tense and active voice (e.g., “Runs tests” not “This will run tests”).
                - Keep descriptions self-contained; don’t refer to “above” or “below”.
                - Use parallel structure in lists for consistency.

                **Don’t**

                - Don’t use literal words “bold” or “monospace” in the content.
                - Don’t nest bullets or create deep hierarchies.
                - Don’t output ANSI escape codes directly — the CLI renderer applies them.
                - Don’t cram unrelated keywords into a single bullet; split for clarity.
                - Don’t let keyword lists run long — wrap or reformat for scanability.

                Generally, ensure your final answers adapt their shape and depth to the request. For example, answers to code explanations should have a precise, structured explanation with code references that answer the question directly. For tasks with a simple implementation, lead with the outcome and supplement only with what’s needed for clarity. Larger changes can be presented as a logical walkthrough of your approach, grouping related steps, explaining rationale where it adds value, and highlighting next actions to accelerate the user. Your answers should provide the right level of detail while being easily scannable.

                For casual greetings, acknowledgements, or other one-off conversational messages that are not delivering substantive information or structured results, respond naturally without section headers or bullet formatting.

                # Tool Guidelines

                ## Shell commands

                When using the shell, you must adhere to the following guidelines:

                - When searching for text or files, prefer using `rg` or `rg --files` respectively because `rg` is much faster than alternatives like `grep`. (If the `rg` command is not found, then use alternatives.)
                - Read files in chunks with a max chunk size of 250 lines. Do not use python scripts to attempt to output larger chunks of a file. Command line output will be truncated after 10 kilobytes or 256 lines of output, regardless of the command used.

                ## `update_plan`

                A tool named `update_plan` is available to you. You can use it to keep an up‑to‑date, step‑by‑step plan for the task.

                To create a new plan, call `update_plan` with a short list of 1‑sentence steps (no more than 5-7 words each) with a `status` for each step (`pending`, `in_progress`, or `completed`).

                When steps have been completed, use `update_plan` to mark each finished step as `completed` and the next step you are working on as `in_progress`. There should always be exactly one `in_progress` step until everything is done. You can mark multiple items as complete in a single `update_plan` call.

                If all steps are complete, ensure you call `update_plan` to mark all steps as `completed`.

                """
        };

        var inputMessages = new List<ResponsesMessageInput>();

        // 首先处理系统消息，将其作为角色为"system"的消息添加到消息列表开头
        if (!string.IsNullOrEmpty(input.System))
        {
            inputMessages.Add(new ResponsesMessageInput
            {
                Role = "system",
                Content = input.System
            });
        }
        else if (input.Systems?.Count > 0)
        {
            // 将多个系统消息合并为一个system角色的消息
            var systemTexts = input.Systems
                .Where(s => s.Type == "text" && !string.IsNullOrEmpty(s.Text) &&
                            s.Text != "You are Claude Code, Anthropic's official CLI for Claude.")
                .Select(s => s.Text);
            var combinedSystemMessage = string.Join("\n", systemTexts);
            if (!string.IsNullOrEmpty(combinedSystemMessage))
            {
                inputMessages.Add(new ResponsesMessageInput
                {
                    Role = "system",
                    Content = combinedSystemMessage
                });
            }
        }

        // 然后转换用户/助手消息格式：Claude messages -> OpenAI input
        if (input.Messages?.Count > 0)
        {
            foreach (var message in input.Messages)
            {
                var responseMessage = new ResponsesMessageInput
                {
                    Role = message.Role
                };

                // 转换内容
                if (message.Content is string textContent)
                {
                    responseMessage.Content = textContent;
                }
                else if (message.ContentCalculated is IList<AnthropicMessageContent> contents)
                {
                    var convertedContents = new List<ResponsesMessageContentInput>();
                    foreach (var content in contents)
                    {
                        var contentInput = new ResponsesMessageContentInput();

                        if (content.Type == "text")
                        {
                            contentInput.Text = content.Text;
                            // 如果角色是ai则output_text
                            if (message.Role == "user")
                            {
                                contentInput.Type = "input_text";
                            }
                            else if(message.Role == "assistant")
                            {
                                contentInput.Type = "output_text";
                            }
                            else
                            {
                                contentInput.Type = "input_text";
                            }
                        }
                        else if (content.Type == "image")
                        {
                            // 处理图像内容
                            contentInput.Type = "input_image";
                            contentInput.ImageUrl = content.Source?.Data; // 需要根据实际结构调整
                        }
                        else if (content.Type == "tool_use")
                        {
                            // 工具调用转换为computer_call格式
                            contentInput.Type = "computer_call";
                            contentInput.Action = new
                            {
                                type = "function_call",
                                function = new
                                {
                                    name = content.Name,
                                    arguments = System.Text.Json.JsonSerializer.Serialize(content.Input)
                                }
                            };
                            contentInput.CallId = content.Id;
                            contentInput.Id = content.Id ?? Guid.NewGuid().ToString();
                            contentInput.PendingSafetyChecks = new object[0]; // 空的安全检查数组
                        }
                        else if (content.Type == "tool_result")
                        {
                            // 工具结果转换为computer_call_output格式
                            contentInput.Type = "computer_call_output";
                            contentInput.CallId = content.ToolUseId;
                            contentInput.Output = new
                            {
                                type = "function_output",
                                content = content.Content?.ToString() ?? string.Empty,
                                status = "completed"
                            };
                        }

                        // 如果所有内容都是空则不添加
                        if (string.IsNullOrEmpty(contentInput.Text) && string.IsNullOrEmpty(contentInput.ImageUrl) &&
                            string.IsNullOrEmpty(contentInput.Type))
                        {
                            continue;
                        }

                        convertedContents.Add(contentInput);
                    }

                    responseMessage.Contents = convertedContents;
                }

                inputMessages.Add(responseMessage);
            }
        }

        // 设置输入消息列表
        openAiRequest.Inputs = inputMessages;

        // 转换工具
        if (input.Tools?.Count > 0)
        {
            openAiRequest.Tools = input.Tools.Select(AnthropicMessageToolToResponsesToolsInput).ToList();
        }

        // 转换工具选择
        if (input.Tools?.Count > 0)
        {
            openAiRequest.ToolChoice = "auto";
        }

        openAiRequest.ParallelToolCalls = true; // 支持并行工具调用
        openAiRequest.Store = false; // 默认不存储

        // 如果需要支持会话持续性，可以设置previous_response_id
        // 这需要从上下文或缓存中获取上一个响应的ID
        // openAIRequest.PreviousResponseId = GetPreviousResponseId(input);

        return openAiRequest;
    }

    private ResponsesToolsInput AnthropicMessageToolToResponsesToolsInput(AnthropicMessageTool tool)
    {
        var input = new ResponsesToolsInput()
        {
            Type = "function",
            Description = tool.Description,
            Name = tool.name,
        };

        if (tool.InputSchema != null)
        {
            input.Parameters = new ThorToolFunctionPropertyDefinition
            {
                Required = tool.InputSchema.Required?.ToArray() ?? [],
                Type = "object",
                Properties = new ConcurrentDictionary<string, ThorToolFunctionPropertyDefinition>()
            };
            if (tool.InputSchema.Properties != null)
            {
                foreach (var schemaValue in tool.InputSchema.Properties)
                {
                    var definition = new ThorToolFunctionPropertyDefinition
                    {
                        Description = schemaValue.Value.description,
                        Type = schemaValue.Value.type,
                        Properties = new ConcurrentDictionary<string, ThorToolFunctionPropertyDefinition>()
                    };

                    if (schemaValue.Value.type == "array" && schemaValue.Value.items != null)
                    {
                        definition.Items = new ThorToolFunctionPropertyDefinition
                        {
                            Type = schemaValue.Value.items.type
                        };
                    }

                    input.Parameters.Properties.Add(schemaValue.Key, definition);

                    // 套娃
                }
            }
        }

        return input;
    }

    private ClaudeChatCompletionDto ConvertOpenAIToClaude(ResponsesDto openAIResponse, AnthropicInput input)
    {
        var claudeResponse = new ClaudeChatCompletionDto
        {
            id = openAIResponse.Id ?? Guid.NewGuid().ToString(),
            type = "message",
            role = "assistant",
            model = openAIResponse.Model ?? input.Model,
            stop_reason = GetClaudeStopReason(openAIResponse.Status),
            stop_sequence = null
        };

        // 转换内容
        var contentList = new List<ClaudeChatCompletionDtoContent>();

        // 处理输出内容
        if (openAIResponse.Output?.Length > 0)
        {
            foreach (var outputItem in openAIResponse.Output)
            {
                // 处理角色为assistant的输出
                if (outputItem.Role == "assistant" && outputItem.Content?.Length > 0)
                {
                    foreach (var contentItem in outputItem.Content)
                    {
                        var content = new ClaudeChatCompletionDtoContent();

                        // 处理文本内容
                        if (contentItem.Type == "text")
                        {
                            content.type = "text";
                            content.text = contentItem.Text;
                            contentList.Add(content);
                        }
                    }
                }
                // 处理推理内容 (reasoning -> thinking)
                else if (outputItem.Type == "reasoning")
                {
                    var content = new ClaudeChatCompletionDtoContent
                    {
                        type = "thinking",
                        Thinking = ExtractReasoningContent(openAIResponse.Reasoning)
                    };

                    // 处理加密的推理内容 - OpenAI Responses API 2025新特性
                    if (!string.IsNullOrEmpty(outputItem.EncryptedContent))
                    {
                        logger.LogDebug("Found encrypted reasoning content: {Length} characters",
                            outputItem.EncryptedContent.Length);
                        // 加密内容目前无法直接解密，但可以记录其存在
                        content.signature = outputItem.EncryptedContent; // 使用signature字段存储加密内容
                    }

                    contentList.Add(content);
                }
                // 处理工具调用
                else if (outputItem.Type == "tool_calls" || outputItem.Type == "function")
                {
                    var content = new ClaudeChatCompletionDtoContent
                    {
                        type = "tool_use",
                        id = outputItem.CallId ?? Guid.NewGuid().ToString(),
                        name = outputItem.Name
                    };

                    // 解析工具参数
                    if (!string.IsNullOrEmpty(outputItem.Arguments))
                    {
                        try
                        {
                            content.input = System.Text.Json.JsonSerializer.Deserialize<object>(
                                outputItem.Arguments, ThorJsonSerializer.DefaultOptions);
                        }
                        catch
                        {
                            // 如果解析失败，将原始字符串作为输入
                            content.input = outputItem.Arguments;
                        }
                    }

                    contentList.Add(content);
                }
            }
        }

        // 如果没有内容，添加默认文本内容
        if (contentList.Count == 0 && !string.IsNullOrEmpty(openAIResponse.OutputText))
        {
            contentList.Add(new ClaudeChatCompletionDtoContent
            {
                type = "text",
                text = openAIResponse.OutputText
            });
        }

        claudeResponse.content = contentList.ToArray();

        // 转换使用统计
        claudeResponse.Usage = new ClaudeChatCompletionDtoUsage();
        if (openAIResponse.Usage != null)
        {
            claudeResponse.Usage.input_tokens = openAIResponse.Usage.InputTokens;
            claudeResponse.Usage.output_tokens = openAIResponse.Usage.OutputTokens;

            // 转换缓存相关的token统计
            if (openAIResponse.Usage.InputTokensDetails?.CachedTokens > 0)
            {
                claudeResponse.Usage.cache_read_input_tokens = openAIResponse.Usage.InputTokensDetails.CachedTokens;
            }

            // 处理reasoning tokens - OpenAI Responses API 2025新特性
            if (openAIResponse.Usage.OutputTokensDetails?.ReasoningTokens > 0)
            {
                logger.LogDebug("OpenAI Reasoning Tokens in response: {ReasoningTokens}",
                    openAIResponse.Usage.OutputTokensDetails.ReasoningTokens);
                // Claude不直接支持reasoning tokens字段，已包含在output_tokens中
            }
        }

        return claudeResponse;
    }

    /// <summary>
    /// 从推理对象中提取推理内容
    /// </summary>
    private string? ExtractReasoningContent(object? reasoning)
    {
        if (reasoning == null) return null;

        // 如果是字符串直接返回
        if (reasoning is string reasoningStr)
            return reasoningStr;

        // 如果是对象，尝试提取文本内容
        if (reasoning is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString();

            // 尝试提取常见的推理内容字段
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("content", out var contentProp))
                    return contentProp.GetString();
                if (element.TryGetProperty("text", out var textProp))
                    return textProp.GetString();
                if (element.TryGetProperty("reasoning", out var reasoningProp))
                    return reasoningProp.GetString();
            }
        }

        // 最后尝试序列化整个对象
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(reasoning, ThorJsonSerializer.DefaultOptions);
        }
        catch
        {
            return reasoning?.ToString();
        }
    }

    /// <summary>
    /// 将OpenAI的状态转换为Claude的停止原因
    /// </summary>
    private string GetClaudeStopReason(string? openAIStatus)
    {
        return openAIStatus switch
        {
            "completed" => "end_turn",
            "failed" => "max_tokens",
            "incomplete" => "max_tokens",
            "tool_calls" => "tool_use",
            _ => "end_turn"
        };
    }
}