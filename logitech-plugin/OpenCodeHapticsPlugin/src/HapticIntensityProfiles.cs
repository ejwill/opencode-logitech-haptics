namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using System.Collections.Generic;

    internal static class HapticIntensityProfiles
    {
        public static readonly IReadOnlyList<String> All = new[] { "subtle", "normal", "strong" };

        private static readonly IReadOnlyDictionary<String, IReadOnlyDictionary<String, String>> Profiles =
            new Dictionary<String, IReadOnlyDictionary<String, String>>(StringComparer.OrdinalIgnoreCase)
            {
                ["subtle"] = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
                {
                    [OpenCodeHapticEvents.Complete] = "damp_state_change",
                    [OpenCodeHapticEvents.Permission] = "subtle_collision",
                    [OpenCodeHapticEvents.Error] = "damp_state_change",
                    [OpenCodeHapticEvents.Question] = "wave",
                    [OpenCodeHapticEvents.PlanExit] = "happy_alert",
                },
                ["normal"] = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
                {
                    [OpenCodeHapticEvents.Complete] = "completed",
                    [OpenCodeHapticEvents.Permission] = "knock",
                    [OpenCodeHapticEvents.Error] = "angry_alert",
                    [OpenCodeHapticEvents.Question] = "ringing",
                    [OpenCodeHapticEvents.PlanExit] = "happy_alert",
                },
                ["strong"] = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
                {
                    [OpenCodeHapticEvents.Complete] = "firework",
                    [OpenCodeHapticEvents.Permission] = "ringing",
                    [OpenCodeHapticEvents.Error] = "mad",
                    [OpenCodeHapticEvents.Question] = "jingle",
                    [OpenCodeHapticEvents.PlanExit] = "firework",
                },
            };

        public static Boolean IsSupported(String intensity) => !String.IsNullOrWhiteSpace(intensity) && Profiles.ContainsKey(intensity);

        public static String GetWaveform(String intensity, String hapticEvent) =>
            IsSupported(intensity) && Profiles[intensity].TryGetValue(hapticEvent, out var waveform) ? waveform : null;
    }
}
