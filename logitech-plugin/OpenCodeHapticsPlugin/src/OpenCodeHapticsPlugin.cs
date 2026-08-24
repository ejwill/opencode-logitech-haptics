namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;

    public class OpenCodeCompanionPlugin : Plugin
    {
        private const Int32 DefaultPort = 17844;
        private OpenCodeCompanionServer _server;
        internal OpenCodeControlClient OpenCodeControls { get; } = new();
        private String _lastOpenCodeSessionID;

        public override Boolean UsesApplicationApiOnly => true;
        public override Boolean HasNoApplication => true;

        public OpenCodeCompanionPlugin()
        {
            PluginLog.Init(this.Log);
            PluginResources.Init(this.Assembly);
        }

        public override void Load()
        {
            this.RegisterHapticEvents();
            this.ActionEditorCommands.AddAction(new HapticPreferencesCommand());
            var server = new OpenCodeCompanionServer(DefaultPort, this.HandleOpenCodeEvent, this.ReportOpenCodeServerUrl);
            if (server.Start()) this._server = server;
            else server.Dispose();
        }

        public override void Unload()
        {
            this.ActionEditorCommands.Clear();
            this._server?.Dispose();
            this._server = null;
            this._lastOpenCodeSessionID = null;
        }

        private void RegisterHapticEvents()
        {
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Complete, "Turn Complete", "OpenCode finished a turn");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Permission, "Permission Request", "OpenCode needs your approval");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Error, "Turn Error", "OpenCode encountered an error");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Question, "Question Awaiting Answer", "OpenCode is waiting for your answer");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.PlanExit, "Plan Ready", "OpenCode has a plan ready for review");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Test, "Test Haptic Feedback", "Confirm that your Logitech device can vibrate");
        }

        private void RaiseHapticEvent(String hapticEvent)
        {
            if (!this.IsHapticEventEnabled(hapticEvent))
            {
                PluginLog.Info($"Skipped disabled haptic event: {hapticEvent}");
                return;
            }

            this.PluginEvents.RaiseEvent(hapticEvent);
            PluginLog.Info($"Raised haptic event: {hapticEvent}");
        }

        private void HandleOpenCodeEvent(String openCodeEvent, String requestedWaveform, String sessionID)
        {
            if (!OpenCodeHapticEvents.FromOpenCodeEvent.TryGetValue(openCodeEvent, out var baseEvent)) return;
            if (!String.IsNullOrWhiteSpace(sessionID)) this._lastOpenCodeSessionID = sessionID;
            var waveform = this.GetHapticWaveformOverride(baseEvent)
                ?? (this.GetHapticIntensityOverride(baseEvent) is String intensity ? HapticIntensityProfiles.GetWaveform(intensity, baseEvent) : null)
                ?? requestedWaveform;
            this.RaiseHapticEvent(OpenCodeHapticEvents.Resolve(openCodeEvent, waveform));
        }

        private void ReportOpenCodeServerUrl(String serverUrl)
        {
            this.OpenCodeControls.SetServerUrl(serverUrl);
            PluginLog.Info($"OpenCode server URL received: {serverUrl}");
        }

        internal String GetLastOpenCodeSessionID() => this._lastOpenCodeSessionID;

        internal Boolean IsHapticEventEnabled(String hapticEvent)
        {
            var settingName = HapticPreferenceSettings.GetSettingName(OpenCodeHapticEvents.GetBaseEvent(hapticEvent));
            return !this.TryGetPluginSetting(settingName, out var value)
                || !Boolean.TryParse(value, out var enabled)
                || enabled;
        }

        internal void SetHapticEventEnabled(String hapticEvent, Boolean enabled)
        {
            this.SetPluginSetting(HapticPreferenceSettings.GetSettingName(OpenCodeHapticEvents.GetBaseEvent(hapticEvent)), enabled.ToString(), false);
        }

        internal String GetHapticWaveform(String hapticEvent, String fallback)
        {
            var baseEvent = OpenCodeHapticEvents.GetBaseEvent(hapticEvent);
            return this.GetHapticWaveformOverride(baseEvent) ?? fallback;
        }

        internal String GetHapticIntensity(String fallback) => this.GetHapticIntensityOverride() ?? fallback;

        internal String GetHapticIntensity(String hapticEvent, String fallback) => this.GetHapticIntensityOverride(hapticEvent) ?? fallback;

        internal void SetHapticIntensity(String intensity)
        {
            if (HapticIntensityProfiles.IsSupported(intensity)) this.SetPluginSetting(HapticPreferenceSettings.Intensity, intensity, false);
        }

        internal void SetHapticIntensity(String hapticEvent, String intensity)
        {
            var baseEvent = OpenCodeHapticEvents.GetBaseEvent(hapticEvent);
            if (HapticIntensityProfiles.IsSupported(intensity)) this.SetPluginSetting(HapticPreferenceSettings.GetIntensitySettingName(baseEvent), intensity, false);
        }

        private String GetHapticIntensityOverride() =>
            this.TryGetPluginSetting(HapticPreferenceSettings.Intensity, out var value) && HapticIntensityProfiles.IsSupported(value) ? value : null;

        private String GetHapticIntensityOverride(String hapticEvent)
        {
            var baseEvent = OpenCodeHapticEvents.GetBaseEvent(hapticEvent);
            return this.TryGetPluginSetting(HapticPreferenceSettings.GetIntensitySettingName(baseEvent), out var eventValue)
                && HapticIntensityProfiles.IsSupported(eventValue) ? eventValue : this.GetHapticIntensityOverride();
        }

        private String GetHapticWaveformOverride(String hapticEvent)
        {
            var baseEvent = OpenCodeHapticEvents.GetBaseEvent(hapticEvent);
            return this.TryGetPluginSetting(HapticPreferenceSettings.GetWaveformSettingName(baseEvent), out var value)
                && HapticWaveforms.IsSupported(value) ? value : null;
        }

        internal void SetHapticWaveform(String hapticEvent, String waveform)
        {
            var baseEvent = OpenCodeHapticEvents.GetBaseEvent(hapticEvent);
            if (HapticWaveforms.IsSupported(waveform)) this.SetPluginSetting(HapticPreferenceSettings.GetWaveformSettingName(baseEvent), waveform, false);
        }
    }
}
