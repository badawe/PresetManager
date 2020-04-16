﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace BrunoMikoski.PresetManager
{
    public static class PresetManagerUtils
    {
        private static List<Preset> projectPresets;
        private static List<Preset> ProjectPresets
        {
            get
            {
                if (projectPresets == null)
                    LoadProjectPresets();
                return projectPresets;
            }
        }

        private static void LoadProjectPresets()
        {
            string[] presetsGUIDs = AssetDatabase.FindAssets("t:Preset");

            projectPresets = new List<Preset>();
            for (var i = 0; i < presetsGUIDs.Length; i++)
            {
                projectPresets.Add(
                    AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(presetsGUIDs[i])));
            }
        }

        public static Preset[] GetAvailablePresetsForAssetImporter(AssetImporter assetImporter)
        {
            List<Preset> resultPresets = new List<Preset>();
            for (int i = 0; i < ProjectPresets.Count; i++)
            {
                Preset preset = ProjectPresets[i];
                if (!preset.ApplyTo(assetImporter))
                    continue;
                
                resultPresets.Add(preset);
            }

            return resultPresets.ToArray();
        }

        public static bool HasAnyPresetForFolder(string relativeFolderPath)
        {
            return PresetManagerStorage.Instance.HasAnyPresetForFolder(relativeFolderPath);
        }
        
        public static bool HasPresetFor(AssetImporter assetImporter)
        {
            for (int i = 0; i < ProjectPresets.Count; i++)
            {
                Preset preset = ProjectPresets[i];
                if (!preset.ApplyTo(assetImporter))
                    continue;

                return true;
            }

            return false;
        }

        public static bool TryGetAssetPresetFromFolder(string relativeFolderPath, AssetImporter assetImporter,
            out PresetData preset)
        {
            return PresetManagerStorage.Instance.TryGetAssetPresetFromFolder(relativeFolderPath, assetImporter, out preset);
        }

        public static void SetPresetForFolder(string relativeFolderPath, Preset preset)
        {
            PresetManagerStorage.Instance.SetPresetForFolder(relativeFolderPath, preset);
        }

        public static void ClearPresetForFolder(string relativeFolderPath)
        {
            PresetManagerStorage.Instance.ClearPresetForFolder(relativeFolderPath);
        }
        
        public static void ClearAllPresetsForFolder(string relativeFolderPath)
        {
            PresetManagerStorage.Instance.ClearAllPresetForFolder(relativeFolderPath);
        }
        
        public static void ProjectPresetsChanged()
        {
            projectPresets = null;
        }

        public static bool TryToGetParentPresetSettings(string relativeFolderPath, AssetImporter assetImporter,
            out string relativeParentPath)
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(RelativeToAbsolutePath(relativeFolderPath)).Parent;

            relativeParentPath = string.Empty;
            while (currentDirectory != null && !string.Equals(currentDirectory.FullName, Directory.GetCurrentDirectory(), StringComparison.Ordinal))
            {
                if (PresetManagerStorage.Instance.TryGetPresetFolderPathFromFolder(AbsoluteToRelativePath(currentDirectory.FullName),
                    assetImporter, out string ownerFolderPath))
                {
                    relativeParentPath = ownerFolderPath;
                    break;
                }
                currentDirectory = currentDirectory.Parent;
            }

            return !string.IsNullOrEmpty(relativeParentPath);
        }
        
        public static bool TryToGetParentPresetSettings(string relativeFolderPath, AssetImporter assetImporter,
            out PresetData preset)
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(RelativeToAbsolutePath(relativeFolderPath)).Parent;

            preset = default;
            while (currentDirectory != null && !string.Equals(currentDirectory.FullName, Directory.GetCurrentDirectory(), StringComparison.Ordinal))
            {
                if (PresetManagerStorage.Instance.TryGetAssetPresetFromFolder(AbsoluteToRelativePath(currentDirectory.FullName),
                    assetImporter, out preset))
                    break;
                currentDirectory = currentDirectory.Parent;
            }

            return preset.Preset != null;
        }
        
        
        public static string AbsoluteToRelativePath(string absoluteFilePath)
        {
            return "Assets" + absoluteFilePath.Substring(Application.dataPath.Length);
        }

        public static string RelativeToAbsolutePath(string relativeFilePath)
        {
            return Path.GetFullPath(relativeFilePath);
        }

        public static void ApplySettingsToAsset(string relativeFolderPath, AssetImporter assetImporter)
        {
            if (TryGetAssetPresetFromFolder(relativeFolderPath, assetImporter, out PresetData preset))
            {
                if (preset.Preset.ApplyTo(assetImporter, preset.TargetParameters))
                {
                    EditorUtility.SetDirty(assetImporter);
                }
            }
            else
            {
                if(TryToGetParentPresetSettings(relativeFolderPath, assetImporter, out preset))
                {
                    if (preset.Preset.ApplyTo(assetImporter, preset.TargetParameters))
                    {
                        EditorUtility.SetDirty(assetImporter);
                    }
                }
            }
        }
        
        public static void ApplyPresetsToFolder(string relativeFolderPath)
        {
            projectPresets = null;
            string[] assetPaths = GetAllAssetsAtDirectory(relativeFolderPath);
            for (int i = 0; i < assetPaths.Length; i++)
            {
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetPaths[i]);
                ApplySettingsToAsset(relativeFolderPath, assetImporter);
            }

            string[] subFolder = AssetDatabase.GetSubFolders(relativeFolderPath);
            for (var i = 0; i < subFolder.Length; i++)
            {
                string subFolderPath = subFolder[i];
                ApplyPresetsToFolder(subFolderPath);
            }
        }


        public static string[] GetAllAssetsAtDirectory(string relativeDirectoryPath)
        {
            string[] fileEntries = Directory.GetFiles(RelativeToAbsolutePath(relativeDirectoryPath));
            List<string> resuts = new List<string>();

            for (var i = 0; i < fileEntries.Length; i++)
            {
                string fileEntry = fileEntries[i];
                if (fileEntry.EndsWith(".meta"))
                    continue;

                resuts.Add(AbsoluteToRelativePath(fileEntry));
            }

            return resuts.ToArray();
        }
    }
}