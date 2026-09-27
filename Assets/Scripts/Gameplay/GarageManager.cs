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
            textTrendTitle.text = "Рынок стабилен";
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

        // Цены мест — в PlayerData (BALANCE.md, раздел 5.3). Максимум 6 мест (уровень 4).
        int nextCost = PlayerData.Instance.GarageUpgradeCost;
        if (nextCost >= 0)
        {
            expansionPanel.SetActive(true);
            textExpansionCost.text = $"+1 место · {nextCost:N0} ₽";

            btnBuyExpansion.interactable = PlayerData.Instance.money >= nextCost;
            btnBuyExpansion.onClick.RemoveAllListeners();
            btnBuyExpansion.onClick.AddListener(() =>
            {
                // Покупка прокачки не является игровым действием, такт не тратится
                if (PlayerData.Instance.TryUpgradeGarage())
                {
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