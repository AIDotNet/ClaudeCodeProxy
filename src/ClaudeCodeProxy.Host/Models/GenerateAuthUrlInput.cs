using ClaudeCodeProxy.Domain;

namespace ClaudeCodeProxy.Host.Models;

public class GenerateAuthUrlInput
{
    public ProxyConfig Proxy { get; set; }
}