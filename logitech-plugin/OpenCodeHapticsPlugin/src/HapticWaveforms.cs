namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using System.Collections.Generic;

    internal static class HapticWaveforms
    {
        public static readonly IReadOnlyList<String> All = new[]
        {
            "sharp_state_change", "damp_state_change", "sharp_collision", "damp_collision",
            "subtle_collision", "happy_alert", "angry_alert", "completed", "square", "wave",
            "firework", "mad", "knock", "jingle", "ringing",
        };

        public static Boolean IsSupported(String waveform) => !String.IsNullOrWhiteSpace(waveform)
            && All.Contains(waveform, StringComparer.OrdinalIgnoreCase);
    }
}
