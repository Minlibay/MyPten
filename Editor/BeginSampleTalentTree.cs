#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Begin.Talents;

public static class BeginSampleTalentTree {
    [MenuItem("Tools/Begin/Create ▸ Sample Talent Tree")]
    public static void Create() {
        Ensure("Assets/Resources/Talents");

        TalentNode mk(string id, string title, TalentType t, int max, float[] vals, params (string,int)[] req) {
            var so = ScriptableObject.CreateInstance<TalentNode>();
            so.id = id; so.title = title; so.type = t; so.maxRank = max; so.valuesPerRank = vals;
            foreach (var r in req) {
                so.requirements.Add(new TalentRequirement {
                    nodeId = r.Item1,
                    requiredRank = r.Item2
                });
            }
            AssetDatabase.CreateAsset(so, $"Assets/Resources/Talents/{id}.asset");
            return so;
        }

        var hp1 = mk("hp_1","Живучесть I", TalentType.MaxHP, 3, new float[]{10,20,30});
        var hp2 = mk("hp_2","Живучесть II", TalentType.MaxHP, 2, new float[]{40,60}, ("hp_1",3));
        var dmg = mk("dmg_1","Сила удара", TalentType.Damage, 3, new float[]{5,10,15});
        var gold= mk("gold_1","Жадность", TalentType.GoldGain, 3, new float[]{10,20,30});
        var drop= mk("drop_1","Коллекционер", TalentType.ItemDropChance, 2, new float[]{5,10});
        var disc= mk("disc_1","Торгаш", TalentType.VendorDiscount, 2, new float[]{5,10});
        var spd = mk("spd_1","Лёгкие ноги", TalentType.MoveSpeed, 3, new float[]{5,10,15});

        var tree = ScriptableObject.CreateInstance<TalentTree>();
        tree.nodes.AddRange(new[]{hp1,hp2,dmg,gold,drop,disc,spd});
        AssetDatabase.CreateAsset(tree, "Assets/Resources/Talents/SampleTree.asset");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Begin", "Создан SampleTree с узлами в Resources/Talents", "OK");
    }

    static void Ensure(string path) {
        if (!AssetDatabase.IsValidFolder(path)) {
            var parts = path.Split('/'); string acc = parts[0];
            for (int i=1;i<parts.Length;i++){ var next=$"{acc}/{parts[i]}"; if(!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(acc, parts[i]); acc=next; }
        }
    }
}
#endif
