using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketCardUI : MonoBehaviour
{
    [Header("UI Тексты")]
    public TextMeshProUGUI textCarName;
    public TextMeshProUGUI textCondition;
    public TextMeshProUGUI textDefects;
    public TextMeshProUGUI textPrice;

    [Header("Кнопка")]
    public Button buyButton;

    private CarInstance _car;
    private MarketManager _marketManager;

    public void Setup(CarInstance car, MarketManager manager)
    {
        _car = car;
        _marketManager = manager;

        textCarName.text = car.data.carName;
        textCondition.text = $"Состояние: {car.conditionName}";
        
        // Получаем чистую строку "Дефекты: Кузов, Салон"
        textDefects.text = car.GetFormattedDefectsString(PlayerData.Instance.diagnostLevel);
        
        textPrice.text = $"{car.buyPrice:N0} ₽";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);

        // Проверяем доступность кнопки
        bool canAfford = PlayerData.Instance.money >= car.buyPrice;
        bool hasSpace = PlayerData.Instance.garage.Count < PlayerData.Instance.MaxGarageSlots;
        buyButton.interactable = canAfford && hasSpace;
    }

    private void OnBuyClicked()
    {
        _marketManager.BuyCarFromMarket(_car, gameObject);
    }
}