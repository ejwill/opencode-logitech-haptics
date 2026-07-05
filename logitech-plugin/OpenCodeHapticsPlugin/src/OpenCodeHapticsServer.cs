namespace Loupedeck.OpenCodeHapticsPlugin
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class OpenCodeHapticsServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly Action<String> _raiseHapticEvent;
        private CancellationTokenSource _cancellation;
        private Task _serveTask;

        public OpenCodeHapticsServer(Int32 port, Action<String> raiseHapticEvent)
        {
            this.Port = port;
            this._raiseHapticEvent = raiseHapticEvent ?? throw new ArgumentNullException(nameof(raiseHapticEvent));
            this._listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        }

        public Int32 Port { get; }

        public void Start()
        {
            if (this._serveTask != null) return;
            this._cancellation = new CancellationTokenSource();
            this._listener.Start();
            this._serveTask = Task.Run(() => this.ServeAsync(this._cancellation.Token));
            PluginLog.Info($"OpenCode haptics listener started on http://127.0.0.1:{this.Port}/haptic");
        }

        public void Stop()
        {
            try
            {
                this._cancellation?.Cancel();
                if (this._listener.IsListening) this._listener.Stop();
            }
            catch (Exception ex) { PluginLog.Error($"Error while stopping OpenCode haptics listener: {ex}"); }
            finally
            {
                this._cancellation?.Dispose();
                this._cancellation = null;
                this._serveTask = null;
            }
        }

        private async Task ServeAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && this._listener.IsListening)
            {
                try
                {
                    var context = await this._listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => this.HandleAsync(context), cancellationToken);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested) { return; }
                catch (ObjectDisposedException) { return; }
                catch (Exception ex) { PluginLog.Error($"OpenCode haptics listener error: {ex}"); }
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            if (!String.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !String.Equals(context.Request.Url?.AbsolutePath, "/haptic", StringComparison.OrdinalIgnoreCase))
            {
                await this.RespondAsync(context, 404, "not found").ConfigureAwait(false);
                return;
            }
            try
            {
                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
                var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                using var document = JsonDocument.Parse(body);
                if (!document.RootElement.TryGetProperty("event", out var eventProperty))
                {
                    await this.RespondAsync(context, 400, "missing event").ConfigureAwait(false);
                    return;
                }
                var openCodeEvent = eventProperty.GetString();
                if (String.IsNullOrWhiteSpace(openCodeEvent) || !OpenCodeHapticEvents.FromOpenCodeEvent.TryGetValue(openCodeEvent, out var hapticEvent))
                {
                    await this.RespondAsync(context, 204, String.Empty).ConfigureAwait(false);
                    return;
                }
                this._raiseHapticEvent(hapticEvent);
                await this.RespondAsync(context, 202, "accepted").ConfigureAwait(false);
            }
            catch (JsonException) { await this.RespondAsync(context, 400, "invalid json").ConfigureAwait(false); }
            catch (Exception ex)
            {
                PluginLog.Error($"OpenCode haptics request failed: {ex}");
                await this.RespondAsync(context, 500, "internal error").ConfigureAwait(false);
            }
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

        public void Dispose() => this.Stop();
    }
}
