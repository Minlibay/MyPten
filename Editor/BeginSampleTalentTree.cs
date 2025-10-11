#if UNITY_EDITOR
using UnityEditor;
using Begin.EditorTools;

public static class BeginSampleTalentTree {
    [MenuItem("Tools/Begin/Create ▸ Sample Talent Tree")]
    public static void Create() {
        TalentBatchGeneratorWindow.OpenWindow();
        EditorUtility.DisplayDialog(
            "Begin",
            "Открылось окно генерации талантов. Выберите CSV и нажмите Generate/Update Assets, чтобы создать CompleteTree.",
            "OK");
    }
}
#endif
