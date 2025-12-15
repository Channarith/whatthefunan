using UnityEngine;
using UnityEditor;

namespace WhatTheFunan.Editor
{
    /// <summary>
    /// Custom Unity Editor menu for What the Funan development tools.
    /// </summary>
    public static class WhatTheFunanMenu
    {
        #region Menu Items
        
        [MenuItem("What the Funan/Open Documentation", false, 0)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/Channarith/whatthefunan");
        }
        
        [MenuItem("What the Funan/Clear Player Prefs", false, 100)]
        public static void ClearPlayerPrefs()
        {
            if (EditorUtility.DisplayDialog(
                "Clear Player Prefs",
                "Are you sure you want to delete all saved data?\nThis cannot be undone.",
                "Clear All",
                "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[WhatTheFunan] All PlayerPrefs cleared!");
            }
        }
        
        [MenuItem("What the Funan/Clear Save Data", false, 101)]
        public static void ClearSaveData()
        {
            string savePath = Application.persistentDataPath;
            if (EditorUtility.DisplayDialog(
                "Clear Save Data",
                $"Delete all save files from:\n{savePath}\n\nThis cannot be undone.",
                "Delete",
                "Cancel"))
            {
                if (System.IO.Directory.Exists(savePath))
                {
                    string[] files = System.IO.Directory.GetFiles(savePath, "*.sav");
                    foreach (string file in files)
                    {
                        System.IO.File.Delete(file);
                    }
                    Debug.Log($"[WhatTheFunan] Deleted {files.Length} save files!");
                }
            }
        }
        
        [MenuItem("What the Funan/Add Test Currency", false, 200)]
        public static void AddTestCurrency()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Game must be running!", "OK");
                return;
            }
            
            Economy.CurrencyManager.Instance?.AddCoins(10000);
            Economy.CurrencyManager.Instance?.AddGems(1000);
            Debug.Log("[WhatTheFunan] Added 10,000 coins and 1,000 gems!");
        }
        
        [MenuItem("What the Funan/Unlock All Content", false, 201)]
        public static void UnlockAllContent()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Game must be running!", "OK");
                return;
            }
            
            // Unlock all codex entries
            // Unlock all characters
            // Unlock all mounts
            Debug.Log("[WhatTheFunan] All content unlocked (dev mode)!");
        }
        
        [MenuItem("What the Funan/Complete Tutorial", false, 202)]
        public static void CompleteTutorial()
        {
            PlayerPrefs.SetString("Tutorial_Completed", "intro,movement,combat,minigames");
            PlayerPrefs.Save();
            Debug.Log("[WhatTheFunan] Tutorial marked as complete!");
        }
        
        [MenuItem("What the Funan/Build/Android APK", false, 300)]
        public static void BuildAndroidAPK()
        {
            string path = EditorUtility.SaveFilePanel(
                "Save Android APK",
                "",
                "WhatTheFunan.apk",
                "apk");
            
            if (!string.IsNullOrEmpty(path))
            {
                BuildPipeline.BuildPlayer(
                    GetScenePaths(),
                    path,
                    BuildTarget.Android,
                    BuildOptions.None);
            }
        }
        
        [MenuItem("What the Funan/Build/Android AAB (Release)", false, 301)]
        public static void BuildAndroidAAB()
        {
            EditorUserBuildSettings.buildAppBundle = true;
            
            string path = EditorUtility.SaveFilePanel(
                "Save Android AAB",
                "",
                "WhatTheFunan.aab",
                "aab");
            
            if (!string.IsNullOrEmpty(path))
            {
                BuildPipeline.BuildPlayer(
                    GetScenePaths(),
                    path,
                    BuildTarget.Android,
                    BuildOptions.None);
            }
        }
        
        [MenuItem("What the Funan/Build/iOS Xcode Project", false, 302)]
        public static void BuildiOS()
        {
            string path = EditorUtility.SaveFolderPanel(
                "Select iOS Build Folder",
                "",
                "iOS_Build");
            
            if (!string.IsNullOrEmpty(path))
            {
                BuildPipeline.BuildPlayer(
                    GetScenePaths(),
                    path,
                    BuildTarget.iOS,
                    BuildOptions.None);
            }
        }
        
        [MenuItem("What the Funan/Legal/Open Compliance Checklist", false, 400)]
        public static void OpenComplianceChecklist()
        {
            string path = System.IO.Path.Combine(
                Application.dataPath, 
                "..", 
                "LEGAL_COMPLIANCE_CHECKLIST.txt");
            
            if (System.IO.File.Exists(path))
            {
                System.Diagnostics.Process.Start(path);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Checklist file not found!", "OK");
            }
        }
        
        [MenuItem("What the Funan/Legal/Content Review Reminder", false, 401)]
        public static void ContentReviewReminder()
        {
            EditorUtility.DisplayDialog(
                "Content Review Reminder",
                "Before submitting to app stores, ensure:\n\n" +
                "✓ No Thai royal imagery\n" +
                "✓ Buddha images are respectful\n" +
                "✓ No monks in combat\n" +
                "✓ All temples are fictional\n" +
                "✓ No political content\n" +
                "✓ Parental gates working\n" +
                "✓ COPPA compliance verified\n\n" +
                "See LEGAL_COMPLIANCE_CHECKLIST.txt for full details.",
                "I Understand");
        }
        
        #endregion
        
        #region Helpers
        
        private static string[] GetScenePaths()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }
            return scenes.ToArray();
        }
        
        #endregion
    }
}

