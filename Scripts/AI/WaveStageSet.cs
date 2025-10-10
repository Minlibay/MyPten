using System;
using System.Collections.Generic;
using UnityEngine;
using Begin.Enemies;

namespace Begin.AI {
    public enum WaveSequenceOverflowMode {
        ClampToLast,
        Loop,
        Stop
    }

    [CreateAssetMenu(menuName = "Begin/Waves/Stage Set")]
    public class WaveStageSet : ScriptableObject {
        [Min(1)] public int firstStageWaveCount = 6;
        public int waveIncrementPerStage = 1;
        public bool includeBossByDefault = true;
        public WaveSequenceOverflowMode defaultOverflowMode = WaveSequenceOverflowMode.Loop;
        [Tooltip("Если не задано явно, используется значение из WaveSpawner.table")] public WaveTable defaultWaveTable;
        public List<StageOverride> stageOverrides = new();

        [Serializable]
        public class StageOverride {
            [Min(1)] public int stageIndex = 1;
            public WaveTable waveTableOverride;

            [Header("Основные настройки")]
            public bool overrideNormalWaveCount;
            [Min(0)] public int normalWaveCount = 6;
            public bool overrideIncludeBoss;
            public bool includeBoss = true;

            [Header("Босс")]
            public bool overrideBoss;
            public EnemyDefinition bossOverride;
            public bool overrideBossSupport;
            public List<WaveEntry> bossSupportOverride = new();

            [Header("Порядок волн")]
            public bool overrideOverflowMode;
            public WaveSequenceOverflowMode overflowMode = WaveSequenceOverflowMode.Loop;
            [Tooltip("0-based индексы волн из таблицы. Если пусто — используется последовательность по умолчанию.")]
            public List<int> waveSequence = new();
        }

        public class WaveStagePlan {
            public WaveTable sourceTable;
            public List<WaveRow> normalWaves = new();
            public List<WaveEntry> bossSupport = new();
            public EnemyDefinition boss;
            public bool includeBoss;

            public int TotalWaves {
                get {
                    bool hasBossWave = includeBoss && (boss || (bossSupport != null && bossSupport.Count > 0));
                    return normalWaves.Count + (hasBossWave ? 1 : 0);
                }
            }
        }

        public WaveStagePlan BuildStagePlan(int stageIndex, WaveTable fallbackTable) {
            var plan = new WaveStagePlan();
            stageIndex = Mathf.Max(stageIndex, 1);

            var stage = stageOverrides.Find(s => s.stageIndex == stageIndex);

            var table = stage != null && stage.waveTableOverride ? stage.waveTableOverride : (defaultWaveTable ? defaultWaveTable : fallbackTable);
            if (!table) {
                // если таблицы нет, вернуть пустой план
                plan.includeBoss = false;
                return plan;
            }

            int normalCount = Mathf.Max(0, firstStageWaveCount + waveIncrementPerStage * (stageIndex - 1));
            if (stage != null && stage.overrideNormalWaveCount) normalCount = Mathf.Max(0, stage.normalWaveCount);

            bool includeBoss = includeBossByDefault;
            if (stage != null && stage.overrideIncludeBoss) includeBoss = stage.includeBoss;

            var overflowMode = defaultOverflowMode;
            if (stage != null && stage.overrideOverflowMode) overflowMode = stage.overflowMode;

            plan.normalWaves = BuildWaveOrder(table, normalCount, stage != null ? stage.waveSequence : null, overflowMode);
            plan.includeBoss = includeBoss;
            plan.sourceTable = table;

            if (includeBoss) {
                if (stage != null && stage.overrideBoss) plan.boss = stage.bossOverride ? stage.bossOverride : null;
                else plan.boss = table.finalBoss;

                List<WaveEntry> supportSource = null;
                if (stage != null && stage.overrideBossSupport) supportSource = stage.bossSupportOverride;
                else supportSource = table.bossSupport;

                if (supportSource != null) plan.bossSupport = new List<WaveEntry>(supportSource);
                else plan.bossSupport = new List<WaveEntry>();
            } else {
                plan.boss = null;
                plan.bossSupport = new List<WaveEntry>();
            }

            return plan;
        }

        public int GetTotalWaves(int stageIndex, WaveTable fallbackTable) => BuildStagePlan(stageIndex, fallbackTable).TotalWaves;

        List<WaveRow> BuildWaveOrder(WaveTable table, int count, List<int> sequence, WaveSequenceOverflowMode overflowMode) {
            var result = new List<WaveRow>();
            if (!table || table.waves == null || table.waves.Count == 0 || count <= 0) return result;

            bool hasCustomSequence = sequence != null && sequence.Count > 0;
            int available = table.waves.Count;

            for (int i = 0; i < count; i++) {
                int desiredIndex;
                if (hasCustomSequence) {
                    if (i < sequence.Count) desiredIndex = sequence[i];
                    else {
                        switch (overflowMode) {
                            case WaveSequenceOverflowMode.ClampToLast:
                                desiredIndex = sequence[sequence.Count - 1];
                                break;
                            case WaveSequenceOverflowMode.Loop:
                                desiredIndex = sequence[i % sequence.Count];
                                break;
                            case WaveSequenceOverflowMode.Stop:
                                return result;
                            default:
                                desiredIndex = sequence[sequence.Count - 1];
                                break;
                        }
                    }
                } else {
                    desiredIndex = i;
                }

                if (available <= 0) break;

                if (desiredIndex < 0 || desiredIndex >= available) {
                    switch (overflowMode) {
                        case WaveSequenceOverflowMode.ClampToLast:
                            desiredIndex = Mathf.Clamp(desiredIndex, 0, available - 1);
                            break;
                        case WaveSequenceOverflowMode.Loop:
                            desiredIndex = (available + (desiredIndex % available)) % available;
                            break;
                        case WaveSequenceOverflowMode.Stop:
                            return result;
                    }
                }

                if (desiredIndex < 0 || desiredIndex >= available) continue;

                result.Add(table.waves[desiredIndex]);
            }

            return result;
        }
    }
}
