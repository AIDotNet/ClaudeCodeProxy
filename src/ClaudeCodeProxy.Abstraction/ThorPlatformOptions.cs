namespace Thor.Abstractions;

/// <summary>
///     对话平台选项
/// </summary>
public class ThorPlatformOptions
{
    public ThorPlatformOptions()
    {
    }

    public ThorPlatformOptions(string address, string apiKey, string? other = null)
    {
        Address = address;
        ApiKey = apiKey;
        Other = other;
    }

    /// <summary>
    ///     对话平台基地址
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    ///     对话平台的秘钥，如果是多个参数，则通过 | 分隔，如 ApiKey|SecretKey，然后在具体平台实现里面做解析
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    ///     额外的平台参数信息，如果有多个参数，则通过 | 分隔，然后在具体平台实现里面做解析
    /// </summary>
    public string Other { get; set; }
}