namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using Loupedeck;

    public abstract class HapticEventPreferencesCommand : ActionEditorCommand
    {
        private readonly String _hapticEvent;
        private readonly String _enabledControl;
        private readonly String _intensityControl;
        private readonly String _waveformControl;

        protected HapticEventPreferencesCommand(String hapticEvent, String displayName, String description)
        {
            this._hapticEvent = hapticEvent;
            this._enabledControl = hapticEvent + "Enabled";
            this._intensityControl = hapticEvent + "Intensity";
            this._waveformControl = hapticEvent + "Waveform";
            this.Name = hapticEvent + "Preferences";
            this.DisplayName = displayName;
            this.GroupName = "OpenCode Companion###Haptic Preferences";
            this.Description = description;
            this.ActionEditor.AddControlEx(new ActionEditorCheckbox(this._enabledControl, "Enabled").SetDefaultValue(true));
            this.ActionEditor.AddControlEx(new ActionEditorListbox(this._intensityControl, "Intensity", "Choose how noticeable this notification should be"));
            this.ActionEditor.AddControlEx(new ActionEditorListbox(this._waveformControl, "Waveform", "Choose the vibration pattern for this notification"));
            this.ActionEditor.ListboxItemsRequested += this.OnListboxItemsRequested;
            this.ActionEditor.ControlsStateRequested += this.OnControlsStateRequested;
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (this.Plugin is not OpenCodeCompanionPlugin plugin) return false;
            if (actionParameters.TryGetBoolean(this._enabledControl, out var enabled)) plugin.SetHapticEventEnabled(this._hapticEvent, enabled);
            if (actionParameters.TryGetString(this._intensityControl, out var intensity)) plugin.SetHapticIntensity(this._hapticEvent, intensity);
            if (actionParameters.TryGetString(this._waveformControl, out var waveform)) plugin.SetHapticWaveform(this._hapticEvent, waveform);
            return true;
        }

        private void OnListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            if (String.Equals(e.ControlName, this._intensityControl, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var intensity in HapticIntensityProfiles.All) e.AddItem(intensity, intensity, $"Use the {intensity} profile");
                if (this.Plugin is OpenCodeCompanionPlugin plugin) e.SetSelectedItemName(plugin.GetHapticIntensity(this._hapticEvent, "normal"));
                return;
            }

            if (!String.Equals(e.ControlName, this._waveformControl, StringComparison.OrdinalIgnoreCase)) return;
            foreach (var waveform in HapticWaveforms.All) e.AddItem(waveform, waveform.Replace('_', ' '), $"Use the {waveform.Replace('_', ' ')} pattern");
            if (this.Plugin is OpenCodeCompanionPlugin waveformPlugin) e.SetSelectedItemName(waveformPlugin.GetHapticWaveform(this._hapticEvent, HapticWaveforms.All[0]));
        }

        private void OnControlsStateRequested(Object sender, ActionEditorControlsStateRequestedEventArgs e)
        {
            if (this.Plugin is not OpenCodeCompanionPlugin plugin) return;
            e.ActionEditorState.SetValue(this._enabledControl, plugin.IsHapticEventEnabled(this._hapticEvent) ? "true" : "false");
            e.ActionEditorState.SetValue(this._intensityControl, plugin.GetHapticIntensity(this._hapticEvent, "normal"));
            e.ActionEditorState.SetValue(this._waveformControl, plugin.GetHapticWaveform(this._hapticEvent, HapticWaveforms.All[0]));
        }
    }

    public sealed class TurnCompleteHapticPreferencesCommand : HapticEventPreferencesCommand
    {
        public TurnCompleteHapticPreferencesCommand() : base(OpenCodeHapticEvents.Complete, "Turn Complete Preferences", "Configure turn-complete haptic enablement, intensity, and waveform") { }
    }

    public sealed class PermissionHapticPreferencesCommand : HapticEventPreferencesCommand
    {
        public PermissionHapticPreferencesCommand() : base(OpenCodeHapticEvents.Permission, "Permission Preferences", "Configure permission-request haptic enablement, intensity, and waveform") { }
    }

    public sealed class ErrorHapticPreferencesCommand : HapticEventPreferencesCommand
    {
        public ErrorHapticPreferencesCommand() : base(OpenCodeHapticEvents.Error, "Turn Error Preferences", "Configure turn-error haptic enablement, intensity, and waveform") { }
    }

    public sealed class QuestionHapticPreferencesCommand : HapticEventPreferencesCommand
    {
        public QuestionHapticPreferencesCommand() : base(OpenCodeHapticEvents.Question, "Question Preferences", "Configure question haptic enablement, intensity, and waveform") { }
    }

    public sealed class PlanReadyHapticPreferencesCommand : HapticEventPreferencesCommand
    {
        public PlanReadyHapticPreferencesCommand() : base(OpenCodeHapticEvents.PlanExit, "Plan Ready Preferences", "Configure plan-ready haptic enablement, intensity, and waveform") { }
    }
}
