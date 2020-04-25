using HarmonyLib;
using System.Reflection;

namespace TrafficManager.RedirectionFramework {
    public static class HarmonyUtil {
        /// <summary>
        /// Finds the method that is represented by the given harmony method.
        /// </summary>
        /// <param name="method">queried harmony method</param>
        /// <returns>method information</returns>
        public static MethodBase GetOriginalMethod(object method) { // use object for Cities Harmony compatibality.
            HarmonyMethod actualMethod = method as HarmonyMethod;
            if (actualMethod.declaringType == null) return null;
            if (actualMethod.methodName == null)
                return AccessTools.Constructor(actualMethod.declaringType, actualMethod.argumentTypes);
            return AccessTools.Method(actualMethod.declaringType, actualMethod.methodName, actualMethod.argumentTypes);
        }
    }
}
