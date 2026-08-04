using System;
using System.Linq;
using System.Reflection;

namespace RemoteTechWormholeBridge
{
    internal static class AssemblyVersions
    {
        internal static void LogDetectedVersions()
        {
            Log.Info("version plugin=" + PluginVersion.Current + " ksp=" + Versioning.GetVersionStringFull());
            LogAssembly("RemoteTech", true);
            LogAssembly("KEX-Wormholes", true);
            LogAssembly("Kopernicus", true);
            LogAssembly("0Harmony", true);
            LogAssembly("ModuleManager", false);
            LogAssembly("WormholeSignalBridge", false);
        }

        internal static Assembly Find(string simpleName)
        {
            return AssemblyLoader.loadedAssemblies
                .Select(loaded => loaded.assembly)
                .FirstOrDefault(assembly => String.Equals(
                    assembly.GetName().Name, simpleName, StringComparison.Ordinal));
        }

        private static void LogAssembly(string simpleName, bool required)
        {
            Assembly assembly = Find(simpleName);
            if (assembly == null)
            {
                string message = "dependency name=" + simpleName + " status=missing required=" + required;
                if (required)
                    Log.Error(message);
                else
                    Log.Info(message);
                return;
            }

            Log.Info("dependency name=" + simpleName + " status=loaded version=" +
                     assembly.GetName().Version);
        }
    }

    internal static class PluginVersion
    {
        internal const string Current = "0.4.0-render-test";
    }
}
