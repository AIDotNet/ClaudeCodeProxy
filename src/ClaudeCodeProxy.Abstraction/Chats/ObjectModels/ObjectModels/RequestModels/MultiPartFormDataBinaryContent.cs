using System.ClientModel;
using System.Globalization;
using System.Net.Http.Headers;

namespace Thor.Abstractions.ObjectModels.ObjectModels.RequestModels;

public class MultiPartFormDataBinaryContent : BinaryContent
{
    private static readonly Random _random = new();

    private static readonly char[] _boundaryValues =
        "0123456789=ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz".ToCharArray();

    private readonly MultipartFormDataContent _multipartContent;

    public MultiPartFormDataBinaryContent()
    {
        _multipartContent = new MultipartFormDataContent(CreateBoundary());
    }

    public string ContentType => _multipartContent.Headers.ContentType.ToString();

    public HttpContent HttpContent => _multipartContent;

    private static string CreateBoundary()
    {
        Span<char> chars = new char[70];
        var random = new byte[70];
        _random.NextBytes(random);
        var mask = 255 >> 2;
        var i = 0;
        for (; i < 70; i++) chars[i] = _boundaryValues[random[i] & mask];

        return chars.ToString();
    }

    public void Add(string content, string name, string filename = default, string contentType = default)
    {
        Add(new StringContent(content), name, filename, contentType);
    }

    public void Add(int content, string name, string filename = default, string contentType = default)
    {
        var value = content.ToString("G", CultureInfo.InvariantCulture);
        Add(new StringContent(value), name, filename, contentType);
    }

    public void Add(long content, string name, string filename = default, string contentType = default)
    {
        var value = content.ToString("G", CultureInfo.InvariantCulture);
        Add(new StringContent(value), name, filename, contentType);
    }

    public void Add(float content, string name, string filename = default, string contentType = default)
    {
        var value = content.ToString("G", CultureInfo.InvariantCulture);
        Add(new StringContent(value), name, filename, contentType);
    }

    public void Add(double content, string name, string filename = default, string contentType = default)
    {
        var value = content.ToString("G", CultureInfo.InvariantCulture);
        Add(new StringContent(value), name, filename, contentType);
    }

    public void Add(decimal content, string name, string filename = default, string contentType = default)
    {
        var value = content.ToString("G", CultureInfo.InvariantCulture);
        Add(new StringContent(value), name, filename, contentType);
    }

    public void Add(bool content, string name, string filename = default, string contentType = default)
    {
        var value = content ? "true" : "false";
        Add(new StringContent(value), name, filename, contentType);
    }

    public void Add(Stream content, string name, string filename = default, string contentType = default)
    {
        Add(new StreamContent(content), name, filename, contentType);
    }

    public void Add(byte[] content, string name, string filename = default, string contentType = default)
    {
        Add(new ByteArrayContent(content), name, filename, contentType);
    }

    public void Add(BinaryData content, string name, string filename = default, string contentType = default)
    {
        Add(new ByteArrayContent(content.ToArray()), name, filename, contentType);
    }

    private void Add(HttpContent content, string name, string filename, string contentType)
    {
        if (contentType != null) AddContentTypeHeader(content, contentType);

        if (filename != null)
            _multipartContent.Add(content, name, filename);
        else
            _multipartContent.Add(content, name);
    }

    public static void AddContentTypeHeader(HttpContent content, string contentType)
    {
        var header = new MediaTypeHeaderValue(contentType);
        content.Headers.ContentType = header;
    }

    public override bool TryComputeLength(out long length)
    {
        if (_multipartContent.Headers.ContentLength is long contentLength)
        {
            length = contentLength;
            return true;
        }

        length = 0;
        return false;
    }

    public override void WriteTo(Stream stream, CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        _multipartContent.CopyTo(stream, default, cancellationToken);
#else
            _multipartContent.CopyToAsync(stream).GetAwaiter().GetResult();
#endif
    }

    public override async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        await _multipartContent.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
#else
            await _multipartContent.CopyToAsync(stream).ConfigureAwait(false);
#endif
    }

    public override void Dispose()
    {
        _multipartContent.Dispose();
    }
}