﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeCodeProxy.Abstraction;

public static class ThorJsonSerializer
{
    public static JsonSerializerOptions DefaultOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}