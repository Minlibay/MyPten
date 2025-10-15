#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Begin.Talents.Editor {
    /// <summary>
    /// Cleans up legacy TalentRequirement ScriptableObjects and migrates their data
    /// into the inline requirement list on the owning TalentNode assets.
    /// </summary>
    static class TalentRequirementCleanup {
        const string SessionKey = "Begin.Talents.RequirementCleanup.v2";

        [InitializeOnLoadMethod]
        static void RunOnce() {
            EditorApplication.delayCall += () => {
                if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (SessionState.GetBool(SessionKey, false)) return;
                SessionState.SetBool(SessionKey, true);
                CleanupLegacyRequirements();
            };
        }

        static void CleanupLegacyRequirements() {
            var migratedNodes = new List<string>();
            var removedAssets = new HashSet<string>();

            try {
                var nodeGuids = AssetDatabase.FindAssets("t:Begin.Talents.TalentNode");
                foreach (var guid in nodeGuids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var node = AssetDatabase.LoadAssetAtPath<TalentNode>(path);
                    if (node == null) continue;

                    var serializedNode = new SerializedObject(node);
                    var requirementsProp = serializedNode.FindProperty("_requirements") ?? serializedNode.FindProperty("requirements");
                    var existing = new HashSet<(string nodeId, int rank)>();

                    if (requirementsProp != null && requirementsProp.isArray) {
                        for (int i = 0; i < requirementsProp.arraySize; i++) {
                            var entry = requirementsProp.GetArrayElementAtIndex(i);
                            var nodeId = entry.FindPropertyRelative("nodeId")?.stringValue ?? string.Empty;
                            var rank = entry.FindPropertyRelative("requiredRank")?.intValue ?? 1;
                            if (!string.IsNullOrEmpty(nodeId)) {
                                existing.Add((nodeId, rank));
                            }
                        }
                    }

                    bool changed = false;
                    changed |= MigrateLegacyList(serializedNode, requirementsProp, existing, "legacyRequirementAssets", removedAssets);
                    changed |= MigrateLegacyList(serializedNode, requirementsProp, existing, "legacyPrereqAssets", removedAssets);

                    if (changed) {
                        serializedNode.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(node);
                        migratedNodes.Add(path);
                    }
                }

                // Sweep Resources/Talents for stray assets that still reference the legacy scripts.
                var talentsRoot = Path.Combine("Assets", "Resources", "Talents");
                if (Directory.Exists(talentsRoot)) {
                    foreach (var assetPath in Directory.GetFiles(talentsRoot, "*.asset", SearchOption.AllDirectories)) {
                        var unityPath = assetPath.Replace(Path.DirectorySeparatorChar, '/');
                        var text = File.ReadAllText(assetPath);
                        if (!text.Contains("TalentNode") && !text.Contains("TalentTree")) {
                            if (AssetDatabase.DeleteAsset(unityPath)) {
                                removedAssets.Add(unityPath);
                            }
                        }
                    }
                }

                if (migratedNodes.Count > 0 || removedAssets.Count > 0) {
                    var summary = $"Talent requirement cleanup migrated {migratedNodes.Count} node(s)";
                    if (removedAssets.Count > 0) summary += $", removed {removedAssets.Count} orphaned asset(s)";
                    Debug.Log(summary + ". Inline requirement lists are now authoritative.");
                    AssetDatabase.SaveAssets();
                }
            }
            catch (Exception ex) {
                Debug.LogError($"Talent requirement cleanup failed: {ex}");
            }
        }

        static bool MigrateLegacyList(SerializedObject serializedNode,
                                      SerializedProperty requirementsProp,
                                      HashSet<(string nodeId, int rank)> existing,
                                      string propertyName,
                                      HashSet<string> removedAssets) {
            var legacyProp = serializedNode.FindProperty(propertyName);
            if (legacyProp == null || !legacyProp.isArray || legacyProp.arraySize == 0) return false;

            bool changed = false;

            for (int i = 0; i < legacyProp.arraySize; i++) {
                var element = legacyProp.GetArrayElementAtIndex(i);
                var prerequisite = element.objectReferenceValue as TalentPrereq;
                if (prerequisite != null && !string.IsNullOrEmpty(prerequisite.nodeId)) {
                    var key = (prerequisite.nodeId, Mathf.Max(1, prerequisite.requiredRank));
                    if (existing.Add(key)) {
                        AppendRequirement(requirementsProp, key.nodeId, key.rank);
                        changed = true;
                    }
                    var assetPath = AssetDatabase.GetAssetPath(prerequisite);
                    if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.DeleteAsset(assetPath)) {
                        removedAssets.Add(assetPath);
                    }
                }
            }

            for (int i = legacyProp.arraySize - 1; i >= 0; i--) {
                legacyProp.DeleteArrayElementAtIndex(i);
                if (i < legacyProp.arraySize) {
                    legacyProp.DeleteArrayElementAtIndex(i);
                }
            }
            return changed;
        }

        static void AppendRequirement(SerializedProperty requirementsProp, string nodeId, int rank) {
            if (requirementsProp == null) return;
            var index = requirementsProp.arraySize;
            requirementsProp.InsertArrayElementAtIndex(index);
            var element = requirementsProp.GetArrayElementAtIndex(index);
            var nodeIdProp = element.FindPropertyRelative("nodeId");
            var rankProp = element.FindPropertyRelative("requiredRank");
            if (nodeIdProp != null) nodeIdProp.stringValue = nodeId;
            if (rankProp != null) rankProp.intValue = Mathf.Max(1, rank);
        }
    }
}
#endif
