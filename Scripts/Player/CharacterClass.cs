using UnityEngine;

namespace Begin.PlayerData {
    [CreateAssetMenu(menuName="RPG/Character Class")]
    public class CharacterClass : ScriptableObject {
        public string id;
        public string displayName;
        public Sprite icon;
        public int baseHP = 100;
        public int baseSTR = 5;
        public int baseDEX = 5;
        public int baseINT = 5;
        [TextArea] public string description;
    }
}
