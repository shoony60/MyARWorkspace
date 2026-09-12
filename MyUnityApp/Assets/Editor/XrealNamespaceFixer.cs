#if UNITY_ANDROID
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

public sealed class XrealNamespaceFixer : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 999;

    public void OnPostGenerateGradleAndroidProject(string gradleProjectPath)
    {
        PatchModule(
            gradleProjectPath,
            "nr_loader",
            "nrsdk.pack.loader"
        );

        PatchModule(
            gradleProjectPath,
            "nr_common",
            "nrsdk.pack.common"
        );
    }

    private static void PatchModule(
        string gradleProjectPath,
        string moduleName,
        string uniqueNamespace)
    {
        string[] moduleDirectories = Directory.GetDirectories(
            gradleProjectPath,
            moduleName,
            SearchOption.AllDirectories
        );

        if (moduleDirectories.Length == 0)
        {
            Debug.LogWarning(
                $"[XrealNamespaceFixer] {moduleName} 모듈을 찾지 못했습니다."
            );
            return;
        }

        foreach (string moduleDirectory in moduleDirectories)
        {
            string gradleFile = Path.Combine(moduleDirectory, "build.gradle");

            if (!File.Exists(gradleFile))
                continue;

            string contents = File.ReadAllText(gradleFile);

            // 기존 namespace 선언이 있으면 교체합니다.
            if (Regex.IsMatch(contents, @"(?m)^\s*namespace\s+['""][^'""]+['""]"))
            {
                contents = Regex.Replace(
                    contents,
                    @"(?m)^(\s*)namespace\s+['""][^'""]+['""]",
                    $"$1namespace '{uniqueNamespace}'"
                );
            }
            else
            {
                // android { 블록 바로 아래에 namespace를 추가합니다.
                contents = Regex.Replace(
                    contents,
                    @"android\s*\{",
                    $"android {{{System.Environment.NewLine}" +
                    $"    namespace '{uniqueNamespace}'",
                    RegexOptions.None,
                    System.TimeSpan.FromSeconds(1)
                );
            }

            File.WriteAllText(gradleFile, contents);

            Debug.Log(
                $"[XrealNamespaceFixer] {moduleName} namespace → " +
                $"{uniqueNamespace}\n{gradleFile}"
            );
        }
    }
}
#endif