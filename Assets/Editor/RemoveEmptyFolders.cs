using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RuntimeEditor.Helpers
{
    /// <summary>
    /// Remove empty folders automatically.
    /// </summary>
    public class RemoveEmptyFolders: UnityEditor.AssetModificationProcessor
    {
        public const string kMenuText = "Assets/Remove Empty Folders";
        static readonly StringBuilder s_Log = new StringBuilder();
        static readonly List<DirectoryInfo> s_Results = new List<DirectoryInfo>();

        /// <summary>
        /// Raises the initialize on load method event.
        /// </summary>
        [InitializeOnLoadMethod]
        static void OnInitializeOnLoadMethod()
        {
            EditorApplication.delayCall += () => Valid();
        }

        /// <summary>
        /// Raises the will save assets event.
        /// </summary>
        static string[] OnWillSaveAssets(string[] paths)
        {
            // If menu is unchecked, do nothing.
            if (!EditorPrefs.GetBool(kMenuText, false)) return paths;

            var assetsDir = Application.dataPath + Path.DirectorySeparatorChar;

            var cache = s_Results.ToList();
            s_Results.Clear();
            cache.ForEach(t => GetEmptyDirectories(t, s_Results));

            // When empty directories has detected, remove the directory.
            if (0 < s_Results.Count) {
                s_Log.Length = 0;
                s_Log.AppendFormat("Remove {0} empty directories as following:\n", s_Results.Count);
                foreach (var d in s_Results) {
                    s_Log.AppendFormat("- {0}\n", d.FullName.Replace(assetsDir, ""));
                    FileUtil.DeleteFileOrDirectory(d.FullName);
                    try {
                        FileUtil.DeleteFileOrDirectory(d.Parent +
                            "\\" +
                            d.Name +
                            ".meta"); // unity 2020.2 need to delete the meta too
                    } catch (Exception e) { }
                }

                // UNITY BUG: Debug.Log can not set about more than 15000 characters.
                s_Log.Length = Mathf.Min(s_Log.Length, 15000);
                Debug.Log(s_Log.ToString());
                s_Log.Length = 0;
                AssetDatabase.Refresh();
            }

            // Get empty directories in Assets directory
            s_Results.Clear();
            GetEmptyDirectories(new DirectoryInfo(assetsDir), s_Results);
            GetEmptyDirectories(new DirectoryInfo(Application.dataPath +"/../Packages"), s_Results);

            return paths;
        }

        /// <summary>
        /// Toggles the menu.
        /// </summary>
        [MenuItem(kMenuText)]
        static void OnClickMenu()
        {
            // Check/Uncheck menu.
            bool isChecked = !Menu.GetChecked(kMenuText);
            Menu.SetChecked(kMenuText, isChecked);

            // Save to EditorPrefs.
            EditorPrefs.SetBool(kMenuText, isChecked);
            OnWillSaveAssets(null);
        }

        [MenuItem(kMenuText, true)]
        static bool Valid()
        {
            // Check/Uncheck menu from EditorPrefs.
            Menu.SetChecked(kMenuText, EditorPrefs.GetBool(kMenuText, false));
            return true;
        }

        [MenuItem("Assets/Clear Empty Folders")]
        static void ClearDirect()
        {
            OnWillSaveAssets(null);
            OnWillSaveAssets(null);
        }

        /// <summary>
        /// Get empty directories.
        /// </summary>
        static bool GetEmptyDirectories(DirectoryInfo dir, List<DirectoryInfo> results)
        {
            bool isEmpty = true;
            try {
                isEmpty =
                    dir.GetDirectories().Count(x => !GetEmptyDirectories(x, results)) ==
                    0 // Are sub directories empty?
                    &&
                    dir.GetFiles("*.*")
                        .All(x => x.Extension == ".meta" || x.Name.Contains(".DS_Store")); // No file exist?
            } catch { }

            // Store empty directory to results.
            if (isEmpty) results.Add(dir);
            return isEmpty;
        }
    }
}
