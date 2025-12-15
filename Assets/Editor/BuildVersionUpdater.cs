using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace WhatTheFunan.Editor
{
    /// <summary>
    /// Automatically updates build version numbers before each build.
    /// </summary>
    public class BuildVersionUpdater : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            // Increment build number
            int currentBuild = PlayerPrefs.GetInt("BuildNumber", 0);
            currentBuild++;
            PlayerPrefs.SetInt("BuildNumber", currentBuild);
            
            // Update Android bundle version code
            PlayerSettings.Android.bundleVersionCode = currentBuild;
            
            // Update iOS build number
            PlayerSettings.iOS.buildNumber = currentBuild.ToString();
            
            // Log
            Debug.Log($"[BuildVersionUpdater] Build #{currentBuild} for {report.summary.platform}");
            
            // Show content review reminder for release builds
            if (!EditorUserBuildSettings.development)
            {
                if (!EditorUtility.DisplayDialog(
                    "Release Build Confirmation",
                    "You are creating a RELEASE build.\n\n" +
                    "Have you completed the Legal Compliance Checklist?\n\n" +
                    "• Thailand lèse-majesté review\n" +
                    "• Cultural content review\n" +
                    "• COPPA compliance\n" +
                    "• Parental controls tested",
                    "Yes, Proceed",
                    "Cancel Build"))
                {
                    throw new BuildFailedException("Build cancelled by user - complete compliance review first.");
                }
            }
        }
    }
    
    /// <summary>
    /// Post-build actions.
    /// </summary>
    public class BuildPostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result == BuildResult.Succeeded)
            {
                string size = FormatBytes(report.summary.totalSize);
                Debug.Log($"[BuildVersionUpdater] Build succeeded! Size: {size}");
                Debug.Log($"[BuildVersionUpdater] Output: {report.summary.outputPath}");
                
                // Update changelog
                UpdateChangelog(report);
            }
            else
            {
                Debug.LogError($"[BuildVersionUpdater] Build failed with {report.summary.totalErrors} errors");
            }
        }
        
        private void UpdateChangelog(BuildReport report)
        {
            // Could append build info to changelog
            string platform = report.summary.platform.ToString();
            string version = PlayerSettings.bundleVersion;
            Debug.Log($"[BuildVersionUpdater] Built {platform} v{version}");
        }
        
        private string FormatBytes(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}

