using UnityEngine;

namespace RemoteTechWormholeBridge
{
    internal static class Log
    {
        private const string Prefix = "[RTWB]";

        internal static void Info(string message)
        {
            Debug.Log(Prefix + " " + message);
        }

        internal static void Warning(string message)
        {
            Debug.LogWarning(Prefix + " " + message);
        }

        internal static void Error(string message)
        {
            Debug.LogError(Prefix + " " + message);
        }
    }
}
