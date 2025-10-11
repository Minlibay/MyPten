using UnityEngine;

namespace Begin.Talents {
    /// <summary>
    /// Legacy wrapper kept so existing scenes/assets referencing the old TalentPrereq ScriptableObject
    /// continue to deserialize without missing-script warnings. New logic uses TalentRequirement records
    /// embedded on the TalentNode assets instead.
    /// </summary>
    [CreateAssetMenu(menuName = "Begin/Talents/Legacy Prerequisite"), System.Obsolete]
    public class TalentPrereq : ScriptableObject {
        public string nodeId;
        public int requiredRank = 1;
    }

    [CreateAssetMenu(menuName = "Begin/Talents/Legacy Requirement"), System.Obsolete("Use inline requirements on TalentNode instead.")]
    public class TalentRequirement : TalentPrereq { }
}
