namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;

    public class TestHapticCommand : PluginDynamicCommand
    {
        public TestHapticCommand()
            : base(displayName: "Test Haptic Feedback", description: "Send a test vibration to confirm Logitech haptics are working", groupName: "OpenCode Companion###Diagnostics")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            this.Plugin.PluginEvents.RaiseEvent(OpenCodeHapticEvents.Test);
            PluginLog.Info("Raised OpenCode test haptic event");
        }
    }
}
