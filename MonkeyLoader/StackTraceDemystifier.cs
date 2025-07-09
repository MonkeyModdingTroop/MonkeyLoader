using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
namespace MonkeyLoader
{
    [HarmonyPatch(typeof(Environment))]
    internal static class StackTraceDemystifier
    {
        [HarmonyPrefix]
        [HarmonyPatch("GetStackTrace")]
        private static bool GetStackTracePrefix(out string __result, Exception e, bool needFileInfo)
        {
            var stackTrace = e != null ? new StackTrace(e, needFileInfo) : new StackTrace(needFileInfo);
            __result = new EnhancedStackTrace(stackTrace).ToString();

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Environment.StackTrace), MethodType.Getter)]
        private static bool StackTraceGetterPrefix(out string __result)
        {
            // Skip this frame and the Environment.StackTrace one
            __result = new EnhancedStackTrace(new StackTrace(2, true)).ToString();

            return false;
        }
    }
}
*/