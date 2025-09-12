using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using ClaudeCodeProxy.Domain;

namespace ClaudeCodeProxy.Core;

public static class HttpClientFactory
{
    /// <summary>
    ///     HttpClient池总数
    /// </summary>
    /// <returns></returns>
    private static int _poolSize;

    private static readonly ConcurrentDictionary<string, Lazy<List<HttpClient>>> HttpClientPool = new();

    private static int PoolSize
    {
        get
        {
            if (_poolSize == 0)
            {
                // 获取环境变量
                var poolSize = Environment.GetEnvironmentVariable("HttpClientPoolSize");
                if (!string.IsNullOrEmpty(poolSize) && int.TryParse(poolSize, out var size))
                    _poolSize = size;
                else
                    _poolSize = 5; // 默认池大小

                if (_poolSize < 1) _poolSize = 2;
            }

            return _poolSize;
        }
    }

    public static HttpClient GetHttpClient(string key, ProxyConfig? proxyConfig)
    {
        return HttpClientPool.GetOrAdd(key, k => new Lazy<List<HttpClient>>(() =>
        {
            // 创建好代理
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate |
                                         DecompressionMethods.Brotli,
                UseDefaultCredentials = false,
                PreAuthenticate = false,
                UseCookies = true,
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true,
                MaxAutomaticRedirections = 3,
                UseProxy = true
            };

            if (proxyConfig != null && !string.IsNullOrEmpty(proxyConfig.Host) && proxyConfig.Port > 0)
            {

                var proxyUri = $"{proxyConfig.Type}://{proxyConfig.Host}:{proxyConfig.Port}";

                if (!string.IsNullOrEmpty(proxyConfig.Username) && !string.IsNullOrEmpty(proxyConfig.Password))
                    proxyUri =
                        $"{proxyConfig.Type}://{proxyConfig.Username}:{proxyConfig.Password}@{proxyConfig.Host}:{proxyConfig.Port}";

                handler.Proxy = new WebProxy(proxyUri);
                handler.UseProxy = true;

                var clients = new List<HttpClient>(PoolSize);

                for (var i = 0; i < PoolSize; i++)
                    clients.Add(new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromMinutes(30)
                    });

                return clients;
            }
            else
            {
                var clients = new List<HttpClient>(PoolSize);

                for (var i = 0; i < PoolSize; i++)
                    clients.Add(new HttpClient(new SocketsHttpHandler
                    {
                        AllowAutoRedirect = true,
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate |
                                                 DecompressionMethods.Brotli,
                        UseCookies = true,
                        PreAuthenticate = false,
                        // 不要验证ssl
                        SslOptions = new SslClientAuthenticationOptions
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

                return clients;
            }
        })).Value[new Random().Next(0, PoolSize)];
    }
}