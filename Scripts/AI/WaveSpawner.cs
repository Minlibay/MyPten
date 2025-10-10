using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Begin.Enemies;
using Begin.Core;
using Begin.Combat; // для Health

namespace Begin.AI {
    public class WaveSpawner : MonoBehaviour {
        [Header("Setup")]
        public WaveTable table;
        public Transform player;
        public Vector2 arenaMin = new Vector2(-10, -10);
        public Vector2 arenaMax = new Vector2( 10,  10);
        public float delayBetweenWaves = 2f;

        [Header("Events")]
        public System.Action onAllCleared;
        public System.Action<int,int> onWaveChanged; // (current, total)

        public int totalWaves => table ? table.waves.Count : 0;

        readonly List<GameObject> alive = new();
        static readonly HashSet<string> _prewarmed = new();

        void Start() {
            if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
            StartCoroutine(Run());
        }

        IEnumerator Run() {
            if (!table) yield break;

            int total = table.waves.Count;
            for (int i = 0; i < total; i++) {
                // сообщаем HUD о смене волны
                onWaveChanged?.Invoke(i + 1, total);

                // начинаем волну с чистого списка живых
                alive.Clear();

                var row = table.waves[i];

                // Спавним записи ряда
                foreach (var e in row.entries) {
                    if (!e.enemy || e.count <= 0 || !e.enemy.prefab) continue;
                    for (int k = 0; k < e.count; k++) {
                        var pos = RandomPos();
                        var go = SpawnEnemy(e.enemy, pos);
                        if (go) alive.Add(go);
                    }
                }

                // ждём, пока все живые из этой волны умрут/деспавнятся
                while (alive.Exists(go => go != null && go.activeInHierarchy)) {
                    yield return null;
                }

                // пауза между волнами
                yield return new WaitForSeconds(delayBetweenWaves);
            }

            onAllCleared?.Invoke();
        }

        Vector3 RandomPos() {
            float x = Random.Range(arenaMin.x, arenaMax.x);
            float z = Random.Range(arenaMin.y, arenaMax.y); // правильный диапазон по Z
            return new Vector3(x, 0f, z);
        }

        GameObject SpawnEnemy(EnemyDefinition def, Vector3 pos) {
            // Пулл: ключ по id врага
            string key = "enemy_" + def.id;
            if (!_prewarmed.Contains(key)) PrewarmEnemy(def);

            var go = Pool.Spawn(key, pos, Quaternion.identity);
            if (!go) return null;

            // назначаем логику/цель
            var stats = go.GetComponent<EnemyStats>();
            if (stats) stats.def = def; // Awake/Reset применит статы

            if (go.TryGetComponent<EnemyBase>(out var ai)) ai.Init(player);

            return go;
        }

        void PrewarmEnemy(EnemyDefinition def) {
            var prefab = BuildRuntimePrefab(def);
            Pool.Prewarm("enemy_" + def.id, prefab, 12);
            _prewarmed.Add("enemy_" + def.id);
        }

        // создаём Runtime-префаб на базе визуального, добавляя необходимые компоненты
        GameObject BuildRuntimePrefab(EnemyDefinition def) {
            var root = new GameObject(def.displayName);

            // визуал как child
            if (def.prefab) {
                var vis = Instantiate(def.prefab);
                vis.transform.SetParent(root.transform, false);
            }

            // контроллер для передвижения
            var cc = root.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.4f; cc.center = new Vector3(0, 1, 0);

            // Мотор (вращает только визуал-ребёнка)
            var motor = root.AddComponent<EnemyMotor>();
            motor.model = (root.transform.childCount > 0) ? root.transform.GetChild(0) : root.transform;
            motor.stoppingDistance = 1.2f;
            motor.turnSpeed = 540f;

            // здоровье
            var health = root.AddComponent<Health>();

            // статы и поведение
            var stats = root.AddComponent<EnemyStats>(); stats.def = def;
            if (def.id.Contains("runner")) root.AddComponent<EnemyRunner>();
            else if (def.id.Contains("tank")) root.AddComponent<EnemyTank>();
            else root.AddComponent<EnemyShooter>(); // по умолчанию — шутер

            // лут/награда
            root.AddComponent<EnemyLoot>();

            // при смерти — убрать из alive и вернуть в пул
            health.onDeath += () => { StartCoroutine(DespawnNextFrame(root)); };

            return root;
        }

        IEnumerator DespawnNextFrame(GameObject go) {
            yield return null;
            alive.Remove(go);
            Pool.Despawn(go);
        }
    }
}
