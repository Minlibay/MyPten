using UnityEngine;
using UnityEngine.UI;
using Begin.Talents;
using System.Linq;

namespace Begin.UI {
    public class TalentsUI : MonoBehaviour {
        [Tooltip("Дерево талантов, которое будет отображаться. Если не назначено, загрузим из Resources по пути ниже.")]
        public TalentTree tree;                   // закинем SampleTree
        [SerializeField]
        [Tooltip("Resources-путь для авто-поиска дерева, если поле tree пустое.")]
        string treeResourcePath = "Talents/CompleteTree";
        public RectTransform listContainer;       // куда класть строки
        public Button rowPrefab;                  // шаблон строки (выключен)
        public Text pointsText;
        public Button respecButton;

        void OnEnable() {
            if (tree == null && !string.IsNullOrEmpty(treeResourcePath)) {
                tree = Resources.Load<TalentTree>(treeResourcePath);
            }

            if (tree == null) {
                tree = Resources.LoadAll<TalentTree>("Talents").FirstOrDefault();
            }

            if (tree == null) {
                Debug.LogWarning("TalentsUI: no talent tree assigned, skipping rebuild.", this);
                return;
            }

            TalentService.BindTree(tree);
            TalentService.OnChanged += Rebuild;
            Rebuild();
        }

        void OnDisable() {
            TalentService.OnChanged -= Rebuild;
        }

        public void Rebuild() {
            if (tree == null || listContainer == null || rowPrefab == null) return;

            foreach (Transform t in listContainer) Destroy(t.gameObject);

            foreach (var node in tree.nodes) {
                var b = Instantiate(rowPrefab, listContainer);
                b.gameObject.SetActive(true);

                // заполняем под-элементы
                var nameT = b.transform.Find("Name")?.GetComponent<Text>();
                var descT = b.transform.Find("Desc")?.GetComponent<Text>();
                var actT  = b.transform.Find("Action")?.GetComponent<Text>();

                int rank = TalentService.GetRank(node.id);
                if (nameT) nameT.text = $"{node.title}  [{rank}/{node.maxRank}]";
                if (descT) descT.text = string.IsNullOrEmpty(node.description)
                    ? $"{node.type}: +{node.GetValueAt(rank)}"
                    : node.description + $"  (текущ.: {node.GetValueAt(rank)})";

                bool can = TalentService.CanUpgrade(node);
                if (actT) actT.text = can ? "+" : "—";
                b.interactable = can;

                b.onClick.AddListener(() => { if (TalentService.Upgrade(node)) Rebuild(); });
            }

            if (pointsText) pointsText.text = $"Очки талантов: {TalentService.Points}";
            if (respecButton) {
                respecButton.onClick.RemoveAllListeners();
                respecButton.onClick.AddListener(()=> { TalentService.RespecAll(); Rebuild(); });
            }
        }
    }
}
