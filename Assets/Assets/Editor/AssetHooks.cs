using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Enklu.Unity.Tools
{
    /// <summary>
    /// Provides hooks for exporting assets.
    /// </summary>
    public class AssetHooks
    {
        /// <summary>
        /// Recented a prefab at the origin.
        /// </summary>
        [MenuItem("Assets/Recenter")]
        private static void Recenter()
        {
            var active = Selection.gameObjects;
            foreach (var obj in active)
            {
                obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        /// <summary>
        /// Exports a prefab and all dependencies into a zip.
        /// </summary>
        [MenuItem("Assets/Export")]
        private static void Export()
        {
            // Display a progress bar - just to indicate a potentially lengthy op is happening.
            EditorUtility.DisplayProgressBar("Please wait", "Exporting asset(s)...", 0);
        
            var gameObjects = Selection.gameObjects;
            var root = Path.Combine(Application.dataPath, "../Exports");
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
            
            var reports = new List<AnalyzerReport>();
            foreach (var active in gameObjects)
            {
                var paths = new List<string>();
                var deps = EditorUtility.CollectDependencies(new Object[] { active });

                var report = new AnalyzerReport(active, deps);
                reports.Add(report);
            
                if (!report.Success)
                {
                    continue;
                }

                var error = string.Empty;
                var path = Path.Combine(root, active.name + ".zip");
                using (var file = File.Create(path))
                {
                    using (var zipOut = new ZipOutputStream(file))
                    {
                        zipOut.SetLevel(5);

                        foreach (var asset in deps.Distinct())
                        {
                            if (report.IsIgnored(asset))
                            {
                                continue;
                            }

                            var assetPath = AssetDatabase.GetAssetPath(asset).Replace("Assets/", string.Empty);
                            var filePath = Path.Combine(
                                Application.dataPath,
                                assetPath);

                            // some builtin unity resources don't exist
                            if (!File.Exists(filePath))
                            {
                                continue;
                            }

                            // already added
                            if (paths.Contains(filePath))
                            {
                                continue;
                            }

                            paths.Add(filePath);
                        
                            if (!ExportFile(filePath, assetPath, zipOut, out error)
                                || !ExportFile(filePath + ".meta", assetPath + ".meta", zipOut, out error))
                            {
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(error))
                {
                    File.Delete(path);
                    report.Error(error, active);
                }
            }

            var output = new StringBuilder();
            for (var i = 0; i < reports.Count; i++)
            {
                var report = reports[i];
                output.AppendLine(report + "\n");
            }

            EditorUtility.ClearProgressBar();

            var dialogResult = EditorUtility.DisplayDialog("Export Complete",
                output.ToString(), 
                "View Exports", "Ok");

            if (dialogResult)
            {
                Application.OpenURL(root);
            }
        }
    
        /// <summary>
        /// Writes a file to the zip.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="assetPath">Path to the asset.</param>
        /// <param name="zipOut">Zip stream to write to.</param>
        /// <param name="error">An error to return, iff method evaluates to false.</param>
        /// <returns></returns>
        private static bool ExportFile(
            string filePath,
            string assetPath,
            ZipOutputStream zipOut,
            out string error)
        {
            var info = new FileInfo(filePath);
            if (!info.Exists)
            {
                error = string.Format(
                    "{0} does not exist or is not supported.",
                    filePath);
                return false;
            }

            var entry = new ZipEntry(assetPath)
            {
                DateTime = info.LastWriteTime,
                Size = info.Length
            };

            zipOut.PutNextEntry(entry);
            var buffer = new Byte[4096];
            using (var stream = File.OpenRead(filePath))
            {
                StreamUtils.Copy(stream, zipOut, buffer);
            }

            zipOut.CloseEntry();

            error = string.Empty;
            return true;
        }
    }
}