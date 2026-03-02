using UnityEditor;
using UnityEngine;

namespace Nomina.Editor
{
    public static class BuildScript
    {
        [MenuItem("Build/Build Android APK")]
        public static void BuildAndroidAPK()
        {
            string[] scenes = { "Assets/Scenes/SampleScene.unity" };
            string outputPath = "Builds/Nomina.apk";

            // Ensure output directory exists
            System.IO.Directory.CreateDirectory("Builds");

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[Nomina] APK build succeeded: {outputPath} ({report.summary.totalSize} bytes)");
            }
            else
            {
                Debug.LogError($"[Nomina] APK build failed: {report.summary.result}");
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error)
                            Debug.LogError($"  {msg.content}");
                    }
                }
            }
        }
    }
}
