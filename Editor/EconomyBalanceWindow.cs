using UnityEditor;
using UnityEngine;
using Begin.Economy;

namespace Begin.EditorTools {
    public class EconomyBalanceWindow : EditorWindow {
        EconomyBalance asset;
        SerializedObject serializedAsset;
        Vector2 scroll;

        [MenuItem("Begin/Balance/Economy Balance")] public static void Open() {
            GetWindow<EconomyBalanceWindow>("Economy Balance");
        }

        void OnEnable() {
            TryLoadAsset();
        }

        void TryLoadAsset() {
            if (asset != null) return;
            asset = Resources.Load<EconomyBalance>("Balance/EconomyBalance");
            if (!asset) {
                var path = EditorUtility.SaveFilePanelInProject("Create Economy Balance", "EconomyBalance", "asset", "Pick a save location for the economy balance asset.");
                if (!string.IsNullOrEmpty(path)) {
                    asset = CreateInstance<EconomyBalance>();
                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.SaveAssets();
                }
            }

            if (asset) serializedAsset = new SerializedObject(asset);
        }

        void OnGUI() {
            if (!asset) {
                EditorGUILayout.HelpBox("Balance asset not found in Resources/Balance. Use the button below to create one and move it into the Resources folder.", MessageType.Info);
                if (GUILayout.Button("Create Balance Asset")) {
                    asset = CreateInstance<EconomyBalance>();
                    var path = EditorUtility.SaveFilePanelInProject("Create Economy Balance", "EconomyBalance", "asset", "Pick a save location for the economy balance asset.");
                    if (!string.IsNullOrEmpty(path)) {
                        AssetDatabase.CreateAsset(asset, path);
                        AssetDatabase.SaveAssets();
                        serializedAsset = new SerializedObject(asset);
                    }
                }
                return;
            }

            serializedAsset.Update();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Vendor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedAsset.FindProperty("defaultVendorStock"));
            EditorGUILayout.PropertyField(serializedAsset.FindProperty("rarityPricing"), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedAsset.FindProperty("globalGoldMultiplier"));
            EditorGUILayout.PropertyField(serializedAsset.FindProperty("globalXpMultiplier"));
            EditorGUILayout.PropertyField(serializedAsset.FindProperty("vendorSellFallbackMultiplier"));
            EditorGUILayout.EndScrollView();

            if (serializedAsset.ApplyModifiedProperties()) {
                EditorUtility.SetDirty(asset);
            }

            if (!AssetDatabase.IsMainAsset(asset)) return;
            if (!AssetDatabase.GetAssetPath(asset).Contains("Resources/")) {
                EditorGUILayout.HelpBox("Asset should live under Resources/Balance to be picked up at runtime.", MessageType.Warning);
                if (GUILayout.Button("Move to Resources/Balance")) {
                    var target = "Assets/Resources/Balance";
                    if (!AssetDatabase.IsValidFolder(target)) {
                        var parent = "Assets/Resources";
                        if (!AssetDatabase.IsValidFolder(parent)) {
                            AssetDatabase.CreateFolder("Assets", "Resources");
                        }
                        AssetDatabase.CreateFolder(parent, "Balance");
                    }

                    var newPath = AssetDatabase.GenerateUniqueAssetPath(target + "/" + asset.name + ".asset");
                    AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(asset), newPath);
                }
            }
        }
    }
}
