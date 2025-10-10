using System;
using System.Collections.Generic;
using UnityEngine;
using Begin.Enemies;

namespace Begin.AI {
    [Serializable]
    public class WaveEntry {
        public EnemyDefinition enemy;
        public int count = 5;
    }

    [Serializable]
    public class WaveRow {
        public List<WaveEntry> entries = new();
    }

    [CreateAssetMenu(menuName = "Begin/Waves/Table")]
    public class WaveTable : ScriptableObject {
        public List<WaveRow> waves = new();
        [Header("Final Wave")]
        public EnemyDefinition finalBoss;
        public List<WaveEntry> bossSupport = new();

        public int TotalWaves => waves.Count + (finalBoss != null ? 1 : 0);
    }
}
