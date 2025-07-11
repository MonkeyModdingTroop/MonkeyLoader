using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    [HarmonyPatch]
    internal static class StackTraceDemystifier
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Environment), nameof(Environment.StackTrace), MethodType.Getter)]
        private static bool StackTraceGetterPrefix(out string __result)
        {
            // This Getter is part of netstandard and should thus always exist.
            // Skip this frame and the Environment.StackTrace one.
            __result = new EnhancedStackTrace(new StackTrace(2, true)).ToString();

            return false;
        }

        [HarmonyPatch]
        private static class Core
        {
            [HarmonyPrefix]
            private static bool Prefix(Exception __instance, out string __result)
            {
                __result = new EnhancedStackTrace(new StackTrace(__instance, true)).ToString();

                return false;
            }

            private static bool Prepare()
                => TargetMethod() is not null;

            private static MethodBase TargetMethod()
                => AccessTools.DeclaredMethod(typeof(Exception), "GetStackTrace", []);
        }

        [HarmonyPatch]
        private static class Framework
        {
            [HarmonyPrefix]
            private static bool Prefix(Exception __0, out string __result)
            {
                // __0 because I don't trust in the argument name on a private method being the same across all versions
                __result = new EnhancedStackTrace(new StackTrace(__0, true)).ToString();

                return false;
            }

            private static bool Prepare()
                => TargetMethod() is not null;

            private static MethodBase TargetMethod()
                => AccessTools.DeclaredMethod(typeof(Exception), "GetStackTrace", [typeof(Exception)]);
        }

        [HarmonyPatch]
        private static class Mono
        {
            private static bool Prefix(out string __result, Exception e, bool needFileInfo)
            {
                var stackTrace = e != null ? new StackTrace(e, needFileInfo) : new StackTrace(needFileInfo);
                __result = new EnhancedStackTrace(stackTrace).ToString();

                return false;
            }

            private static bool Prepare()
                => TargetMethod() is not null;

            private static MethodBase TargetMethod()
                => AccessTools.DeclaredMethod(typeof(Environment), "GetStackTrace");
        }
    }
}