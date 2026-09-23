using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GarageManager : MonoBehaviour
{
    public static GarageManager Instance { get; private set; }

    [Header("Сетка слотов")]
    public Transform slotsContainer;        // Content внутри ScrollView
    public GameObject garageSlotPrefab;     // Твой ПОЛНЫЙ префаб

    [Header("Рыночный тренд")]
    public TextMeshProUGUI textTrendTitle;
    public TextMeshProUGUI textTrendDesc;

    [Header("Расширение гаража")]
    public GameObject expansionPanel;
    public TextMeshProUGUI textExpansionCost;
    public Button btnBuyExpansion;

    [Header("Окно ремонта")]
    public GarageCarDetailsUI carDetailsModal;

    // Цены расширения гаража по документу (1->15к, 2->50к, 3->150к, 4->400к, 5->1м)
    private readonly int[] _slotCosts = { 0, 15000, 50000, 150000, 400000, 1000000 };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        RefreshGarage();
    }

    public void RefreshGarage()
    {
        UpdateTrendUI();
        UpdateSlotsGrid();
        UpdateExpansionUI();
    }

    private void UpdateTrendUI()
    {
        if (textTrendTitle == null) return;

        float trend = PlayerData.Instance.marketTrend;
        int pct = Mathf.RoundToInt((trend - 1f) * 100f);

        if (trend >= 1.07f)
        {
            textTrendTitle.text = "Рынок на подъёме";
            textTrendTitle.color = new Color32(0, 185, 84, 255);
        }
        else if (trend <= 0.93f)
        {
            textTrendTitle.text = "Рынок просел";
            textTrendTitle.color = new Color32(255, 92, 92, 255);
        }
        else
        {
            textTrendTitle.text = "⚖Рынок стабилен";
            textTrendTitle.color = new Color32(138, 145, 163, 255);
        }

        string sign = pct >= 0 ? "+" : "";
        textTrendDesc.text = $"Цены продажи сейчас {sign}{pct}% к справедливой стоимости";
    }

    private void UpdateSlotsGrid()
    {
        foreach (Transform child in slotsContainer) Destroy(child.gameObject);

        int maxSlots = PlayerData.Instance.MaxGarageSlots;
        var cars = PlayerData.Instance.garage;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(garageSlotPrefab, slotsContainer);
            GarageSlotUI slotUI = slotObj.GetComponent<GarageSlotUI>();

            if (i < cars.Count)
            {
                slotUI.SetupFilled(cars[i], (car) => carDetailsModal.Open(car));
            }
            else
            {
                slotUI.SetupEmpty();
            }
        }
    }

    private void UpdateExpansionUI()
    {
        if (expansionPanel == null) return;

        int currentLvl = PlayerData.Instance.garageLevel;
        // Максимум 6 мест (уровень 5)
        if (currentLvl < 5)
        {
            expansionPanel.SetActive(true);
            int nextCost = _slotCosts[currentLvl + 1];
            textExpansionCost.text = $"+1 место · {nextCost:N0} ₽";

            btnBuyExpansion.interactable = PlayerData.Instance.money >= nextCost;
            btnBuyExpansion.onClick.RemoveAllListeners();
            btnBuyExpansion.onClick.AddListener(() =>
            {
                if (PlayerData.Instance.money >= nextCost)
                {
                    PlayerData.Instance.money -= nextCost;
                    PlayerData.Instance.garageLevel++;
                    PlayerData.Instance.TickWorld();
                    UIManager.Instance.UpdateTopBar();
                    RefreshGarage();
                }
            });
        }
        else
        {
            expansionPanel.SetActive(false);
        }
    }
}