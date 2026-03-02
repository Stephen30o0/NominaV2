using UnityEditor;
using UnityEngine;

namespace Nomina.Editor
{
    /// <summary>
    /// Automatically adds NOMINA_SENTIS scripting define symbol when the Sentis package is present.
    /// This runs once on domain reload.
    /// </summary>
    [InitializeOnLoad]
    public static class AddSentisDefine
    {
        private const string DEFINE = "NOMINA_SENTIS";

        static AddSentisDefine()
        {
            // Check if Inference Engine (formerly Sentis) is available
            bool sentisAvailable = false;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = asm.GetName().Name;
                if (name == "Unity.InferenceEngine" || name == "Unity.Sentis")
                {
                    sentisAvailable = true;
                    break;
                }
            }

            if (sentisAvailable)
            {
                AddDefine(BuildTargetGroup.Android);
                AddDefine(BuildTargetGroup.Standalone);
            }
            else
            {
                Debug.Log("[Nomina] Unity Sentis not detected. NOMINA_SENTIS define not added.");
            }
        }

        private static void AddDefine(BuildTargetGroup group)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (!defines.Contains(DEFINE))
            {
                defines = string.IsNullOrEmpty(defines) ? DEFINE : defines + ";" + DEFINE;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
                Debug.Log($"[Nomina] Added {DEFINE} to {group} scripting defines");
            }
        }
    }
}
