using UnityEngine;

namespace Begin.World {
    public class MapGeneratorSimple : MonoBehaviour {
        public Vector2Int size = new(40, 40);
        public GameObject[] obstacles; // можно оставить пустым
        [Range(0f,1f)] public float fill = 0.06f;
        public int seed = 0;

        public void Generate() {
            var rnd = new System.Random(seed == 0 ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : seed);
            int toPlace = Mathf.RoundToInt(size.x * size.y * fill);
            for (int i = 0; i < toPlace; i++) {
                if (obstacles == null || obstacles.Length == 0) break;
                var pref = obstacles[rnd.Next(obstacles.Length)];
                var pos = new Vector3(rnd.Next(size.x) - size.x/2, 0, rnd.Next(size.y) - size.y/2);
                Object.Instantiate(pref, pos, Quaternion.identity, transform);
            }
        }
    }
}
