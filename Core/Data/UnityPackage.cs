﻿using System;
using System.IO;
using System.Linq;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThunderKit.Core.Data
{
    using static ScriptableHelper;
    [Flags]
    public enum IncludedSettings
    {
        AudioManager = 1,
        ClusterInputManager = 2,
        DynamicsManager = 4,
        EditorBuildSettings = 8,
        EditorSettings = 16,
        GraphicsSettings = 32,
        InputManager = 64,
        NavMeshAreas = 128,
        NetworkManager = 256,
        Physics2DSettings = 512,
        PresetManager = 1024,
        ProjectSettings = 2048,
        QualitySettings = 4096,
        TagManager = 8192,
        TimeManager = 16384,
        UnityConnectSettings = 32768,
        VFXManager = 65536,
        XRSettings = 131072
    }

    public class UnityPackage : ScriptableObject
    {
        const string ExportMenuPath = Constants.ThunderKitContextRoot + "Compile " + nameof(UnityPackage);

        [EnumFlag]
        public IncludedSettings IncludedSettings;

        public bool recurseDirectories;

        public Object[] AssetFiles;

        [MenuItem(Constants.ThunderKitContextRoot + nameof(UnityPackage), false, priority = Constants.ThunderKitMenuPriority)]
        public static void Create()
        {
            SelectNewAsset<UnityPackage>();
        }

        [MenuItem(ExportMenuPath, true, priority = Constants.ThunderKitMenuPriority)]
        public static bool CanExport() => Selection.activeObject is UnityPackage;

        [MenuItem(ExportMenuPath, false, priority = Constants.ThunderKitMenuPriority)]
        public static void Export()
        {
            if (!(Selection.activeObject is UnityPackage redist)) return;
            Export(redist, "Deployments");
        }

        public static void Export(UnityPackage redist, string path)
        {

            var assetPaths = redist.AssetFiles.Select(af => AssetDatabase.GetAssetPath(af));
            if (redist.recurseDirectories)
            {
                var folderPaths = assetPaths.Where(AssetDatabase.IsValidFolder).ToArray();
                var exceptFolderPaths = assetPaths.Except(folderPaths);
                var recursedPaths = folderPaths.SelectMany(p => Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories));
                assetPaths = exceptFolderPaths.Union(recursedPaths);
            }

            var additionalAssets = redist.IncludedSettings.GetFlags().Select(flag => $"ProjectSettings/{flag}.asset");

            assetPaths = assetPaths.Concat(additionalAssets);

            string[] assetPathNames = assetPaths.ToArray();
            string fileName = Path.Combine(path, $"{redist.name}.unityPackage");
            string metaFileName = Path.Combine(path, $"{redist.name}.unityPackage.meta");
            if (File.Exists(fileName)) File.Delete(fileName);
            if (File.Exists(metaFileName)) File.Delete(metaFileName);
            AssetDatabase.ExportPackage(assetPathNames, fileName);
        }
    }
}