using System.IO;
using UnityEditor;
using UnityEngine;
using Begin.Economy;

namespace Begin.EditorTools {
    public class DropTableEditorWindow : EditorWindow {
        DropTable table;
        SerializedObject serializedTable;
        Vector2 scroll;

        [MenuItem("Begin/Balance/Drop Table Editor")] public static void Open() {
            GetWindow<DropTableEditorWindow>("Drop Tables");
        }

        void OnEnable() {
            if (table != null) serializedTable = new SerializedObject(table);
        }

        void OnGUI() {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Drop Table", GUILayout.Width(80));
            var newTable = (DropTable)EditorGUILayout.ObjectField(table, typeof(DropTable), false);
            if (newTable != table) {
                table = newTable;
                serializedTable = table ? new SerializedObject(table) : null;
            }

            if (GUILayout.Button("Create", GUILayout.Width(70))) {
                var path = EditorUtility.SaveFilePanelInProject("Create Drop Table", "DropTable", "asset", "Pick where to store the drop table asset");
                if (!string.IsNullOrEmpty(path)) {
                    table = CreateInstance<DropTable>();
                    AssetDatabase.CreateAsset(table, path);
                    AssetDatabase.SaveAssets();
                    serializedTable = new SerializedObject(table);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!table) {
                EditorGUILayout.HelpBox("Assign or create a drop table asset to edit entries.", MessageType.Info);
                return;
            }

            serializedTable.Update();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.PropertyField(serializedTable.FindProperty("entries"), true);
            EditorGUILayout.EndScrollView();

            if (serializedTable.ApplyModifiedProperties()) {
                EditorUtility.SetDirty(table);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Sync IDs from Definitions")) {
                    foreach (var entry in table.entries) {
                        entry?.SyncIdFromDefinition();
                    }
                    EditorUtility.SetDirty(table);
                }

                if (GUILayout.Button("Import CSV")) {
                    var csvPath = EditorUtility.OpenFilePanel("Import Drop Table CSV", Application.dataPath, "csv");
                    if (!string.IsNullOrEmpty(csvPath)) {
                        var text = File.ReadAllText(csvPath);
                        var textAsset = new TextAsset(text);
                        table.ImportCsv(textAsset);
                        serializedTable = new SerializedObject(table);
                        EditorUtility.SetDirty(table);
                    }
                }
            }
        }
    }
}
