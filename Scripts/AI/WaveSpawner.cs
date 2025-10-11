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
        [Header("Stages")]
        public WaveStageSet stageSet;
        [Min(1)] public int stageIndex = 1;
        public Transform player;
        public Vector2 arenaMin = new Vector2(-10, -10);
        public Vector2 arenaMax = new Vector2( 10,  10);
        public float delayBetweenWaves = 2f;

        [Header("Events")]
        public System.Action onAllCleared;
        public System.Action<int,int> onWaveChanged; // (current, total)

        public int totalWaves => activePlan != null
            ? activePlan.TotalWaves
            : stageSet ? stageSet.GetTotalWaves(stageIndex, table) : table ? table.TotalWaves : 0;

        readonly List<GameObject> alive = new();
        static readonly Dictionary<string,int> _poolCapacities = new();
        WaveStageSet.WaveStagePlan activePlan;

        void Start() {
            if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
            StartCoroutine(Run());
        }

        IEnumerator Run() {
            // дождаться появления игрока (например, если он создаётся менеджером сцены)
            while (!player) {
                var found = GameObject.FindGameObjectWithTag("Player");
                if (found) player = found.transform;
                else yield return null;
            }

            activePlan = BuildPlan();
            if (activePlan == null || (activePlan.normalWaves.Count == 0 && !HasBossWave(activePlan))) {
                yield break;
            }

            PreparePools(activePlan);

            int total = activePlan.TotalWaves;
            for (int i = 0; i < total; i++) {
                // сообщаем HUD о смене волны
                onWaveChanged?.Invoke(i + 1, total);

                // начинаем волну с чистого списка живых
                alive.Clear();

                if (i < activePlan.normalWaves.Count) {
                    SpawnEntries(activePlan.normalWaves[i].entries);
                } else {
                    SpawnEntries(activePlan.bossSupport);
                    if (activePlan.includeBoss && activePlan.boss) {
                        var boss = SpawnEnemy(activePlan.boss, RandomPos());
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

        WaveStageSet.WaveStagePlan BuildPlan() {
            if (stageSet) {
                var plan = stageSet.BuildStagePlan(stageIndex, table);
                if (plan != null && plan.sourceTable == null && !table) {
                    // если план не смог подобрать таблицу, но у нас есть table, подставляем её
                    plan.sourceTable = table;
                }
                return plan;
            }

            if (!table) return null;

            var fallback = new WaveStageSet.WaveStagePlan {
                sourceTable = table,
                includeBoss = table.finalBoss != null,
                boss = table.finalBoss,
                bossSupport = table.bossSupport != null ? new List<WaveEntry>(table.bossSupport) : new List<WaveEntry>()
            };
            fallback.normalWaves.AddRange(table.waves);
            return fallback;
        }

        void PreparePools(WaveStageSet.WaveStagePlan plan) {
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

            foreach (var row in plan.normalWaves) Accumulate(row.entries);
            Accumulate(plan.bossSupport);
            if (plan.includeBoss && plan.boss) required[plan.boss] = Mathf.Max(required.TryGetValue(plan.boss, out var have) ? have : 0, 1);

            foreach (var kv in required) EnsurePoolCapacity(kv.Key, kv.Value + 2);
        }

        bool HasBossWave(WaveStageSet.WaveStagePlan plan) {
            if (plan == null || !plan.includeBoss) return false;
            if (plan.boss) return true;
            return plan.bossSupport != null && plan.bossSupport.Count > 0;
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

            if (!go) {
                // аварийный путь: если пул пуст, создаём экземпляр напрямую
                var prefab = BuildRuntimePrefab(def);
                go = Instantiate(prefab, pos, Quaternion.identity);
                Object.Destroy(prefab);
            }

            if (!go) return null;

            go.transform.position = pos;

            // назначаем логику/цель
            var stats = go.GetComponent<EnemyStats>();
            if (stats) {
                stats.def = def;
                stats.ApplyDefinition(true);
            }

            if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (go.TryGetComponent<EnemyBase>(out var ai)) ai.Init(player);

            var tracker = go.GetComponent<WaveSpawnedEnemy>();
            if (!tracker) tracker = go.AddComponent<WaveSpawnedEnemy>();
            tracker.Attach(this, def);

            return go;
        }

        void EnsurePoolCapacity(EnemyDefinition def, int desired) {
            if (!def) return;
            string key = "enemy_" + def.id;
            desired = Mathf.Max(desired, 1);
            int current = _poolCapacities.TryGetValue(key, out var have) ? have : 0;
            if (desired <= current) return;
            var prefab = BuildRuntimePrefab(def);
            prefab.SetActive(false);
            Pool.Prewarm(key, prefab, desired - current);
            Object.Destroy(prefab);
            _poolCapacities[key] = current + (desired - current);
        }

        // создаём Runtime-префаб на базе визуального, добавляя необходимые компоненты
        GameObject BuildRuntimePrefab(EnemyDefinition def) {
            var root = new GameObject(def.displayName);

            // визуал как child
            Transform visual = null;
            if (def.prefab) {
                var vis = Instantiate(def.prefab);
                vis.transform.SetParent(root.transform, false);
                visual = vis.transform;
            } else {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Visual";
                capsule.transform.SetParent(root.transform, false);
                if (capsule.TryGetComponent<Collider>(out var col)) col.enabled = false;
                if (capsule.TryGetComponent<Renderer>(out var renderer)) renderer.material.color = PickColor(def);
                visual = capsule.transform;
            }

            // контроллер для передвижения
            var cc = root.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.4f; cc.center = new Vector3(0, 1, 0);

            // Мотор (вращает только визуал-ребёнка)
            var motor = root.AddComponent<EnemyMotor>();
            motor.model = visual ? visual : root.transform;
            motor.stoppingDistance = 1.2f;
            motor.turnSpeed = 540f;

            // здоровье
            root.AddComponent<Health>();

            // статы и поведение
            var stats = root.AddComponent<EnemyStats>();
            stats.def = def;
            stats.ApplyDefinition(true);
            if (def.id.Contains("runner")) root.AddComponent<EnemyRunner>();
            else if (def.id.Contains("tank")) root.AddComponent<EnemyTank>();
            else root.AddComponent<EnemyShooter>(); // по умолчанию — шутер

            // лут/награда
            root.AddComponent<EnemyLoot>();

            return root;
        }

        static Color PickColor(EnemyDefinition def) {
            if (!def || string.IsNullOrEmpty(def.id)) return new Color(0.6f, 0.6f, 0.6f);
            var id = def.id.ToLowerInvariant();
            if (id.Contains("boss")) return new Color(0.8f, 0.2f, 0.2f);
            if (id.Contains("tank")) return new Color(0.85f, 0.3f, 0.3f);
            if (id.Contains("run")) return new Color(0.2f, 0.8f, 0.3f);
            if (id.Contains("shoot")) return new Color(0.2f, 0.4f, 0.85f);
            return new Color(0.6f, 0.6f, 0.6f);
        }

        internal void OnSpawnedEnemyDeath(GameObject go) {
            if (!go) return;
            StartCoroutine(DespawnNextFrame(go));
        }

        IEnumerator DespawnNextFrame(GameObject go) {
            yield return null;
            alive.Remove(go);
            Pool.Despawn(go);
        }
    }
}
