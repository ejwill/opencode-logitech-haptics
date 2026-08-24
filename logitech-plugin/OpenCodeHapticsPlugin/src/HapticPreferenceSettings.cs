namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using System.Collections.Generic;

    internal static class HapticPreferenceSettings
    {
        private const String Prefix = "haptic.enabled.";
        private const String WaveformPrefix = "haptic.waveform.";
        private const String IntensitySetting = "haptic.intensity";

        public static String GetSettingName(String hapticEvent) => Prefix + hapticEvent;
        public static String GetWaveformSettingName(String hapticEvent) => WaveformPrefix + hapticEvent;
        public static String GetIntensitySettingName(String hapticEvent) => IntensitySetting + "." + hapticEvent;
        public static String Intensity => IntensitySetting;

        public static readonly IReadOnlyDictionary<String, String> Labels = new Dictionary<String, String>
        {
            [OpenCodeHapticEvents.Complete] = "Turn complete",
            [OpenCodeHapticEvents.Permission] = "Permission request",
            [OpenCodeHapticEvents.Error] = "Turn error",
            [OpenCodeHapticEvents.Question] = "Question awaiting answer",
            [OpenCodeHapticEvents.PlanExit] = "Plan ready",
        };
    }
}
