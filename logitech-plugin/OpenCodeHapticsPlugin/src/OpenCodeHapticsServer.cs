namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class OpenCodeCompanionServer : IDisposable
    {
        private const Int32 MaxRequestBodyBytes = 16 * 1024;
        private readonly HttpListener _listener = new();
        private readonly Action<String, String, String> _raiseHapticEvent;
        private readonly Action<String> _reportServerUrl;
        private readonly Object _lifecycleLock = new();
        private CancellationTokenSource _cancellation;
        private Task _serveTask;
        private Boolean _disposed;

        public OpenCodeCompanionServer(Int32 port, Action<String, String, String> raiseHapticEvent)
            : this(port, raiseHapticEvent, null) { }

        public OpenCodeCompanionServer(Int32 port, Action<String, String, String> raiseHapticEvent, Action<String> reportServerUrl)
        {
            this.Port = port;
            this._raiseHapticEvent = raiseHapticEvent ?? throw new ArgumentNullException(nameof(raiseHapticEvent));
            this._reportServerUrl = reportServerUrl;
            this._listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        }

        public Int32 Port { get; }

        public Boolean Start()
        {
            lock (this._lifecycleLock)
            {
                if (this._disposed) throw new ObjectDisposedException(nameof(OpenCodeCompanionServer));
                if (this._serveTask != null) return true;
                var cancellation = new CancellationTokenSource();
                try
                {
                    this._listener.Start();
                    this._cancellation = cancellation;
                    this._serveTask = Task.Run(() => this.ServeAsync(cancellation.Token));
                    PluginLog.Info($"OpenCode haptics listener started on http://127.0.0.1:{this.Port}/haptic");
                    return true;
                }
                catch (Exception ex)
                {
                    cancellation.Dispose();
                    PluginLog.Error($"OpenCode haptics listener could not start on port {this.Port}: {ex}");
                    return false;
                }
            }
        }

        public void Stop()
        {
            Task serveTask;
            CancellationTokenSource cancellation;
            lock (this._lifecycleLock)
            {
                serveTask = this._serveTask;
                cancellation = this._cancellation;
                this._serveTask = null;
                this._cancellation = null;
                try
                {
                    cancellation?.Cancel();
                    if (this._listener.IsListening) this._listener.Stop();
                }
                catch (Exception ex) { PluginLog.Error($"Error while stopping OpenCode haptics listener: {ex}"); }
            }
            try { serveTask?.GetAwaiter().GetResult(); }
            catch (Exception ex) { PluginLog.Error($"OpenCode haptics listener did not stop cleanly: {ex}"); }
            finally { cancellation?.Dispose(); }
        }

        private async Task ServeAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && this._listener.IsListening)
            {
                try
                {
                    var context = await this._listener.GetContextAsync().ConfigureAwait(false);
                    _ = this.HandleAsync(context);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested) { return; }
                catch (ObjectDisposedException) { return; }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested) PluginLog.Error($"OpenCode haptics listener error: {ex}");
                }
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            try
            {
                if (!String.Equals(context.Request.Url?.AbsolutePath, "/haptic", StringComparison.OrdinalIgnoreCase))
                {
                    await this.RespondAsync(context, 404, "not found").ConfigureAwait(false);
                    return;
                }
                if (!String.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers["Allow"] = "POST";
                    await this.RespondAsync(context, 405, "method not allowed").ConfigureAwait(false);
                    return;
                }
                if (context.Request.ContentLength64 > MaxRequestBodyBytes)
                {
                    await this.RespondAsync(context, 413, "request too large").ConfigureAwait(false);
                    return;
                }

                var body = await ReadBodyAsync(context.Request.InputStream).ConfigureAwait(false);
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    await this.RespondAsync(context, 400, "missing event").ConfigureAwait(false);
                    return;
                }
                if (document.RootElement.TryGetProperty("type", out var typeProperty)
                    && typeProperty.ValueKind == JsonValueKind.String
                    && String.Equals(typeProperty.GetString(), "server_info", StringComparison.OrdinalIgnoreCase))
                {
                    if (!document.RootElement.TryGetProperty("serverUrl", out var serverUrlProperty)
                        || serverUrlProperty.ValueKind != JsonValueKind.String
                        || !TryGetLoopbackUrl(serverUrlProperty.GetString(), out var serverUrl))
                    {
                        await this.RespondAsync(context, 400, "invalid server url").ConfigureAwait(false);
                        return;
                    }
                    this._reportServerUrl?.Invoke(serverUrl);
                    await this.RespondAsync(context, 202, "server info accepted").ConfigureAwait(false);
                    return;
                }
                if (!document.RootElement.TryGetProperty("event", out var eventProperty))
                {
                    await this.RespondAsync(context, 400, "missing event").ConfigureAwait(false);
                    return;
                }
                if (eventProperty.ValueKind != JsonValueKind.String || String.IsNullOrWhiteSpace(eventProperty.GetString()))
                {
                    await this.RespondAsync(context, 400, "invalid event").ConfigureAwait(false);
                    return;
                }
                var openCodeEvent = eventProperty.GetString();
                var waveform = document.RootElement.TryGetProperty("waveform", out var waveformProperty)
                    && waveformProperty.ValueKind == JsonValueKind.String
                    ? waveformProperty.GetString()
                    : null;
                var sessionID = document.RootElement.TryGetProperty("sessionID", out var sessionProperty)
                    && sessionProperty.ValueKind == JsonValueKind.String
                    ? sessionProperty.GetString()
                    : null;
                if (!OpenCodeHapticEvents.FromOpenCodeEvent.ContainsKey(openCodeEvent))
                {
                    await this.RespondAsync(context, 204, String.Empty).ConfigureAwait(false);
                    return;
                }
                this._raiseHapticEvent(openCodeEvent, waveform, sessionID);
                await this.RespondAsync(context, 202, "accepted").ConfigureAwait(false);
            }
            catch (JsonException) { await this.TryRespondAsync(context, 400, "invalid json").ConfigureAwait(false); }
            catch (InvalidDataException) { await this.TryRespondAsync(context, 413, "request too large").ConfigureAwait(false); }
            catch (Exception ex)
            {
                PluginLog.Error($"OpenCode haptics request failed: {ex}");
                await this.TryRespondAsync(context, 500, "internal error").ConfigureAwait(false);
            }
        }

        private static Boolean TryGetLoopbackUrl(String value, out String serverUrl)
        {
            serverUrl = null;
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || !String.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || !(String.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase))) return false;
            serverUrl = uri.ToString().TrimEnd('/');
            return true;
        }

        private static async Task<String> ReadBodyAsync(Stream input)
        {
            using var output = new MemoryStream();
            var buffer = new Byte[4096];
            Int32 read;
            while ((read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                if (output.Length + read > MaxRequestBodyBytes) throw new InvalidDataException("Request body exceeds limit.");
                await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);
            }
            return Encoding.UTF8.GetString(output.ToArray());
        }

        private async Task TryRespondAsync(HttpListenerContext context, Int32 statusCode, String body)
        {
            try { await this.RespondAsync(context, statusCode, body).ConfigureAwait(false); }
            catch (Exception responseError) { PluginLog.Error($"OpenCode haptics response failed: {responseError}"); }
        }

        private async Task RespondAsync(HttpListenerContext context, Int32 statusCode, String body)
        {
            var bytes = Encoding.UTF8.GetBytes(body ?? String.Empty);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            if (bytes.Length > 0) await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            context.Response.Close();
        }

        public void Dispose()
        {
            if (this._disposed) return;
            this.Stop();
            this._listener.Close();
            this._disposed = true;
        }
    }
}
