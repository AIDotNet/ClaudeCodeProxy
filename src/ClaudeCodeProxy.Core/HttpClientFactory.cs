using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using ClaudeCodeProxy.Domain;

namespace ClaudeCodeProxy.Core;

public static class HttpClientFactory
{
    /// <summary>
    /// HttpClient池总数
    /// </summary>
    /// <returns></returns>
    private static int _poolSize;

    private static int PoolSize
    {
        get
        {
            if (_poolSize == 0)
            {
                // 获取环境变量
                var poolSize = Environment.GetEnvironmentVariable("HttpClientPoolSize");
                if (!string.IsNullOrEmpty(poolSize) && int.TryParse(poolSize, out var size))
                {
                    _poolSize = size;
                }
                else
                {
                    _poolSize = 5; // 默认池大小
                }

                if (_poolSize < 1)
                {
                    _poolSize = 2;
                }
            }

            return _poolSize;
        }
    }

    private static readonly ConcurrentDictionary<string, Lazy<List<HttpClient>>> HttpClientPool = new();

    public static HttpClient GetHttpClient(string key, ProxyConfig? config)
    {
        return HttpClientPool.GetOrAdd(key, k => new Lazy<List<HttpClient>>(() =>
        {
            // 创建好代理
            var proxy = new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate |
                                         DecompressionMethods.Brotli,
                UseDefaultCredentials = false,
                PreAuthenticate = false,
                UseCookies = true,
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true,
                MaxAutomaticRedirections = 3,
                UseProxy = true,
            };
            if (config != null && !string.IsNullOrEmpty(config.Host) && config.Port > 0)
            {
                proxy.Proxy = new WebProxy(config.Host, config.Port)
                {
                    BypassProxyOnLocal = true,
                    UseDefaultCredentials = false,
                };
                if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
                {
                    proxy.Proxy.Credentials = new NetworkCredential(config.Username, config.Password);
                }

                proxy.UseProxy = true;
                var clients = new List<HttpClient>(PoolSize);

                for (var i = 0; i < PoolSize; i++)
                {
                    clients.Add(new HttpClient(proxy)
                    {
                        Timeout = TimeSpan.FromMinutes(30)
                    });
                }

                return clients;
            }
            else
            {
                var clients = new List<HttpClient>(PoolSize);

                for (var i = 0; i < PoolSize; i++)
                {
                    clients.Add(new HttpClient(new SocketsHttpHandler()
                    {
                        AllowAutoRedirect = true,
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate |
                                                 DecompressionMethods.Brotli,
                        UseCookies = true,
                        PreAuthenticate = false,
                        // 不要验证ssl
                        SslOptions = new SslClientAuthenticationOptions()
                        {
                            RemoteCertificateValidationCallback = (message, certificate2, arg3, arg4) => true
                        },
                        EnableMultipleHttp2Connections = true,
                        UseProxy = true,
                        MaxAutomaticRedirections = 3
                    })
                    {
                        DefaultRequestVersion = HttpVersion.Version20,
                        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                        Timeout = TimeSpan.FromMinutes(30)
                    });
                }

                return clients;
            }
        })).Value[new Random().Next(0, PoolSize)];
    }
}