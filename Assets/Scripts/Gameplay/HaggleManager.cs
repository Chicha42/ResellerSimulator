using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ───────────────────────── Модель торга (BALANCE.md, раздел 12.1) ─────────────────────────

// Личность покупателя. Имена без транслита.
public enum BuyerPersonality
{
    Stingy,     // Прижимистый
    Regular,    // Обычный
    Generous,   // Щедрый
    Impatient,  // Нетерпеливый
    Haggler     // Торгаш
}

// Результат одной реплики торга.
public enum HaggleMoveResult
{
    Success,     // Покупатель поднял цену
    Fail,        // Отказ, но покупатель остался
    BuyerLeft,   // Покупатель обиделся и ушёл
    NoPatience   // Не хватает терпения на реплику
}

// Состояние одной сессии торга с покупателем.
public class HaggleSession
{
    public BuyerPersonality personality;
    public string buyerName;
    public float startMultMin;   // Нижняя граница стартового предложения, × от справедливой цены
    public float startMultMax;   // Верхняя граница стартового предложения, × от справедливой цены
    public float ceilingMult;    // Потолок цены, × от справедливой цены
    public float successChance;  // Базовый шанс успеха торга
    public int patienceMax;      // Максимум терпения
    public int patienceLeft;     // Остаток терпения
    public int fairValue;        // Справедливая цена машины (PlayerData.CalculateSellPrice)
    public int currentOffer;     // Текущее предложение покупателя
    public bool finished;        // Покупатель ушёл / сделка закрыта
}

// ───────────────────────── Модалка чата с покупателем (BALANCE.md, раздел 12.1) ─────────────────────────

// UI собран в сцене (Modal_Haggle), скрипт только наполняет тексты и обрабатывает кнопки.
// Модалка выключена в сцене (как Modal_CarDetails), открывается через Open(car).
public class HaggleManager : MonoBehaviour
{
    public static HaggleManager Instance { get; private set; }

    // BALANCE.md, 12.1: если покупатель ушёл, новый появится не раньше чем через 5 игровых действий.
    private const int ReopenCooldownTicks = 5;

    [Header("Тексты")]
    public TextMeshProUGUI textTitle;     // "Покупатель: <личность>"
    public TextMeshProUGUI textOffer;     // Текущее предложение
    public TextMeshProUGUI textPatience;  // Терпение
    public TextMeshProUGUI textChat;      // Лента сообщений
    public TextMeshProUGUI textInfo;      // Инфо / ожидание нового покупателя

    [Header("Кнопки")]
    public Button btnSoft;    // Мягкий торг — 1 терпение
    public Button btnHard;    // Агрессивный торг — 2 терпения, шанс ниже на 20 п.п.
    public Button btnAccept;  // Продать по текущему предложению
    public Button btnLeave;   // Уйти
    public Button btnClose;   // Закрыть модалку

    private readonly List<string> _messages = new List<string>();
    private HaggleSession _session;
    private CarInstance _currentCar;

    private void Awake()
    {
        Instance = this;

        // Подписываем кнопки один раз. Awake сработает при первом включении модалки.
        if (btnSoft != null) btnSoft.onClick.AddListener(() => OnHaggle(false));
        if (btnHard != null) btnHard.onClick.AddListener(() => OnHaggle(true));
        if (btnAccept != null) btnAccept.onClick.AddListener(OnAccept);
        if (btnLeave != null) btnLeave.onClick.AddListener(OnLeave);
        if (btnClose != null) btnClose.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Открыть окно торга для конкретной машины.
    public void Open(CarInstance car)
    {
        _currentCar = car;
        _messages.Clear();
        _session = null;

        // BALANCE.md, 12.1: переоткрытие не раньше, чем через 5 тактов после создания сессии.
        int readyAt = PlayerData.Instance.lastHaggleSessionTick + ReopenCooldownTicks;
        if (PlayerData.Instance.actionTick < readyAt)
        {
            int wait = readyAt - PlayerData.Instance.actionTick;
            textTitle.text = "Покупателей пока нет";
            textOffer.text = "";
            textPatience.text = "";
            textChat.text = "";
            textInfo.text = $"Новый покупатель появится через {wait} действ.";
            SetActionButtons(false, false, false);
            gameObject.SetActive(true);
            return;
        }

        PlayerData.Instance.lastHaggleSessionTick = PlayerData.Instance.actionTick;
        _session = CreateSession(car);

        AddMessage($"{_session.buyerName} покупатель: Здравствуйте! Заберу за {_session.currentOffer:N0} ₽.");
        textInfo.text = "";
        Refresh();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void AddMessage(string msg)
    {
        _messages.Add(msg);
        // Держим последние 6 сообщений, чтобы влезало в окошко.
        while (_messages.Count > 6) _messages.RemoveAt(0);
    }

    private void Refresh()
    {
        if (_session == null) return;

        textTitle.text = $"Покупатель: {_session.buyerName}";
        textOffer.text = $"Текущее предложение: {_session.currentOffer:N0} ₽";
        textPatience.text = $"Терпение: {_session.patienceLeft}/{_session.patienceMax}";
        textChat.text = string.Join("\n", _messages);

        bool alive = !_session.finished;
        bool canSoft = alive && _session.patienceLeft >= 1; // мягкой реплике нужно 1 терпение
        bool canHard = alive && _session.patienceLeft >= 2; // агрессивной — 2 терпения
        SetActionButtons(canSoft, canHard, alive);
    }

    private void SetActionButtons(bool soft, bool hard, bool accept)
    {
        if (btnSoft != null) btnSoft.interactable = soft;
        if (btnHard != null) btnHard.interactable = hard;
        if (btnAccept != null)
        {
            btnAccept.interactable = accept;
            var label = btnAccept.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = _session != null ? $"Продать за {_session.currentOffer:N0} ₽" : "Продать";
        }
    }

    private void OnHaggle(bool aggressive)
    {
        if (_session == null || _session.finished) return;

        AddMessage($"Вы: {(aggressive ? "Маловато, подумай" : "Может, дороже?")}");

        var result = Haggle(_session, aggressive);
        switch (result)
        {
            case HaggleMoveResult.Success:
                AddMessage($"{_session.buyerName}: Ладно, {_session.currentOffer:N0} ₽.");
                break;
            case HaggleMoveResult.Fail:
                AddMessage($"{_session.buyerName}: Больше не могу, это моё последнее слово.");
                break;
            case HaggleMoveResult.BuyerLeft:
                AddMessage($"{_session.buyerName}: Всё, я передумал. Уехал.");
                break;
        }

        Refresh();
    }

    private void OnAccept()
    {
        if (_session == null || _session.finished) return;

        // BALANCE.md, 12.3: перед завершением сделки возможен сюрприз — срез цены на 8–18%.
        int finalPrice = ApplySurprise(_session.currentOffer, out float cut);
        if (cut > 0f)
            AddMessage($"Покупатель нашёл мелочь: -{Mathf.RoundToInt(cut * 100)}%.");

        CompleteSale(finalPrice);
    }

    private void OnLeave()
    {
        if (_session != null) _session.finished = true;
        Close();
    }

    private void CompleteSale(int finalPrice)
    {
        PlayerData.Instance.money += finalPrice;
        PlayerData.Instance.totalSalesCount++;
        PlayerData.Instance.garage.Remove(_currentCar);
        PlayerData.Instance.TickWorld(); // двигает тренд и стоянку (BALANCE.md, 12.2/12.4)
        UIManager.Instance?.UpdateTopBar();

        // Закрываем окно осмотра машины и обновляем гараж.
        GarageManager.Instance?.carDetailsModal?.Close();
        GarageManager.Instance?.RefreshGarage();

        Debug.Log($"Сделка с покупателем: {_currentCar.data.carName} за {finalPrice:N0} ₽");
        Close();

        SaveManager.Instance?.SaveAll();
    }

    // ───────────────────────── Логика торга (BALANCE.md, раздел 12.1) ─────────────────────────

    // Новая сессия: случайная личность покупателя, стартовое предложение, потолок, терпение, шанс.
    public static HaggleSession CreateSession(CarInstance car)
    {
        var s = new HaggleSession
        {
            personality = (BuyerPersonality)Random.Range(0, 5),
            // Точка отсчёта — справедливая цена машины (BALANCE.md, раздел 1, с учётом готовности).
            fairValue = PlayerData.Instance.CalculateSellPrice(car)
        };

        switch (s.personality)
        {
            // BALANCE.md, 12.1: старт ×0.70–0.82, потолок ×0.88, терпение 2, шанс успеха 48%.
            case BuyerPersonality.Stingy:
                s.buyerName = "Прижимистый";
                s.startMultMin = 0.70f; s.startMultMax = 0.82f;
                s.ceilingMult = 0.88f; s.successChance = 0.48f; s.patienceMax = 2;
                break;

            // BALANCE.md, 12.1: старт ×0.88–0.98, потолок ×1.02, терпение 3, шанс успеха 62%.
            case BuyerPersonality.Regular:
                s.buyerName = "Обычный";
                s.startMultMin = 0.88f; s.startMultMax = 0.98f;
                s.ceilingMult = 1.02f; s.successChance = 0.62f; s.patienceMax = 3;
                break;

            // BALANCE.md, 12.1: старт ×0.98–1.08, потолок ×1.15, терпение 3, шанс успеха 78%.
            case BuyerPersonality.Generous:
                s.buyerName = "Щедрый";
                s.startMultMin = 0.98f; s.startMultMax = 1.08f;
                s.ceilingMult = 1.15f; s.successChance = 0.78f; s.patienceMax = 3;
                break;

            // BALANCE.md, 12.1: старт ×0.82–0.96, потолок ×0.98, терпение 2, шанс успеха 42%.
            case BuyerPersonality.Impatient:
                s.buyerName = "Нетерпеливый";
                s.startMultMin = 0.82f; s.startMultMax = 0.96f;
                s.ceilingMult = 0.98f; s.successChance = 0.42f; s.patienceMax = 2;
                break;

            // BALANCE.md, 12.1: старт ×0.78–0.92, потолок ×1.10, терпение 4, шанс успеха 72%.
            default: // BuyerPersonality.Haggler
                s.buyerName = "Торгаш";
                s.startMultMin = 0.78f; s.startMultMax = 0.92f;
                s.ceilingMult = 1.10f; s.successChance = 0.72f; s.patienceMax = 4;
                break;
        }

        s.patienceLeft = s.patienceMax;
        s.currentOffer = Mathf.RoundToInt(s.fairValue * Random.Range(s.startMultMin, s.startMultMax));
        return s;
    }

    // Одна реплика торга (BALANCE.md, 12.1).
    // Мягкий торг «Может, дороже?» — 1 терпение.
    // Агрессивный «Маловато, подумай» — 2 терпения, шанс ниже на 20 п.п. (минимум 15%),
    // и 35% шанс, что покупатель уйдёт сразу при неудаче.
    public static HaggleMoveResult Haggle(HaggleSession s, bool aggressive)
    {
        if (s.finished) return HaggleMoveResult.BuyerLeft;

        int cost = aggressive ? 2 : 1;
        if (s.patienceLeft < cost) return HaggleMoveResult.NoPatience;

        s.patienceLeft -= cost;

        float chance = s.successChance - (aggressive ? 0.20f : 0f);
        chance = Mathf.Max(0.15f, chance); // BALANCE.md, 12.1: минимальный шанс успеха 15%.

        if (Random.value < chance)
        {
            int ceiling = Mathf.RoundToInt(s.fairValue * s.ceilingMult);
            s.currentOffer = Mathf.RoundToInt(Mathf.Lerp(s.currentOffer, ceiling, 0.5f));
            if (s.currentOffer > ceiling) s.currentOffer = ceiling; // не поднимаем выше потолка
            return HaggleMoveResult.Success;
        }

        // Неудача: агрессивный торг с шансом 35% заставляет покупателя уйти сразу.
        if (aggressive && Random.value < 0.35f)
        {
            s.finished = true;
            return HaggleMoveResult.BuyerLeft;
        }

        // Кончилось терпение — покупатель уходит.
        if (s.patienceLeft <= 0)
        {
            s.finished = true;
            return HaggleMoveResult.BuyerLeft;
        }

        return HaggleMoveResult.Fail;
    }

    // Сюрприз на сделке (BALANCE.md, 12.3): срез цены 8–18%. Шанс зависит от уровня Диагноста.
    public static int ApplySurprise(int price, out float cutPct)
    {
        cutPct = 0f;
        if (Random.value < PlayerData.Instance.SurpriseChance)
        {
            cutPct = Random.Range(0.08f, 0.18f);
            return Mathf.RoundToInt(price * (1f - cutPct));
        }
        return price;
    }
}
