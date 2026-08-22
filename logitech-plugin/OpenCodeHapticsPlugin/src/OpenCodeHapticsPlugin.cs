namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;

    public class OpenCodeCompanionPlugin : Plugin
    {
        private const Int32 DefaultPort = 17844;
        private OpenCodeCompanionServer _server;

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
            var server = new OpenCodeCompanionServer(DefaultPort, this.RaiseHapticEvent);
            if (server.Start()) this._server = server;
            else server.Dispose();
        }

        public override void Unload()
        {
            this.ActionEditorCommands.Clear();
            this._server?.Dispose();
            this._server = null;
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

        internal Boolean IsHapticEventEnabled(String hapticEvent)
        {
            var settingName = HapticPreferenceSettings.GetSettingName(hapticEvent);
            return !this.TryGetPluginSetting(settingName, out var value)
                || !Boolean.TryParse(value, out var enabled)
                || enabled;
        }

        internal void SetHapticEventEnabled(String hapticEvent, Boolean enabled)
        {
            this.SetPluginSetting(HapticPreferenceSettings.GetSettingName(hapticEvent), enabled.ToString(), false);
        }
    }
}
