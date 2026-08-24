namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using System.Collections.Generic;

    internal static class OpenCodeHapticEvents
    {
        public const String Complete = "opencodeComplete";
        public const String Permission = "opencodePermission";
        public const String Error = "opencodeError";
        public const String Question = "opencodeQuestion";
        public const String PlanExit = "opencodePlanExit";
        public const String Test = "opencodeTest";

        public static readonly IReadOnlyDictionary<String, String> FromOpenCodeEvent = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
        {
            ["complete"] = Complete,
            ["permission"] = Permission,
            ["error"] = Error,
            ["question"] = Question,
            ["plan_exit"] = PlanExit,
            ["test"] = Test,
        };

        private static readonly IReadOnlySet<String> SupportedWaveforms = new HashSet<String>(StringComparer.OrdinalIgnoreCase)
        {
            "sharp_state_change", "damp_state_change", "sharp_collision", "damp_collision",
            "subtle_collision", "happy_alert", "angry_alert", "completed", "square", "wave",
            "firework", "mad", "knock", "jingle", "ringing",
        };

        public static String Resolve(String openCodeEvent, String waveform)
        {
            if (!FromOpenCodeEvent.TryGetValue(openCodeEvent, out var baseEvent)) return null;
            if (String.Equals(baseEvent, Test, StringComparison.Ordinal)) return baseEvent;
            return String.IsNullOrWhiteSpace(waveform) || !SupportedWaveforms.Contains(waveform)
                ? baseEvent
                : $"{baseEvent}_{waveform.ToLowerInvariant()}";
        }

        public static String GetBaseEvent(String hapticEvent)
        {
            foreach (var baseEvent in new[] { Complete, Permission, Error, Question, PlanExit, Test })
            {
                if (String.Equals(hapticEvent, baseEvent, StringComparison.Ordinal)
                    || hapticEvent.StartsWith(baseEvent + "_", StringComparison.Ordinal)) return baseEvent;
            }
            return hapticEvent;
        }
    }
}
