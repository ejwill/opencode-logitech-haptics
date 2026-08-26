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
        private const String CompleteWaveformControl = "TurnCompleteWaveform";
        private const String PermissionWaveformControl = "PermissionWaveform";
        private const String ErrorWaveformControl = "ErrorWaveform";
        private const String QuestionWaveformControl = "QuestionWaveform";
        private const String PlanWaveformControl = "PlanWaveform";
        private const String IntensityControl = "Intensity";

        public HapticPreferencesCommand()
        {
            this.Name = "HapticPreferences";
            this.DisplayName = "Haptic Preferences";
            this.GroupName = "OpenCode Companion###Haptic Preferences";
            this.Description = "View and configure notification enablement, intensity, and waveform choices";

            this.ActionEditor.AddControlEx(new ActionEditorListbox(IntensityControl, "Haptic intensity profile", "Select subtle, normal, or strong waveform patterns"));

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
            this.ActionEditor.AddControlEx(new ActionEditorListbox(CompleteWaveformControl, "Turn complete waveform", "Waveform used when OpenCode finishes a turn"));
            this.ActionEditor.AddControlEx(new ActionEditorListbox(PermissionWaveformControl, "Permission waveform", "Waveform used when OpenCode needs approval"));
            this.ActionEditor.AddControlEx(new ActionEditorListbox(ErrorWaveformControl, "Error waveform", "Waveform used when an OpenCode turn fails"));
            this.ActionEditor.AddControlEx(new ActionEditorListbox(QuestionWaveformControl, "Question waveform", "Waveform used when OpenCode asks a question"));
            this.ActionEditor.AddControlEx(new ActionEditorListbox(PlanWaveformControl, "Plan ready waveform", "Waveform used when an OpenCode plan is ready"));
            this.ActionEditor.ListboxItemsRequested += this.OnListboxItemsRequested;
            this.ActionEditor.ControlsStateRequested += this.OnControlsStateRequested;
            this.ActionEditor.ControlValueChanged += this.OnControlValueChanged;
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            this.Save(OpenCodeHapticEvents.Complete, actionParameters, CompleteControl);
            this.Save(OpenCodeHapticEvents.Permission, actionParameters, PermissionControl);
            this.Save(OpenCodeHapticEvents.Error, actionParameters, ErrorControl);
            this.Save(OpenCodeHapticEvents.Question, actionParameters, QuestionControl);
            this.Save(OpenCodeHapticEvents.PlanExit, actionParameters, PlanControl);
            this.SaveWaveform(OpenCodeHapticEvents.Complete, actionParameters, CompleteWaveformControl);
            this.SaveWaveform(OpenCodeHapticEvents.Permission, actionParameters, PermissionWaveformControl);
            this.SaveWaveform(OpenCodeHapticEvents.Error, actionParameters, ErrorWaveformControl);
            this.SaveWaveform(OpenCodeHapticEvents.Question, actionParameters, QuestionWaveformControl);
            this.SaveWaveform(OpenCodeHapticEvents.PlanExit, actionParameters, PlanWaveformControl);
            this.SaveIntensity(actionParameters);
            return true;
        }

        private void OnListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            PluginLog.Info($"Haptic Preferences listbox requested: {e.ControlName}");
            if (String.Equals(e.ControlName, IntensityControl, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var intensity in HapticIntensityProfiles.All) e.AddItem(intensity, intensity, $"Use the {intensity} haptic profile");
                if (this.Plugin is OpenCodeCompanionPlugin intensityPlugin) e.SetSelectedItemName(intensityPlugin.GetHapticIntensity("normal"));
                PluginLog.Info($"Haptic Preferences populated {HapticIntensityProfiles.All.Count} intensity profiles");
                return;
            }

            if (!this.TryGetWaveformEvent(e.ControlName, out var hapticEvent))
            {
                PluginLog.Warning($"Haptic Preferences has no waveform mapping for listbox: {e.ControlName}");
                return;
            }

            foreach (var waveform in HapticWaveforms.All) e.AddItem(waveform, waveform.Replace('_', ' '), $"Use the {waveform.Replace('_', ' ')} pattern");
            if (this.Plugin is OpenCodeCompanionPlugin plugin) e.SetSelectedItemName(plugin.GetHapticWaveform(hapticEvent, HapticWaveforms.All[0]));
            PluginLog.Info($"Haptic Preferences populated {HapticWaveforms.All.Count} waveforms for {e.ControlName}");
        }

        private void OnControlsStateRequested(Object sender, ActionEditorControlsStateRequestedEventArgs e)
        {
            if (this.Plugin is not OpenCodeCompanionPlugin plugin) return;
            this.SetCurrentValue(e.ActionEditorState, CompleteControl, plugin.IsHapticEventEnabled(OpenCodeHapticEvents.Complete));
            this.SetCurrentValue(e.ActionEditorState, PermissionControl, plugin.IsHapticEventEnabled(OpenCodeHapticEvents.Permission));
            this.SetCurrentValue(e.ActionEditorState, ErrorControl, plugin.IsHapticEventEnabled(OpenCodeHapticEvents.Error));
            this.SetCurrentValue(e.ActionEditorState, QuestionControl, plugin.IsHapticEventEnabled(OpenCodeHapticEvents.Question));
            this.SetCurrentValue(e.ActionEditorState, PlanControl, plugin.IsHapticEventEnabled(OpenCodeHapticEvents.PlanExit));
            e.ActionEditorState.SetValue(CompleteWaveformControl, plugin.GetHapticWaveform(OpenCodeHapticEvents.Complete, HapticWaveforms.All[0]));
            e.ActionEditorState.SetValue(PermissionWaveformControl, plugin.GetHapticWaveform(OpenCodeHapticEvents.Permission, HapticWaveforms.All[0]));
            e.ActionEditorState.SetValue(ErrorWaveformControl, plugin.GetHapticWaveform(OpenCodeHapticEvents.Error, HapticWaveforms.All[0]));
            e.ActionEditorState.SetValue(QuestionWaveformControl, plugin.GetHapticWaveform(OpenCodeHapticEvents.Question, HapticWaveforms.All[0]));
            e.ActionEditorState.SetValue(PlanWaveformControl, plugin.GetHapticWaveform(OpenCodeHapticEvents.PlanExit, HapticWaveforms.All[0]));
            e.ActionEditorState.SetValue(IntensityControl, plugin.GetHapticIntensity("normal"));
            this.UpdateDisplayName(e.ActionEditorState);
        }

        private void OnControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e) => this.UpdateDisplayName(e.ActionEditorState);

        private void SetCurrentValue(ActionEditorState state, String controlName, Boolean enabled) => state.SetValue(controlName, enabled ? "true" : "false");

        private void UpdateDisplayName(ActionEditorState state)
        {
            var enabled = new[] { CompleteControl, PermissionControl, ErrorControl, QuestionControl, PlanControl }
                .Count(control => String.Equals(state.GetControlValue(control), "true", StringComparison.OrdinalIgnoreCase));
            state.SetDisplayName($"Haptic Preferences ({enabled}/5 notifications enabled)");
        }

        private void Save(String hapticEvent, ActionEditorActionParameters parameters, String controlName)
        {
            if (parameters.TryGetBoolean(controlName, out var enabled)
                && this.Plugin is OpenCodeCompanionPlugin plugin)
            {
                plugin.SetHapticEventEnabled(hapticEvent, enabled);
            }
        }

        private void SaveWaveform(String hapticEvent, ActionEditorActionParameters parameters, String controlName)
        {
            if (parameters.TryGetString(controlName, out var waveform)
                && this.Plugin is OpenCodeCompanionPlugin plugin) plugin.SetHapticWaveform(hapticEvent, waveform);
        }

        private void SaveIntensity(ActionEditorActionParameters parameters)
        {
            if (parameters.TryGetString(IntensityControl, out var intensity)
                && this.Plugin is OpenCodeCompanionPlugin plugin) plugin.SetHapticIntensity(intensity);
        }

        private Boolean TryGetWaveformEvent(String controlName, out String hapticEvent)
        {
            hapticEvent = null;
            if (String.Equals(controlName, CompleteWaveformControl, StringComparison.OrdinalIgnoreCase)) hapticEvent = OpenCodeHapticEvents.Complete;
            else if (String.Equals(controlName, PermissionWaveformControl, StringComparison.OrdinalIgnoreCase)) hapticEvent = OpenCodeHapticEvents.Permission;
            else if (String.Equals(controlName, ErrorWaveformControl, StringComparison.OrdinalIgnoreCase)) hapticEvent = OpenCodeHapticEvents.Error;
            else if (String.Equals(controlName, QuestionWaveformControl, StringComparison.OrdinalIgnoreCase)) hapticEvent = OpenCodeHapticEvents.Question;
            else if (String.Equals(controlName, PlanWaveformControl, StringComparison.OrdinalIgnoreCase)) hapticEvent = OpenCodeHapticEvents.PlanExit;
            return hapticEvent is not null;
        }
    }
}
