using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Логика экрана прокачки (BALANCE.md, раздел 5) для готового UI.
// UI лежит в сцене (UpgradesPanel), менеджер только наполняет тексты и вешает обработчики кнопок.
public class UpgradesManager : MonoBehaviour
{
    public static UpgradesManager Instance { get; private set; }

    [Header("Карточка Диагноста")]
    public Button DiagnostBuyButton;
    public TextMeshProUGUI DiagnostBuyBtnText;
    public TextMeshProUGUI DiagnostDescription;

    [Header("Карточка Склада")]
    public Button StorageBuyButton;
    public TextMeshProUGUI StorageBuyBtnText;
    public TextMeshProUGUI StorageDescription;

    [Header("Карточка Гаража")]
    public Button GarageBuyButton;
    public TextMeshProUGUI GarageBuyBtnText;
    public TextMeshProUGUI GarageDescription;

    private void Awake()
    {
        Instance = this;

        if (DiagnostBuyButton != null) DiagnostBuyButton.onClick.AddListener(OnBuyDiagnost);
        if (StorageBuyButton != null) StorageBuyButton.onClick.AddListener(OnBuyStorage);
        if (GarageBuyButton != null) GarageBuyButton.onClick.AddListener(OnBuyGarage);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (PlayerData.Instance == null) return;
        var p = PlayerData.Instance;

        // Описания уровней. Числа — из BALANCE.md, раздел 5.1/5.2/5.3.
        if (DiagnostDescription != null)
            DiagnostDescription.text = $"Уровень {p.diagnostLevel}/4 · {DiagnostEffect(p.diagnostLevel)}";
        if (StorageDescription != null)
            StorageDescription.text = $"Уровень {p.warehouseLevel}/4 · скидка на ремонт {p.warehouseLevel * 10}%";
        if (GarageDescription != null)
            GarageDescription.text = $"Уровень {p.garageLevel}/4 · мест в гараже {p.MaxGarageSlots}";

        SetupBuyButton(DiagnostBuyButton, DiagnostBuyBtnText, p.DiagnostUpgradeCost);
        SetupBuyButton(StorageBuyButton, StorageBuyBtnText, p.WarehouseUpgradeCost);
        SetupBuyButton(GarageBuyButton, GarageBuyBtnText, p.GarageUpgradeCost);
    }

    private static void SetupBuyButton(Button button, TextMeshProUGUI label, int cost)
    {
        if (button == null) return;

        if (cost < 0)
        {
            if (label != null) label.text = "МАКС";
            button.interactable = false;
            return;
        }

        if (label != null) label.text = $"Улучшить · {cost:N0} ₽";
        button.interactable = PlayerData.Instance.money >= cost;
    }

    private void OnBuyDiagnost()
    {
        if (PlayerData.Instance.TryUpgradeDiagnost()) Refresh();
    }

    private void OnBuyStorage()
    {
        if (PlayerData.Instance.TryUpgradeWarehouse()) Refresh();
    }

    private void OnBuyGarage()
    {
        if (PlayerData.Instance.TryUpgradeGarage())
        {
            Refresh();
            GarageManager.Instance?.RefreshGarage();
        }
    }

    // Что даёт Диагност по уровням (BALANCE.md, раздел 5.1; видимость без прокачки — раздел 4).
    private static string DiagnostEffect(int level) => level switch
    {
        0 => "видно 0–3 дефекта по состоянию",
        1 => "видно 2 дефекта",
        2 => "видно 4 дефекта",
        3 => "видно все дефекты",
        _ => "видно всё, сюрприз 0%"
    };
}
