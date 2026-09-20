using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Тексты TopBar")]
    public TextMeshProUGUI textMoney;
    public TextMeshProUGUI textGarageSlots;
    public TextMeshProUGUI textReputation;

    [Header("Панели экранов")]
    public GameObject panelMarket;
    public GameObject panelGarage;
    public GameObject panelUpgrades;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateTopBar();
        ShowMarket(); // На старте открываем рынок
    }

    // Обновление верхнего бара
    public void UpdateTopBar()
    {
        if (PlayerData.Instance == null) return;

        // Деньги с форматированием пробелов (25 000 Р)
        textMoney.text = $"{PlayerData.Instance.money:N0} Р";

        // Гараж: занято / всего мест
        int currentCars = PlayerData.Instance.garage.Count;
        int maxSlots = PlayerData.Instance.MaxGarageSlots;
        textGarageSlots.text = $"Гараж: {currentCars}/{maxSlots}";

        // Название репутации по документу
        string repTitle = GetReputationTitle(PlayerData.Instance.totalSalesCount);
        textReputation.text = $"Ур: {repTitle}";
    }

    private string GetReputationTitle(int sales)
    {
        if (sales >= 40) return "Легенда";
        if (sales >= 25) return "Автобарон";
        if (sales >= 15) return "Мастер";
        if (sales >= 8)  return "Бывалый";
        if (sales >= 3)  return "Любитель";
        return "Новичок";
    }

    // Методы переключения экранов для кнопок
    public void ShowMarket()
    {
        panelMarket.SetActive(true);
        panelGarage.SetActive(false);
        panelUpgrades.SetActive(false);
    }

    public void ShowGarage()
    {
        panelMarket.SetActive(false);
        panelGarage.SetActive(true);
        panelUpgrades.SetActive(false);
    }

    public void ShowUpgrades()
    {
        panelMarket.SetActive(false);
        panelGarage.SetActive(false);
        panelUpgrades.SetActive(true);
    }
}