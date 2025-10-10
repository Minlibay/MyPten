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

        public int totalWaves => table ? table.TotalWaves : 0;

        readonly List<GameObject> alive = new();
        static readonly Dictionary<string,int> _poolCapacities = new();

        void Start() {
            if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
            StartCoroutine(Run());
        }

        IEnumerator Run() {
            if (!table) yield break;

            PreparePools();

            int total = table.TotalWaves;
            for (int i = 0; i < total; i++) {
                // сообщаем HUD о смене волны
                onWaveChanged?.Invoke(i + 1, total);

                // начинаем волну с чистого списка живых
                alive.Clear();

                if (i < table.waves.Count) {
                    SpawnEntries(table.waves[i].entries);
                } else {
                    SpawnEntries(table.bossSupport);
                    if (table.finalBoss) {
                        var boss = SpawnEnemy(table.finalBoss, RandomPos());
                        if (boss) alive.Add(boss);
                    }
                }

                // ждём, пока все живые из этой волны умрут/деспавнятся
                while (alive.Exists(IsAlive)) {
                    yield return null;
                }

                // пауза между волнами
                if (i < total - 1) yield return new WaitForSeconds(delayBetweenWaves);
            }

            onAllCleared?.Invoke();
        }

        void PreparePools() {
            var required = new Dictionary<EnemyDefinition, int>();

            void Accumulate(IEnumerable<WaveEntry> entries) {
                if (entries == null) return;
                var perWave = new Dictionary<EnemyDefinition, int>();
                foreach (var e in entries) {
                    if (!e.enemy || e.count <= 0) continue;
                    perWave.TryGetValue(e.enemy, out var cur);
                    perWave[e.enemy] = cur + e.count;
                }
                foreach (var kv in perWave) {
                    if (required.TryGetValue(kv.Key, out var existing)) required[kv.Key] = Mathf.Max(existing, kv.Value);
                    else required[kv.Key] = kv.Value;
                }
            }

            foreach (var row in table.waves) Accumulate(row.entries);
            Accumulate(table.bossSupport);
            if (table.finalBoss) required[table.finalBoss] = Mathf.Max(required.TryGetValue(table.finalBoss, out var have) ? have : 0, 1);

            foreach (var kv in required) EnsurePoolCapacity(kv.Key, kv.Value + 2);
        }

        void SpawnEntries(IEnumerable<WaveEntry> entries) {
            if (entries == null) return;
            foreach (var e in entries) {
                if (!e.enemy || e.count <= 0) continue;
                for (int k = 0; k < e.count; k++) {
                    var go = SpawnEnemy(e.enemy, RandomPos());
                    if (go) alive.Add(go);
                }
            }
        }

        bool IsAlive(GameObject go) => go != null && go.activeInHierarchy;

        Vector3 RandomPos() {
            float x = Random.Range(arenaMin.x, arenaMax.x);
            float z = Random.Range(arenaMin.y, arenaMax.y); // правильный диапазон по Z
            return new Vector3(x, 0f, z);
        }

        GameObject SpawnEnemy(EnemyDefinition def, Vector3 pos) {
            // Пулл: ключ по id врага
            string key = "enemy_" + def.id;
            EnsurePoolCapacity(def, 1);

            var go = Pool.Spawn(key, pos, Quaternion.identity);
            if (!go) return null;

            // назначаем логику/цель
            var stats = go.GetComponent<EnemyStats>();
            if (stats) {
                stats.def = def;
                stats.ApplyDefinition(true);
            }

            if (go.TryGetComponent<EnemyBase>(out var ai)) ai.Init(player);

            return go;
        }

        void EnsurePoolCapacity(EnemyDefinition def, int desired) {
            if (!def) return;
            string key = "enemy_" + def.id;
            desired = Mathf.Max(desired, 1);
            int current = _poolCapacities.TryGetValue(key, out var have) ? have : 0;
            if (desired <= current) return;
            var prefab = BuildRuntimePrefab(def);
            Pool.Prewarm(key, prefab, desired - current);
            Object.Destroy(prefab);
            _poolCapacities[key] = current + (desired - current);
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
            var stats = root.AddComponent<EnemyStats>();
            stats.def = def;
            stats.ApplyDefinition(true);
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
