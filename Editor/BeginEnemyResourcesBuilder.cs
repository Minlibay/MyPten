#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Begin.Core;
using Begin.AI;

public static class BeginEnemyResourcesBuilder {
    [MenuItem("Tools/Begin/Create ▸ Enemy Runtime Resources")]
    public static void Create() {
        // bullet prefab (runtime)
        var bullet = new GameObject("Bullet");
        var col = bullet.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 0.15f;
        var rb = bullet.AddComponent<Rigidbody>(); rb.useGravity = false; rb.isKinematic = true;
        bullet.AddComponent<EnemyBullet>();

        // простая визуализация
        var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        vis.transform.SetParent(bullet.transform,false);
        vis.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
        var mr = vis.GetComponent<MeshRenderer>(); mr.material.color = new Color(1f,0.6f,0.1f,1f);

        // положим в Pool: ключ "bullet"
        Pool.Prewarm("bullet", bullet, 32);
        Object.DestroyImmediate(bullet); // исходник не нужен — в пуле уже есть

        EditorUtility.DisplayDialog("Begin","Enemy runtime resources prewarmed (bullet).", "OK");
    }
}
#endif
