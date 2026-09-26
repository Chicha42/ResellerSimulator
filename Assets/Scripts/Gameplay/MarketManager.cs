using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketManager : MonoBehaviour
{
    [Header("Каталог доступных моделей")]
    public List<CarData> catalog = new List<CarData>();

    [Header("UI Ссылки")]
    public Transform cardsContainer;      // Сюда перетащи объект Content из ScrollView
    public GameObject marketCardPrefab;   // Сюда перетащи префаб карточки из папки Prefabs

    [Header("Обновление объявлений (BALANCE.md, раздел 12.5)")]
    public Button refreshButton;
    public TextMeshProUGUI refreshButtonText;
    public float cooldownMin = 5f;
    public float cooldownMax = 15f;

    private List<CarInstance> currentOffers = new List<CarInstance>();
    private bool _onCooldown;
    private string _refreshLabel = "Обновить объявления";

    // Кулдаун считается по абсолютному времени (Time.time), а не накоплением deltaTime.
    // Так он идёт и когда вкладка рынка выключена (корутина останавливается вместе с объектом).
    private float _cooldownEndTime = -1f;
    private bool _cooldownRoutineRunning;

    private void OnEnable()
    {
        // Вернулись на вкладку рынка: догоняем кулдаун по реальному времени
        if (_cooldownEndTime < 0f) return;

        if (Time.time >= _cooldownEndTime) EndCooldown();
        else if (!_cooldownRoutineRunning) StartCoroutine(RefreshCooldownRoutine());
    }

    private void OnDisable()
    {
        // Объект выключен — корутина остановилась, снимаем флаг, чтобы возобновить её в OnEnable
        _cooldownRoutineRunning = false;
    }

    private void Start()
    {
        if (refreshButtonText != null && !string.IsNullOrEmpty(refreshButtonText.text))
            _refreshLabel = refreshButtonText.text;

        // Первичная генерация рынка — это не действие игрока, такт не тратится
        GenerateMarketOffers();
        SetRefreshInteractable(true);
    }

    // Кнопка обновления объявлений: тратит такт и уходит на кулдаун 5-15 сек
    public void TryRefreshMarket()
    {
        if (_onCooldown) return;

        GenerateMarketOffers();

        // Обновление объявлений — действие игрока (двигает тренд и стоянку, разделы 12.2/12.4)
        PlayerData.Instance.TickWorld();
        UIManager.Instance?.UpdateTopBar();

        StartCooldown();
    }

    // Генерация 3-5 объявлений
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
        if (available.Count == 0)
        {
            // Не показываем закрытые машины: берём только те, что ближе всего к открытию
            int minUnlock = int.MaxValue;
            foreach (var c in catalog)
                if (c != null && c.requiredSalesToUnlock < minUnlock) minUnlock = c.requiredSalesToUnlock;
            available = catalog.FindAll(c => c != null && c.requiredSalesToUnlock == minUnlock);
            Debug.LogWarning("Нет доступных машин по продажам — показаны ближайшие к открытию.");
        }

        int offersCount = Random.Range(3, 6); // от 3 до 5 машин

        for (int i = 0; i < offersCount; i++)
        {
            CarData randomData = available[Random.Range(0, available.Count)];
            CarInstance newCar = new CarInstance(randomData, randomData.segment);
            currentOffers.Add(newCar);

            // Создаем визуальную карточку на сцене
            GameObject cardObj = Instantiate(marketCardPrefab, cardsContainer);
            MarketCardUI cardUI = cardObj.GetComponent<MarketCardUI>();
            cardUI.Setup(newCar, this);
        }
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

    // Запуск кулдауна: запоминаем абсолютный момент окончания (Time.time).
    private void StartCooldown()
    {
        _cooldownEndTime = Time.time + Random.Range(cooldownMin, cooldownMax);

        if (isActiveAndEnabled && !_cooldownRoutineRunning)
            StartCoroutine(RefreshCooldownRoutine());
    }

    private IEnumerator RefreshCooldownRoutine()
    {
        _cooldownRoutineRunning = true;
        _onCooldown = true;
        SetRefreshInteractable(false);

        // Считаем остаток от абсолютного времени — отсчёт корректно идёт и после возврата на вкладку
        while (Time.time < _cooldownEndTime)
        {
            if (refreshButtonText != null)
                refreshButtonText.text = $"{_refreshLabel} ({Mathf.CeilToInt(_cooldownEndTime - Time.time)}с)";
            yield return null;
        }

        _cooldownRoutineRunning = false;
        EndCooldown();
    }

    private void EndCooldown()
    {
        _cooldownEndTime = -1f;
        _cooldownRoutineRunning = false;
        _onCooldown = false;

        if (refreshButtonText != null) refreshButtonText.text = _refreshLabel;
        SetRefreshInteractable(true);
    }

    private void SetRefreshInteractable(bool value)
    {
        if (refreshButton != null) refreshButton.interactable = value;
    }
}
