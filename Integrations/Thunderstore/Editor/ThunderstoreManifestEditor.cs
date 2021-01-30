﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThunderKit.Core.Manifests;
using ThunderKit.Integrations.Thunderstore.Manifests;
using UnityEditor;
using UnityEngine;
using static ThunderKit.Integrations.Thunderstore.Constants;
using static UnityEditor.EditorGUIUtility;
using static UnityEngine.GUILayout;

namespace ThunderKit.Integrations.Thunderstore.Editor
{
    [CustomEditor(typeof(ThunderstoreManifest), true)]
    public class ThunderstoreManifestEditor : UnityEditor.Editor
    {
        PackageSearchSuggest suggestor = new PackageSearchSuggest
        {
            Evaluate = EvaluateSuggestion,
        };
        private Rect dragDropRect;

        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject, "m_Script", nameof(ThunderstoreManifest.dependencies));

            var property = serializedObject.FindProperty(nameof(ThunderstoreManifest.dependencies));

            var thunderManifest = serializedObject.targetObject as ThunderstoreManifest;
            var manifest = AssetDatabase.LoadAssetAtPath<Manifest>(AssetDatabase.GetAssetPath(thunderManifest));
            if (thunderManifest)
            {

                using (new VerticalScope(GUI.skin.box))
                {
                    Label("Dependencies");
                    for (int i = 0; i < thunderManifest.dependencies.Count; i++)
                    {
                        var depName = thunderManifest.dependencies[i];

                        Label(depName);

                        var bp = GUILayoutUtility.GetLastRect();
                        bp = new Rect(bp.width + 4, bp.y + 1, 13, bp.height - 2);
                        if (Event.current.type == EventType.Repaint)
                        {
                            GUI.skin.box.Draw(bp, new GUIContent(""), 0);
                            GUIContent content = new GUIContent("x");
                            var contentSize = GUIStyle.none.CalcSize(content);
                            GUIStyle.none.Draw(new Rect(bp.x + 3, bp.y - 1, bp.width, bp.height), content, 0);
                        }
                        if (Event.current.type == EventType.MouseUp && bp.Contains(Event.current.mousePosition))
                        {
                            var dependencyPath = Path.Combine(Packages, depName);

                            if (Directory.Exists(dependencyPath)) Directory.Delete(dependencyPath, true);

                            var listed = thunderManifest.dependencies.ToList();
                            listed.RemoveAt(i);
                            thunderManifest.dependencies = new DependencyList(listed);

                            property.serializedObject.SetIsDifferentCacheDirty();

                            property.serializedObject.ApplyModifiedProperties();

                            AssetDatabase.Refresh();
                        }
                    }

                    var suggestRect = GUILayoutUtility.GetRect(currentViewWidth, singleLineHeight);
                    suggestRect.x++;
                    suggestRect.width -= 4;

                    suggestor.OnSuggestionGUI = RenderSuggestion;
                    suggestor.OnSuggestGUI(suggestRect, "Dependency Search");
                    Space(2);
                }
            }

            switch (Event.current.type)
            {
                case EventType.Repaint:
                    dragDropRect = GUILayoutUtility.GetLastRect();
                    break;
                case EventType.DragUpdated:
                    if (dragDropRect.Contains(Event.current.mousePosition) && DragAndDrop.objectReferences.OfType<Manifest>().Any())
                    {
                        var canDrop = false;
                        var manifests = DragAndDrop.objectReferences.OfType<Manifest>().ToArray();

                        foreach (var droppedManifest in manifests)
                            foreach (var depThunderManifest in droppedManifest.Data.OfType<ThunderstoreManifest>())
                            {
                                string thisGuid = $"{thunderManifest.author}-{manifest.name}";
                                if (!depThunderManifest.dependencies.Any(dp => dp.StartsWith(thisGuid))
                                 && !thisGuid.StartsWith($"{depThunderManifest.author}-{droppedManifest.name}"))
                                {
                                    canDrop = true;
                                    break;
                                }
                                if (canDrop) break;
                            }
                        if (canDrop)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            Event.current.Use();
                            return;
                        }
                    }
                    break;
                case EventType.DragPerform:
                    if (DragAndDrop.objectReferences.OfType<Manifest>().Any())
                    {
                        //Debug.Log("Dropping Manifests");
                        var manifests = DragAndDrop.objectReferences.OfType<Manifest>();
                        foreach (var droppedManifest in manifests)
                            foreach (var dependence in droppedManifest.Data.OfType<ThunderstoreManifest>())
                            {
                                string dependency = $"{dependence.author}-{droppedManifest.name}-{dependence.versionNumber}";
                                if (thunderManifest.dependencies.Any(dp => dp.StartsWith($"{dependence.author}-{droppedManifest.name}")))
                                    thunderManifest.dependencies.RemoveAll(dp => dp.StartsWith($"{dependence.author}-{droppedManifest.name}"));

                                if (thunderManifest.dependencies == null || !thunderManifest.dependencies.Any())
                                    thunderManifest.dependencies = new DependencyList();

                                thunderManifest.dependencies.Add(dependency);
                                property.serializedObject.SetIsDifferentCacheDirty();
                                property.serializedObject.ApplyModifiedProperties();
                                DragAndDrop.AcceptDrag();
                                Event.current.Use();
                                return;
                            }
                    }
                    break;
            }

            bool RenderSuggestion(int arg1, Package package)
            {
                if (thunderManifest.dependencies.Contains(package.latest.full_name))
                    return false;

                if (Button(package.name))
                {
                    thunderManifest.dependencies.Add(package.latest.full_name);
                    property.serializedObject.SetIsDifferentCacheDirty();
                    property.serializedObject.ApplyModifiedProperties();
                    suggestor.Cleanup();

                    if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir);

                    var packages = RecurseDependencies(thunderManifest.dependencies)
                        .GroupBy(dep => dep.latest.full_name).Select(g => g.First()).ToArray();

                    foreach (var pack in packages)
                        ThunderstoreAPI.DownloadPackage(package, Path.Combine(TempDir, GetZipFileName(package)));

                    return true;
                }

                return false;
            }
        }

        private static string GetZipFileName(string package) => $"{package}.zip";
        private static string GetZipFileName(Package package) => GetZipFileName(package.latest.full_name);
        IEnumerable<Package> RecurseDependencies(IEnumerable<string> dependencies)
        {
            var deps = dependencies.SelectMany(dep => ThunderstoreAPI.LookupPackage(dep));
            var subDeps = deps.SelectMany(idep => idep.latest.dependencies).Distinct();

            if (subDeps.Any())
                return deps.Union(RecurseDependencies(subDeps));

            return deps;
        }

        private static IEnumerable<Package> EvaluateSuggestion(string searchString) => ThunderstoreAPI.LookupPackage(searchString);
    }
}