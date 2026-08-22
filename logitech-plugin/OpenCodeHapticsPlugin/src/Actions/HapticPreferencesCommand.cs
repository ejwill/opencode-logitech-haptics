namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using Loupedeck;

    public sealed class HapticPreferencesCommand : ActionEditorCommand
    {
        private const String CompleteControl = "TurnComplete";
        private const String PermissionControl = "PermissionRequest";
        private const String ErrorControl = "TurnError";
        private const String QuestionControl = "Question";
        private const String PlanControl = "PlanReady";

        public HapticPreferencesCommand()
        {
            this.Name = "HapticPreferences";
            this.DisplayName = "Haptic Preferences";
            this.GroupName = "OpenCode Companion###Haptic Preferences";
            this.Description = "Choose which OpenCode notifications vibrate your Logitech device";

            this.ActionEditor.AddControlEx(new ActionEditorCheckbox(CompleteControl, "Turn complete")
                .SetDefaultValue(true));
            this.ActionEditor.AddControlEx(new ActionEditorCheckbox(PermissionControl, "Permission request")
                .SetDefaultValue(true));
            this.ActionEditor.AddControlEx(new ActionEditorCheckbox(ErrorControl, "Turn error")
                .SetDefaultValue(true));
            this.ActionEditor.AddControlEx(new ActionEditorCheckbox(QuestionControl, "Question awaiting answer")
                .SetDefaultValue(true));
            this.ActionEditor.AddControlEx(new ActionEditorCheckbox(PlanControl, "Plan ready")
                .SetDefaultValue(true));
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            this.Save(OpenCodeHapticEvents.Complete, actionParameters, CompleteControl);
            this.Save(OpenCodeHapticEvents.Permission, actionParameters, PermissionControl);
            this.Save(OpenCodeHapticEvents.Error, actionParameters, ErrorControl);
            this.Save(OpenCodeHapticEvents.Question, actionParameters, QuestionControl);
            this.Save(OpenCodeHapticEvents.PlanExit, actionParameters, PlanControl);
            return true;
        }

        private void Save(String hapticEvent, ActionEditorActionParameters parameters, String controlName)
        {
            if (parameters.TryGetBoolean(controlName, out var enabled)
                && this.Plugin is OpenCodeCompanionPlugin plugin)
            {
                plugin.SetHapticEventEnabled(hapticEvent, enabled);
            }
        }
    }
}
