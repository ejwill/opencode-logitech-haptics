namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal sealed class OpenCodeControlClient
    {
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(2) };
        private String _serverUrl;

        public OpenCodeControlClient()
        {
            this._serverUrl = (Environment.GetEnvironmentVariable("OPENCODE_SERVER_URL") ?? "http://127.0.0.1:4096").TrimEnd('/');
        }

        public void SetServerUrl(String serverUrl)
        {
            if (!String.IsNullOrWhiteSpace(serverUrl)) this._serverUrl = serverUrl.TrimEnd('/');
        }

        public Task<Boolean> OpenSessionSelectorAsync() => this.PostAsync("/tui/open-sessions");

        public Task<Boolean> OpenModelSelectorAsync() => this.PostAsync("/tui/open-models");

        public Task<Boolean> CycleAgentAsync() => this.ExecuteCommandAsync("agent_cycle");

        public Task<Boolean> CycleThinkingLevelAsync() => this.ExecuteCommandAsync("variant_cycle");

        public Task<Boolean> AbortSessionAsync(String sessionID) =>
            String.IsNullOrWhiteSpace(sessionID) ? Task.FromResult(false) : this.PostAsync($"/session/{Uri.EscapeDataString(sessionID)}/abort");

        private async Task<Boolean> PostAsync(String path)
        {
            try
            {
                using var response = await Client.PostAsync(this._serverUrl + path, new StringContent("{}", Encoding.UTF8, "application/json")).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"OpenCode control request failed for {path}: {ex.Message}");
                return false;
            }
        }

        private async Task<Boolean> ExecuteCommandAsync(String command)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { command });
                using var response = await Client.PostAsync(
                    this._serverUrl + "/tui/execute-command",
                    new StringContent(body, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"OpenCode TUI command failed for {command}: {ex.Message}");
                return false;
            }
        }
    }
}
