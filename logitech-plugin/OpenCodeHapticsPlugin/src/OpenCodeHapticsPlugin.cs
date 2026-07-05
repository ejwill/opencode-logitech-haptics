namespace Loupedeck.OpenCodeHapticsPlugin
{
    using System;

    public class OpenCodeHapticsPlugin : Plugin
    {
        private const Int32 DefaultPort = 17844;
        private OpenCodeHapticsServer _server;

        public override Boolean UsesApplicationApiOnly => true;
        public override Boolean HasNoApplication => true;

        public OpenCodeHapticsPlugin()
        {
            PluginLog.Init(this.Log);
            PluginResources.Init(this.Assembly);
        }

        public override void Load()
        {
            this.RegisterHapticEvents();
            this._server = new OpenCodeHapticsServer(DefaultPort, this.RaiseHapticEvent);
            this._server.Start();
        }

        public override void Unload()
        {
            this._server?.Dispose();
            this._server = null;
        }

        private void RegisterHapticEvents()
        {
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Complete, "OpenCode Complete", "OpenCode session completed");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Permission, "OpenCode Permission", "OpenCode is asking for permission");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Error, "OpenCode Error", "OpenCode reported an error");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Question, "OpenCode Question", "OpenCode has a question");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.PlanExit, "OpenCode Plan Ready", "OpenCode plan is ready for review");
            this.PluginEvents.AddEvent(OpenCodeHapticEvents.Test, "OpenCode Test", "Manual OpenCode haptic test");
        }

        private void RaiseHapticEvent(String hapticEvent)
        {
            this.PluginEvents.RaiseEvent(hapticEvent);
            PluginLog.Info($"Raised haptic event: {hapticEvent}");
        }
    }
}
