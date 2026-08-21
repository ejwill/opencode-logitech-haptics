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
            var server = new OpenCodeCompanionServer(DefaultPort, this.RaiseHapticEvent);
            if (server.Start()) this._server = server;
            else server.Dispose();
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
