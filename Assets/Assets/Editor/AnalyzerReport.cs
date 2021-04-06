using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Enklu.Unity.Tools
{
    /// <summary>
    /// Report for dependency analysis.
    /// </summary>
    public class AnalyzerReport
    {
        /// <summary>
        /// An error.
        /// </summary>
        private class ReportEntry
        {
            /// <summary>
            /// Error message.
            /// </summary>
            public string Message;

            /// <summary>
            /// The type of the message.
            /// </summary>
            public string MessageType;

            /// <summary>
            /// Object the error is for.
            /// </summary>
            public Object Object;

            /// <summary>
            /// The type of the object.
            /// </summary>
            public string ObjectType;
            
            /// <inheritdoc />
            public override string ToString()
            {
                return string.Format("  {0}: [{1}]    {2}: [{3}]",
                    ObjectType,
                    Object.name,
                    MessageType,
                    Message);
            }
        }
        
        /// <summary>
        /// Whitelist of acceptable dlls.
        /// </summary>
        private static readonly string[] DLL_WHITELIST =
        {
            "UnityEngine.UI.dll",
            "UnityEngine.Networking.dll",
            "UnityEngine.UIAutomation.dll",
            "UnityEngine.SpatialTracking.dll",
            "UnityEngine.Timeline.dll"
        };

        /// <summary>
        /// List of all errors.
        /// </summary>
        private readonly List<ReportEntry> _errors = new List<ReportEntry>();

        private readonly List<ReportEntry> _stats = new List<ReportEntry>();

        /// <summary>
        /// List of objects marked to ignore in the export.
        /// </summary>
        private readonly List<Object> _ignores = new List<Object>();

        /// <summary>
        /// Target object.
        /// </summary>
        public Object Target { get; private set; }

        /// <summary>
        /// True iff there were no errors.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public AnalyzerReport(Object target, Object[] deps)
        {
            Target = target;
            Success = true;
            Analyze(deps);
        }

        /// <summary>
        /// Reports an error regarding this object.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="object">The affected object.</param>
        public void Error(string message, Object @object)
        {
            Success = false;

            _errors.Add(new ReportEntry
            {
                Message = message,
                MessageType = "Error",
                Object = @object,
                ObjectType = "GameObject"
            });
        }

        /// <summary>
        /// Marks an object as something that should be ignored in the final export.
        /// </summary>
        /// <param name="object">The object to mark for ignore.</param>
        public void Ignore(Object @object)
        {
            _ignores.Add(@object);
        }

        /// <summary>
        /// True iff the object has been marked as ignored.
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns></returns>
        public bool IsIgnored(Object @object)
        {
            return _ignores.Contains(@object);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            string msgStart;
            List<ReportEntry> msgBodyLookup;

            if (Success)
            {
                msgStart = string.Format("Export complete. {0}\n", _stats.Count > 0 ? "Stats:" : "");
                msgBodyLookup = _stats;
            } else
            {
                msgStart = _errors.Count + " Error(s):\n";
                msgBodyLookup = _errors;
            }
            
            var builder = new StringBuilder();
            builder.AppendFormat("[{0}] {1}", Target.name, msgStart);
            
            foreach (var entry in msgBodyLookup)
            {
                builder.AppendFormat("{0}\n", entry);
            }

            return builder.ToString();
        }
        
        /// <summary>
        /// Analyzes a target and all dependencies for errors.
        /// </summary>
        /// <param name="deps">All listed dependencies.</param>
        /// <returns></returns>
        private void Analyze(Object[] deps)
        {
            foreach (var dependency in deps)
            {
                var gameObject = dependency as GameObject;
                if (null != gameObject)
                {
                    AnalyzeGameObject(gameObject);
                }

                var shader = dependency as Shader;
                if (null != shader)
                {
                    AnalyzeShader(shader);
                }

                var texture = dependency as Texture;
                if (null != texture)
                {
                    _stats.Add(new ReportEntry
                    {
                        Message = texture.width + "x" + texture.height,
                        MessageType = "Size",
                        Object = texture,
                        ObjectType = "Texture"
                    });
                }

                var assetPath = AssetDatabase.GetAssetPath(dependency);
                if (assetPath.EndsWith("dll"))
                {
                    // check whitelist
                    var name = Path.GetFileName(assetPath);
                    if (DLL_WHITELIST.Contains(name))
                    {
                        Ignore(dependency);
                    }
                    else
                    {
                        Error(
                            string.Format("Unsupported dll : {0}. This usually happens when using a custom script or when using a script that is not compatible with all platforms (i.e. a script that is only compatible with HoloLens but not iOS).", name),
                            dependency);
                    }
                }
            }
        }
        
        /// <summary>
        /// Analyzes a GameObject.
        /// </summary>
        /// <param name="report">The report to use.</param>
        /// <param name="gameObject">The GameObject to analyze.</param>
        private void AnalyzeGameObject(GameObject gameObject)
        {
            var components = gameObject.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null) continue;
            
                var type = component.GetType();
                var ns = type.Namespace;
                if (string.IsNullOrEmpty(ns) || !ns.StartsWith("UnityEngine"))
                {
                    Error(
                        string.Format(
                            "Custom Component of type {0} not allowed. Only UnityEngine Components are allowed.",
                            type.FullName),
                        component.gameObject);
                }
            }
        }

        /// <summary>
        /// Analyzes a Shader.
        /// </summary>
        /// <param name="report">The report to use.</param>
        /// <param name="shader">The Shader to analyze.</param>
        private void AnalyzeShader(Shader shader)
        {
            // TODO: Grab shader includes.
        }
    }
}