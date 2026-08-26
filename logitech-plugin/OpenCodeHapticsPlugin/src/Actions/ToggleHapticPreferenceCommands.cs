namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using Loupedeck;

    public abstract class ToggleHapticPreferenceCommand : PluginDynamicCommand
    {
        private readonly String _hapticEvent;

        protected ToggleHapticPreferenceCommand(String hapticEvent, String displayName, String description)
            : base(displayName, description, "OpenCode Companion###Haptics")
        {
            this._hapticEvent = hapticEvent;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.Plugin is not OpenCodeCompanionPlugin plugin) return;

            var enabled = !plugin.IsHapticEventEnabled(this._hapticEvent);
            plugin.SetHapticEventEnabled(this._hapticEvent, enabled);
            PluginLog.Info($"{HapticPreferenceSettings.Labels[this._hapticEvent]} haptics {(enabled ? "enabled" : "disabled")}");
        }
    }

    public sealed class ToggleTurnCompleteHapticsCommand : ToggleHapticPreferenceCommand
    {
        public ToggleTurnCompleteHapticsCommand()
            : base(OpenCodeHapticEvents.Complete, "Turn Complete Haptics", "Enable or disable vibration when OpenCode finishes a turn") { }
    }

    public sealed class TogglePermissionHapticsCommand : ToggleHapticPreferenceCommand
    {
        public TogglePermissionHapticsCommand()
            : base(OpenCodeHapticEvents.Permission, "Permission Request Haptics", "Enable or disable vibration when OpenCode needs approval") { }
    }

    public sealed class ToggleErrorHapticsCommand : ToggleHapticPreferenceCommand
    {
        public ToggleErrorHapticsCommand()
            : base(OpenCodeHapticEvents.Error, "Turn Error Haptics", "Enable or disable vibration when an OpenCode turn fails") { }
    }

    public sealed class ToggleQuestionHapticsCommand : ToggleHapticPreferenceCommand
    {
        public ToggleQuestionHapticsCommand()
            : base(OpenCodeHapticEvents.Question, "Question Haptics", "Enable or disable vibration when OpenCode asks a question") { }
    }

    public sealed class TogglePlanReadyHapticsCommand : ToggleHapticPreferenceCommand
    {
        public TogglePlanReadyHapticsCommand()
            : base(OpenCodeHapticEvents.PlanExit, "Plan Ready Haptics", "Enable or disable vibration when an OpenCode plan is ready") { }
    }
}
