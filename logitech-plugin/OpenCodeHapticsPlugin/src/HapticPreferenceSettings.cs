namespace Loupedeck.OpenCodeCompanionPlugin
{
    using System;
    using System.Collections.Generic;

    internal static class HapticPreferenceSettings
    {
        private const String Prefix = "haptic.enabled.";

        public static String GetSettingName(String hapticEvent) => Prefix + hapticEvent;

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
