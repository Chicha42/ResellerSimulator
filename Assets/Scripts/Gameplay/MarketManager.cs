using System.Collections.Generic;
using UnityEngine;

public class MarketManager : MonoBehaviour
{
    [Header("Каталог доступных моделей")]
    public List<CarData> catalog = new List<CarData>();

    [Header("UI Ссылки")]
    public Transform cardsContainer;      // Сюда перетащи объект Content из ScrollView
    public GameObject marketCardPrefab;   // Сюда перетащи префаб карточки из папки Prefabs

    private List<CarInstance> currentOffers = new List<CarInstance>();

    private void Start()
    {
        GenerateMarketOffers();
    }

    // Генерация 3-5 объявлений (как в прототипе)
    public void GenerateMarketOffers()
    {
        if (catalog.Count == 0)
        {
            Debug.LogWarning("Каталог машин пуст! Добавь CarData в инспекторе.");
            return;
        }

        // Очищаем старые карточки из ScrollView
        foreach (Transform child in cardsContainer)
        {
            Destroy(child.gameObject);
        }
        currentOffers.Clear();

        // Доступные машины по продажам игрока
        List<CarData> available = catalog.FindAll(c => c != null && c.requiredSalesToUnlock <= PlayerData.Instance.totalSalesCount);
        if (available.Count == 0) available = catalog.FindAll(c => c != null);

        int offersCount = Random.Range(3, 6); // от 3 до 5 машин

        for (int i = 0; i < offersCount; i++)
        {
            CarData randomData = available[Random.Range(0, available.Count)];
            CarInstance newCar = new CarInstance(randomData, carSegment: 1);
            currentOffers.Add(newCar);

            // Создаем визуальную карточку на сцене
            GameObject cardObj = Instantiate(marketCardPrefab, cardsContainer);
            MarketCardUI cardUI = cardObj.GetComponent<MarketCardUI>();
            cardUI.Setup(newCar, this);
        }

        // Тик мира при обновлении рынка (проверка стоянки, сдвиг тренда)
        PlayerData.Instance.TickWorld();
    }

    // Покупка машины
    public void BuyCarFromMarket(CarInstance car, GameObject cardObject)
    {
        if (PlayerData.Instance.garage.Count >= PlayerData.Instance.MaxGarageSlots)
        {
            Debug.Log("В гараже нет мест!");
            return;
        }

        if (PlayerData.Instance.money >= car.buyPrice)
        {
            PlayerData.Instance.money -= car.buyPrice;
            PlayerData.Instance.garage.Add(car);

            currentOffers.Remove(car);
            Destroy(cardObject); // Удаляем купленную карточку с рынка

            PlayerData.Instance.TickWorld();
            UIManager.Instance.UpdateTopBar();
            Debug.Log($"Успешно куплен {car.data.carName} за {car.buyPrice:N0} ₽");
        }
        else
        {
            Debug.Log("Не хватает денег!");
        }
    }
}