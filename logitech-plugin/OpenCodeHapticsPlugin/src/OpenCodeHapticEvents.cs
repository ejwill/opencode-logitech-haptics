namespace Loupedeck.OpenCodeHapticsPlugin
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
    }
}
