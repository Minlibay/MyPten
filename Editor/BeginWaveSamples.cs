#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Begin.Enemies;
using Begin.AI;

public static class BeginWaveSamples {
    [MenuItem("Tools/Begin/Create ▸ Sample Enemies & Waves")]
    public static void CreateAll() {
        Ensure("Assets/Resources/Enemies");
        Ensure("Assets/Resources/Waves");

        // Визуальные префабы: примитивы
        GameObject Vis(string name, Color c, Vector3 scale) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mr.sharedMaterial.color = c;
            return go;
        }

        // Создаём EnemyDefinition
        EnemyDefinition Mk(string id, string title, Color col, float hp, float spd, float tdmg, int xp, int gmin, int gmax, float drop) {
            var so = ScriptableObject.CreateInstance<EnemyDefinition>();
            so.id = id; so.displayName = title;
            so.maxHP = hp; so.moveSpeed = spd; so.touchDamage = tdmg;
            so.xpPerKill = xp; so.goldMin = gmin; so.goldMax = gmax; so.itemDropChance = drop;
            var vis = Vis("VIS_"+id, col, id.Contains("tank") ? new Vector3(1.4f,1.4f,1.4f) : Vector3.one);
            so.prefab = vis;
            AssetDatabase.CreateAsset(so, $"Assets/Resources/Enemies/{id}.asset");
            return so;
        }

        var runner = Mk("runner", "Быстрый", new Color(0.3f,0.9f,0.3f), 60, 4.2f, 8, 6, 1, 3, 0.12f);
        var tank   = Mk("tank",   "Танк",     new Color(0.9f,0.4f,0.4f), 200, 2.2f, 16, 12, 2, 5, 0.18f);
        var shoot  = Mk("shooter","Стрелок",  new Color(0.4f,0.6f,1.0f), 90, 3.2f, 0, 8, 1, 3, 0.15f);

        // WaveTable
        var table = ScriptableObject.CreateInstance<WaveTable>();
        table.waves.Add(new WaveRow{ entries = { new WaveEntry{ enemy=runner, count=6 } }});
        table.waves.Add(new WaveRow{ entries = { new WaveEntry{ enemy=runner, count=6 }, new WaveEntry{enemy=shoot, count=2}}});
        table.waves.Add(new WaveRow{ entries = { new WaveEntry{ enemy=runner, count=8 }, new WaveEntry{enemy=shoot, count=3}}});
        table.waves.Add(new WaveRow{ entries = { new WaveEntry{ enemy=tank,   count=3 }, new WaveEntry{enemy=shoot, count=4}}});
        table.waves.Add(new WaveRow{ entries = { new WaveEntry{ enemy=tank,   count=4 }, new WaveEntry{enemy=shoot, count=5}, new WaveEntry{enemy=runner,count=6}}});

        AssetDatabase.CreateAsset(table, "Assets/Resources/Waves/SampleWaves.asset");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Begin","Созданы 3 EnemyDefinition и WaveTable в Resources.", "OK");
    }

    static void Ensure(string path) {
        if (!AssetDatabase.IsValidFolder(path)) {
            var parts = path.Split('/'); string acc = parts[0];
            for (int i=1;i<parts.Length;i++){ var next=$"{acc}/{parts[i]}"; if(!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(acc, parts[i]); acc=next; }
        }
    }
}
#endif
