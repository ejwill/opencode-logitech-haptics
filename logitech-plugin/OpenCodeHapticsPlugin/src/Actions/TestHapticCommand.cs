namespace Loupedeck.OpenCodeHapticsPlugin
{
    using System;

    public class TestHapticCommand : PluginDynamicCommand
    {
        public TestHapticCommand()
            : base(displayName: "Test OpenCode Haptic", description: "Triggers the OpenCode test haptic event", groupName: "Haptics")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            this.Plugin.PluginEvents.RaiseEvent(OpenCodeHapticEvents.Test);
            PluginLog.Info("Raised OpenCode test haptic event");
        }
    }
}
