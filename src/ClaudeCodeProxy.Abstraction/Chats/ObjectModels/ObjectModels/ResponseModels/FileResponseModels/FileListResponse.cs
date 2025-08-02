﻿using System.Text.Json.Serialization;
using Thor.Abstractions.Dtos;
using Thor.Abstractions.ObjectModels.ObjectModels.SharedModels;

namespace Thor.Abstractions.ObjectModels.ObjectModels.ResponseModels.FileResponseModels;

public record FileListResponse : ThorBaseResponse
{
    [JsonPropertyName("data")] public List<FileResponse> Data { get; set; }
}