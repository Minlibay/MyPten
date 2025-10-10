using UnityEngine;
using UnityEngine.UI;
using Begin.Talents;
using System.Linq;

namespace Begin.UI {
    public class TalentsUI : MonoBehaviour {
        public TalentTree tree;                   // закинем SampleTree
        public RectTransform listContainer;       // куда класть строки
        public Button rowPrefab;                  // шаблон строки (выключен)
        public Text pointsText;
        public Button respecButton;

        void OnEnable() {
            TalentService.BindTree(tree);
            TalentService.OnChanged += Rebuild;
            Rebuild();
        }
        void OnDisable() { TalentService.OnChanged -= Rebuild; }

        public void Rebuild() {
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
