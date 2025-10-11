#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Begin.Talents;

namespace Begin.EditorTools {
    public class TalentBatchGeneratorWindow : EditorWindow {
        const string DefaultCsvAssetPath = "Assets/docs/data/talents.csv";
        const string DefaultOutputFolder = "Assets/Resources/Talents/Expanded";
        const string DefaultTreeAssetPath = "Assets/Resources/Talents/CompleteTree.asset";

        TextAsset csvAsset;
        string outputFolder = DefaultOutputFolder;
        string treeAssetPath = DefaultTreeAssetPath;
        Vector2 scroll;

        [MenuItem("Tools/Talents/Batch Generator")]
        public static void OpenWindow() => GetWindow<TalentBatchGeneratorWindow>("Talent Batch Generator");

        void OnEnable() {
            if (csvAsset == null) {
                csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultCsvAssetPath);
            }
        }

        void OnGUI() {
            EditorGUILayout.LabelField("Источник данных", EditorStyles.boldLabel);
            csvAsset = (TextAsset)EditorGUILayout.ObjectField("CSV", csvAsset, typeof(TextAsset), false);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Параметры вывода", EditorStyles.boldLabel);
            outputFolder = EditorGUILayout.TextField("Папка TalentNode", outputFolder);
            treeAssetPath = EditorGUILayout.TextField("Файл TalentTree", treeAssetPath);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Ожидаемый CSV-формат: id;title;description;type;maxRank;values;requirements" +
                "\nvalues разделяются символом |, requirements задаются в виде id:rank через |.", MessageType.Info);

            using (new EditorGUI.DisabledScope(csvAsset == null)) {
                if (GUILayout.Button("Generate/Update Assets")) {
                    Generate();
                }
            }

            EditorGUILayout.Space();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (csvAsset != null) {
                EditorGUILayout.LabelField("Предпросмотр CSV", EditorStyles.boldLabel);
                var previewLines = csvAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Take(10);
                foreach (var line in previewLines) {
                    EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void Generate() {
            if (csvAsset == null) {
                EditorUtility.DisplayDialog("Talent Generator", "Не выбран CSV-файл.", "OK");
                return;
            }

            try {
                var records = TalentCsvParser.Parse(csvAsset.text);
                if (records.Count == 0) {
                    EditorUtility.DisplayDialog("Talent Generator", "CSV не содержит валидных строк.", "OK");
                    return;
                }

                var createdNodes = GenerateNodes(records);
                var tree = GenerateTree(createdNodes);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Talent Generator",
                    $"Генерация завершена. Узлов: {createdNodes.Count}, дерево: {(tree ? tree.name : "—")}", "OK");
            } catch (Exception ex) {
                Debug.LogError($"Talent generator failed: {ex}");
                EditorUtility.DisplayDialog("Talent Generator", "Ошибка при обработке CSV. Подробности в консоли.", "OK");
            }
        }

        List<TalentNode> GenerateNodes(List<TalentCsvRecord> records) {
            EnsureFolder(outputFolder);
            var nodes = new List<TalentNode>(records.Count);

            for (int i = 0; i < records.Count; i++) {
                var record = records[i];
                string assetPath = Path.Combine(outputFolder, record.Id + ".asset").Replace('\\', '/');
                var node = AssetDatabase.LoadAssetAtPath<TalentNode>(assetPath);
                bool isNew = node == null;
                if (isNew) {
                    node = ScriptableObject.CreateInstance<TalentNode>();
                    AssetDatabase.CreateAsset(node, assetPath);
                }

                Undo.RecordObject(node, "Update Talent Node");
                node.id = record.Id;
                node.title = record.Title;
                node.description = record.Description;
                node.type = record.Type;
                node.maxRank = record.MaxRank;
                node.valuesPerRank = record.Values;
                node.requirements.Clear();
                node.requirements.AddRange(record.Requirements.Select(req => new TalentRequirement {
                    nodeId = req.nodeId,
                    requiredRank = req.requiredRank
                }));
                EditorUtility.SetDirty(node);
                nodes.Add(node);
            }

            return nodes;
        }

        TalentTree GenerateTree(List<TalentNode> nodes) {
            EnsureFolder(Path.GetDirectoryName(treeAssetPath));
            var tree = AssetDatabase.LoadAssetAtPath<TalentTree>(treeAssetPath);
            if (tree == null) {
                tree = ScriptableObject.CreateInstance<TalentTree>();
                AssetDatabase.CreateAsset(tree, treeAssetPath);
            }

            Undo.RecordObject(tree, "Update Talent Tree");
            tree.nodes = new List<TalentNode>(nodes);
            EditorUtility.SetDirty(tree);
            return tree;
        }

        static void EnsureFolder(string assetPath) {
            if (string.IsNullOrEmpty(assetPath)) return;
            var normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith("Assets")) return;
            var dirs = normalized.Split('/');
            string current = dirs[0];
            for (int i = 1; i < dirs.Length; i++) {
                string next = current + "/" + dirs[i];
                if (!AssetDatabase.IsValidFolder(next)) {
                    AssetDatabase.CreateFolder(current, dirs[i]);
                }
                current = next;
            }
        }

        class TalentCsvRecord {
            public string Id;
            public string Title;
            public string Description;
            public TalentType Type;
            public int MaxRank;
            public float[] Values;
            public List<(string nodeId, int requiredRank)> Requirements;
        }

        static class TalentCsvParser {
            public static List<TalentCsvRecord> Parse(string csv) {
                var list = new List<TalentCsvRecord>();
                using var reader = new StringReader(csv);
                string line;
                bool isHeader = true;
                while ((line = reader.ReadLine()) != null) {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (isHeader) { isHeader = false; continue; }
                    var parts = line.Split(';');
                    if (parts.Length < 6) continue;

                    var record = new TalentCsvRecord {
                        Id = parts[0].Trim(),
                        Title = parts.Length > 1 ? parts[1].Trim() : string.Empty,
                        Description = parts.Length > 2 ? parts[2].Trim() : string.Empty,
                        Type = ParseType(parts.Length > 3 ? parts[3].Trim() : string.Empty),
                        MaxRank = ParseInt(parts.Length > 4 ? parts[4].Trim() : string.Empty, 3),
                        Values = ParseValues(parts.Length > 5 ? parts[5] : string.Empty)
                    };

                    record.Requirements = ParseRequirements(parts.Length > 6 ? parts[6] : string.Empty);
                    if (record.MaxRank <= 0 || record.Values.Length == 0) {
                        Debug.LogWarning($"Talent CSV: пропущена строка {record.Id} — maxRank или values пустые");
                        continue;
                    }
                    if (record.Values.Length != record.MaxRank) {
                        Array.Resize(ref record.Values, record.MaxRank);
                    }
                    list.Add(record);
                }
                return list;
            }

            static TalentType ParseType(string value) {
                if (Enum.TryParse(value, out TalentType result)) return result;
                Debug.LogWarning($"Talent CSV: неизвестный тип '{value}', используется MaxHP по умолчанию");
                return TalentType.MaxHP;
            }

            static int ParseInt(string value, int fallback) {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
                return fallback;
            }

            static float[] ParseValues(string raw) {
                var parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var values = new float[Math.Max(1, parts.Length)];
                for (int i = 0; i < parts.Length; i++) {
                    if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) {
                        f = 0f;
                    }
                    values[i] = f;
                }
                if (parts.Length == 0) values[0] = 0f;
                return values;
            }

            static List<(string nodeId, int requiredRank)> ParseRequirements(string raw) {
                var list = new List<(string nodeId, int requiredRank)>();
                if (string.IsNullOrWhiteSpace(raw)) return list;
                var chunks = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var chunk in chunks) {
                    var pair = chunk.Split(':');
                    if (pair.Length != 2) continue;
                    var id = pair[0].Trim();
                    if (string.IsNullOrEmpty(id)) continue;
                    int rank = ParseInt(pair[1].Trim(), 1);
                    list.Add((id, rank));
                }
                return list;
            }
        }
    }
}
#endif
