using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OAuth2Bridge;

internal sealed class LocalCallbackServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly OAuthLogger _logger;

    public LocalCallbackServer(string redirectUri, OAuthLogger logger)
    {
        _logger = logger;
        _listener = new HttpListener();
        _listener.Prefixes.Add(redirectUri + "/");
    }

    public void Start() => _listener.Start();

    public async Task<HttpListenerContext> WaitForCallbackAsync(CancellationToken ct)
    {
#if NET8_0_OR_GREATER
        return await _listener.GetContextAsync().WaitAsync(ct).ConfigureAwait(false);
#else
        var tcs = new TaskCompletionSource<HttpListenerContext>();
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
        var task = _listener.GetContextAsync();
        var completed = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
        if (completed == tcs.Task) await tcs.Task.ConfigureAwait(false);
        return await task.ConfigureAwait(false);
#endif
    }

    public static async Task WriteSuccessAsync(HttpListenerResponse resp, string html, CancellationToken ct)
    {
        await WriteHtmlAsync(resp, html, 200, ct).ConfigureAwait(false);
    }

    public static async Task WriteHtmlAsync(HttpListenerResponse resp, string html, int status, CancellationToken ct)
    {
        var buf = Encoding.UTF8.GetBytes(html);
        resp.ContentType = "text/html; charset=utf-8";
        resp.StatusCode = status;
        resp.ContentLength64 = buf.Length;
        await resp.OutputStream.WriteAsync(buf, 0, buf.Length, ct).ConfigureAwait(false);
        resp.OutputStream.Close();
    }

    public static async Task WriteErrorAsync(HttpListenerResponse resp, string msg, CancellationToken ct)
    {
        try
        {
            var html = $"<html><body><h1>Error</h1><p>{System.Net.WebUtility.HtmlEncode(msg)}</p></body></html>";
            await WriteHtmlAsync(resp, html, 400, ct).ConfigureAwait(false);
        }
        catch { }
    }

    public void Stop()
    {
        try { if (_listener.IsListening) _listener.Stop(); _listener.Close(); } catch { }
    }

    public void Dispose() => Stop();
}