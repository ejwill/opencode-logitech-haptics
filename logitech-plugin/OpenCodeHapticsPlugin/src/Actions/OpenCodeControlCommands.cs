namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;

    public abstract class OpenCodeControlCommand : PluginDynamicCommand
    {
        protected OpenCodeControlCommand(String displayName, String description)
            : base(displayName, description, "OpenCode Companion###OpenCode Controls") { }

        protected OpenCodeCompanionPlugin Companion => this.Plugin as OpenCodeCompanionPlugin;

        protected void Report(String action, Boolean succeeded)
        {
            PluginLog.Info($"OpenCode control '{action}' {(succeeded ? "completed" : "was unavailable")}");
        }
    }

    public sealed class SwitchOpenCodeAgentCommand : OpenCodeControlCommand
    {
        public SwitchOpenCodeAgentCommand()
            : base("Switch Agent", "Cycle through the available OpenCode primary agents") { }

        protected override void RunCommand(String actionParameter)
        {
            var succeeded = this.Companion?.OpenCodeControls.CycleAgentAsync().GetAwaiter().GetResult() == true;
            this.Report("switch agent", succeeded);
        }
    }

    public sealed class SwitchOpenCodeThinkingLevelCommand : OpenCodeControlCommand
    {
        public SwitchOpenCodeThinkingLevelCommand()
            : base("Switch Thinking Level", "Cycle through variants for the active OpenCode model") { }

        protected override void RunCommand(String actionParameter)
        {
            var succeeded = this.Companion?.OpenCodeControls.CycleThinkingLevelAsync().GetAwaiter().GetResult() == true;
            this.Report("switch thinking level", succeeded);
        }
    }

    public sealed class OpenCodeSessionSwitcherCommand : OpenCodeControlCommand
    {
        public OpenCodeSessionSwitcherCommand()
            : base("Open Session Switcher", "Open the active OpenCode session selector") { }

        protected override void RunCommand(String actionParameter)
        {
            var succeeded = this.Companion?.OpenCodeControls.OpenSessionSelectorAsync().GetAwaiter().GetResult() == true;
            this.Report("open session selector", succeeded);
        }
    }

    public sealed class OpenCodeModelSelectorCommand : OpenCodeControlCommand
    {
        public OpenCodeModelSelectorCommand()
            : base("Open Model Selector", "Open the active OpenCode model selector") { }

        protected override void RunCommand(String actionParameter)
        {
            var succeeded = this.Companion?.OpenCodeControls.OpenModelSelectorAsync().GetAwaiter().GetResult() == true;
            this.Report("open model selector", succeeded);
        }
    }

    public sealed class StopOpenCodeTurnCommand : OpenCodeControlCommand
    {
        public StopOpenCodeTurnCommand()
            : base("Stop Current Turn", "Abort the active OpenCode turn") { }

        protected override void RunCommand(String actionParameter)
        {
            var succeeded = this.Companion?.OpenCodeControls.AbortSessionAsync(this.Companion.GetLastOpenCodeSessionID()).GetAwaiter().GetResult() == true;
            this.Report("stop current turn", succeeded);
        }
    }
}
