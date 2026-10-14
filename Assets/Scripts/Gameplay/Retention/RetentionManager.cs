using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// Retention-система WheelDealer — вся логика.
// Источник: docs/ai-context/WheelDealer_retention.md.
//   Daily Reward   — календарь входов на 7 дней (раздел 3)
//   Daily Missions — 3 миссии в день: лёгкая/средняя/сложная (раздел 4)
//   Achievements    — 15 достижений на всю игру (раздел 5)
// UI вынесен отдельно (Assets/Scripts/UI/RetentionUI.cs) — этот файл от отрисовки не зависит.
// ─────────────────────────────────────────────────────────────────────────────
public class RetentionManager : MonoBehaviour
{
    public static RetentionManager Instance { get; private set; }

    // Уведомление UI об изменении состояния и всплывающие сообщения (retention, раздел 7).
    public event Action OnChanged;
    public event Action<string> OnToast;

    private RetentionSave save = new RetentionSave();

    // Временная скидка на ремонт из календаря (день 3): храним здесь, PlayerData/формулу ремонта не трогаем.
    private int _repairDiscountUntilUnix;

    // Создать менеджер, если его ещё нет в сцене (вызывается из PlayerData.Awake).
    public static RetentionManager EnsureExists()
    {
        if (Instance == null)
        {
            var go = new GameObject("RetentionManager");
            go.AddComponent<RetentionManager>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        // Инициализация retention не должна ломать запуск игры.
        try
        {
            Normalize();
            RefreshDailyIfNeeded();
        }
        catch (Exception e)
        {
            Debug.LogError("[Retention] init: " + e);
        }
    }

    // ───────────────────────── Каталог и зона (раздел 2) ─────────────────────────

    private List<CarData> _cachedCatalog;

    // Открытые машины: у кого требуемое число продаж не больше текущего.
    private List<CarData> GetCatalog()
    {
        if (_cachedCatalog != null && _cachedCatalog.Count > 0) return _cachedCatalog;

        // Каталог рынка (Panel_Market может быть неактивна — ищем с учётом выключенных объектов).
        var mm = UnityEngine.Object.FindAnyObjectByType<MarketManager>(FindObjectsInactive.Include);
        if (mm != null && mm.catalog != null && mm.catalog.Count > 0)
        {
            _cachedCatalog = mm.catalog;
            return _cachedCatalog;
        }

        return null;
    }

    // Текущая зона = самая высокая зона среди открытых машин (retention, раздел 2).
    public int CurrentZone
    {
        get
        {
            int sales = PlayerData.Instance != null ? PlayerData.Instance.totalSalesCount : 0;
            int zone = 1;
            var catalog = GetCatalog();
            if (catalog != null)
            {
                foreach (var c in catalog)
                {
                    if (c == null) continue;
                    if (c.requiredSalesToUnlock <= sales && c.segment > zone) zone = c.segment;
                }
            }
            return Mathf.Clamp(zone, 1, 5);
        }
    }

    public int CurrentPz => RetentionCatalog.PzForZone(CurrentZone);

    private int MaxOpenBasePrice()
    {
        int sales = PlayerData.Instance != null ? PlayerData.Instance.totalSalesCount : 0;
        var catalog = GetCatalog();
        int max = 0;
        if (catalog != null)
        {
            foreach (var c in catalog)
                if (c != null && c.requiredSalesToUnlock <= sales && c.baseBuyPrice > max) max = c.baseBuyPrice;

            if (max == 0)
                foreach (var c in catalog)
                    if (c != null && c.baseBuyPrice > max) max = c.baseBuyPrice;
        }
        return max;
    }

    // ───────────────────────── Даты (раздел 3, сброс в 00:00) ─────────────────────────

    private static int DateCode(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;
    // Сброс миссий/календаря — по локальному времени (retention, разделы 3–4), без обращения к SDK.
    private static int TodayCode() => DateCode(DateTime.Now.Date);
    private static int YesterdayCode() => DateCode(DateTime.Now.Date.AddDays(-1));

    private static int NowUnixSeconds =>
        (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

    // ───────────────────────── Daily Missions (раздел 4) ─────────────────────────

    public int MissionCount => save.missions.Count;
    public MissionSave GetMission(int index) => (index >= 0 && index < save.missions.Count) ? save.missions[index] : null;

    public int MissionReward(MissionSave m)
    {
        if (m == null) return 0;
        float coef = RetentionCatalog.MissionRewardCoef((MissionDifficulty)m.difficulty);
        return Mathf.RoundToInt(coef * RetentionCatalog.PzForZone(m.zoneAtIssue));
    }

    public bool IsMissionReady(MissionSave m) => m != null && !m.claimed && m.progress >= m.target;

    public int ReadyMissionsCount
    {
        get
        {
            int n = 0;
            foreach (var m in save.missions) if (IsMissionReady(m)) n++;
            return n;
        }
    }

    // Пересоздать миссии, если наступил новый день (раздел 4: обновление в 00:00, невыполненные пропадают).
    public void RefreshDailyIfNeeded()
    {
        int today = TodayCode();
        if (save.missionsDate == today && save.missions.Count > 0) return;

        save.missionsDate = today;
        save.dayIncome = 0;
        GenerateMissions();
        Notify();
    }

    private void GenerateMissions()
    {
        save.missions = new List<MissionSave>();
        int zone = CurrentZone;
        int pz = RetentionCatalog.PzForZone(zone);

        for (int diff = 0; diff < RetentionCatalog.MissionPerDay; diff++)
        {
            // По одной миссии каждого уровня: id 0–4 лёгкие, 5–9 средние, 10–14 сложные.
            int typeId = UnityEngine.Random.Range(diff * 5, diff * 5 + 5);
            var type = (MissionType)typeId;
            save.missions.Add(new MissionSave
            {
                type = typeId,
                difficulty = diff,
                target = RetentionCatalog.MissionTarget(type, pz),
                progress = 0,
                claimed = false,
                zoneAtIssue = zone,
                flag = 0
            });
        }
    }

    public bool ClaimMission(int index)
    {
        if (index < 0 || index >= save.missions.Count) return false;
        var m = save.missions[index];
        if (m.claimed || m.progress < m.target) return false;

        m.claimed = true;
        PlayerData.Instance.money += MissionReward(m);
        UIManager.Instance?.UpdateTopBar();
        RaiseToast($"Награда за миссию: +{MissionReward(m):N0} ₽");
        CheckMoneyMilestone();
        Notify();
        return true;
    }

    private void AddMissionProgress(MissionType type, int amount)
    {
        bool newlyReady = false;
        foreach (var m in save.missions)
        {
            if ((MissionType)m.type != type || m.claimed) continue;
            int before = m.progress;
            m.progress = Mathf.Min(m.target, m.progress + amount);
            if (before < m.target && m.progress >= m.target) newlyReady = true;
        }
        if (newlyReady) RaiseToast("Миссия готова: " + RetentionCatalog.MissionNames[(int)type]);
    }

    // Флаг составной миссии «Мастер переговоров» (куплено со скидкой ≥15%).
    private void SetCombinedFlag()
    {
        foreach (var m in save.missions)
            if ((MissionType)m.type == MissionType.BuyDiscountAndProfit) m.flag = 1;
    }

    // Завершение «Мастер переговоров» при продаже с прибылью ≥20%, если флаг уже стоит.
    private void TryCompleteCombined()
    {
        foreach (var m in save.missions)
        {
            if ((MissionType)m.type != MissionType.BuyDiscountAndProfit) continue;
            if (m.claimed || m.flag != 1 || m.progress >= m.target) continue;
            m.progress = m.target;
            RaiseToast("Миссия готова: " + RetentionCatalog.MissionNames[(int)m.type]);
        }
    }

    // «Дневная выручка»: прогресс = заработанное за день.
    private void UpdateEarnMission()
    {
        foreach (var m in save.missions)
            if ((MissionType)m.type == MissionType.EarnThreePz && !m.claimed)
                m.progress = Mathf.Min(m.target, save.dayIncome);
    }

    // ───────────────────────── Daily Reward (раздел 3) ─────────────────────────

    public int LastDailyClaimDate => save.lastDailyClaimDate;
    public int DailyStreak => save.dailyStreak;
    public bool CanClaimDailyToday => save.lastDailyClaimDate != TodayCode();

    // Номер дня, который будет выдан при следующем получении (1..7). Пропуск дня обнуляет серию.
    public int NextDailyDay
    {
        get
        {
            if (save.lastDailyClaimDate != TodayCode() && save.lastDailyClaimDate != YesterdayCode()) return 1;
            return (save.dailyStreak % 7) + 1;
        }
    }

    // День 7 (машина) нельзя забрать без свободного слота в гараже (раздел 3).
    public bool IsDailyBlockedByGarage
    {
        get
        {
            var p = PlayerData.Instance;
            return NextDailyDay == 7 && p != null && p.garage.Count >= p.MaxGarageSlots;
        }
    }

    public bool DailyReady => CanClaimDailyToday && !IsDailyBlockedByGarage;

    // ── Временная скидка на ремонт (день 3) ──

    public float TempRepairDiscount => _repairDiscountUntilUnix > NowUnixSeconds ? 0.20f : 0f;

    // Применить временную скидку к уже посчитанной цене ремонта (скидка склада учтена в PlayerData).
    public int ApplyRepairDiscount(int cost)
    {
        if (TempRepairDiscount <= 0f) return cost;
        return Mathf.RoundToInt(cost * (1f - TempRepairDiscount));
    }

    // Подпись награды дня для UI (деньги зависят от текущей зоны).
    public string DailyRewardLabel(int day)
    {
        int zone = CurrentZone;
        int baseMoney = RetentionCatalog.DailyMoneyBase(zone);
        switch (day)
        {
            case 1: return $"Деньги · {baseMoney:N0} ₽";
            case 2: return "Репутация · +2 продажи";
            case 3: return "Скидка на ремонт −20% на 1 ч";
            case 4: return $"Деньги · {Mathf.RoundToInt(baseMoney * 2.5f):N0} ₽";
            case 5: return "Репутация · +4 продажи";
            case 6: return $"Деньги · {baseMoney * 5:N0} ₽";
            case 7: return "Случайная машина";
            default: return "";
        }
    }

    public bool ClaimDailyReward()
    {
        if (!CanClaimDailyToday || IsDailyBlockedByGarage) return false;

        // Пропуск дня — серия начинается с дня 1 (раздел 3).
        if (save.lastDailyClaimDate != YesterdayCode()) save.dailyStreak = 0;

        int day = save.dailyStreak % 7 + 1;
        ApplyDailyReward(day);

        save.dailyStreak = day;
        save.lastDailyClaimDate = TodayCode();

        CheckMoneyMilestone();
        Notify();
        return true;
    }

    private void ApplyDailyReward(int day)
    {
        var p = PlayerData.Instance;
        int zone = CurrentZone;
        int baseMoney = RetentionCatalog.DailyMoneyBase(zone);

        switch (day)
        {
            case 1:
                p.money += baseMoney;
                save.dayIncome += baseMoney;
                break;
            case 2:
                p.totalSalesCount += 2; // «+2 засчитанные продажи» (репутация)
                break;
            case 3:
                // Скидка на ремонт −20% на час. Храним тут, чтобы не менять PlayerData и формулу ремонта.
                _repairDiscountUntilUnix = NowUnixSeconds + 3600;
                break;
            case 4:
                p.money += Mathf.RoundToInt(baseMoney * 2.5f);
                break;
            case 5:
                p.totalSalesCount += 4;
                break;
            case 6:
                p.money += baseMoney * 5;
                break;
            case 7:
                GrantGiftCar();
                break;
        }

        UpdateEarnMission();
        UIManager.Instance?.UpdateTopBar();
    }

    // Подарочная машина (раздел 3): из открытых машин текущей зоны, состояние «Хорошее».
    private void GrantGiftCar()
    {
        var p = PlayerData.Instance;
        if (p == null || p.garage.Count >= p.MaxGarageSlots) return;

        var catalog = GetCatalog();
        if (catalog == null || catalog.Count == 0) return;

        int zone = CurrentZone;
        var pool = catalog.FindAll(c => c != null && c.requiredSalesToUnlock <= p.totalSalesCount && c.segment == zone);
        if (pool.Count == 0) pool = catalog.FindAll(c => c != null && c.requiredSalesToUnlock <= p.totalSalesCount);
        if (pool.Count == 0) pool = catalog;

        CarData data = pool[UnityEngine.Random.Range(0, pool.Count)];
        CarInstance car = CarInstance.CreateGiftCar(data, data.segment);
        p.garage.Add(car);

        GarageManager.Instance?.RefreshGarage();
        RaiseToast("Подарочная машина: " + data.carName);
    }

    // ───────────────────────── Achievements (раздел 5) ─────────────────────────

    public bool IsAchievementClaimed(int index)
    {
        if (index < 0 || index >= RetentionCatalog.AchievementCount) return false;
        return (save.claimedAchievementsMask & (1 << index)) != 0;
    }

    // Прогресс достижения: текущее и максимум (для прогресс-бара, раздел 7).
    public void GetAchievementProgress(int index, out int current, out int max)
    {
        var p = PlayerData.Instance;
        switch (index)
        {
            case 0: current = p != null ? p.totalSalesCount : 0; max = 1; break;              // Первый оборот
            case 1: current = save.discountBuys10; max = 1; break;                              // Торг начинается
            case 2: current = save.discountBuys10; max = 10; break;                             // Охотник за скидками
            case 3: current = save.haggleBuys25; max = 25; break;                               // Мастер переговоров
            case 4: current = PopCount(save.segmentsBoughtMask); max = 5; break;                // Коллекционер
            case 5: current = save.boughtMostExpensive ? 1 : 0; max = 1; break;                 // Вершина рынка
            case 6: current = SegmentsInGarage(); max = 5; break;                               // Полный гараж
            case 7: current = p != null ? p.diagnostLevel : 0; max = PlayerData.MaxDiagnostLevel; break;
            case 8: current = p != null ? p.warehouseLevel : 0; max = PlayerData.MaxWarehouseLevel; break;
            case 9: current = p != null ? p.garageLevel : 0; max = PlayerData.MaxGarageLevel; break;
            case 10: current = save.reachedMillion ? 1000000 : (p != null ? p.money : 0); max = 1000000; break; // Капиталист
            case 11: current = save.dealsProfit25; max = 10; break;                             // Железная маржа
            case 12: current = save.consecutiveProfitable; max = 10; break;                     // Без потерь
            case 13: current = save.reachedAutoBaron ? 1 : 0; max = 1; break;                   // Автобарон
            case 14: current = p != null ? p.totalSalesCount : 0; max = 100; break;             // Империя на колёсах
            default: current = 0; max = 1; break;
        }
    }

    public bool IsAchievementComplete(int index)
    {
        GetAchievementProgress(index, out int cur, out int max);
        return cur >= max;
    }

    public bool IsAchievementReady(int index) => IsAchievementComplete(index) && !IsAchievementClaimed(index);

    public int ReadyAchievementsCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < RetentionCatalog.AchievementCount; i++) if (IsAchievementReady(i)) n++;
            return n;
        }
    }

    public bool ClaimAchievement(int index)
    {
        if (index < 0 || index >= RetentionCatalog.AchievementCount) return false;
        if (!IsAchievementComplete(index) || IsAchievementClaimed(index)) return false;

        save.claimedAchievementsMask |= (1 << index);
        int reward = RetentionCatalog.AchievementRewards[index];
        PlayerData.Instance.money += reward;
        UIManager.Instance?.UpdateTopBar();
        RaiseToast($"Достижение: {RetentionCatalog.AchievementNames[index]} · +{reward:N0} ₽");
        CheckMoneyMilestone();
        Notify();
        return true;
    }

    private int SegmentsInGarage()
    {
        var p = PlayerData.Instance;
        if (p == null) return 0;
        int mask = 0;
        foreach (var car in p.garage)
            if (car != null) mask |= 1 << (Mathf.Clamp(car.segment, 1, 5) - 1);
        return PopCount(mask);
    }

    private static int PopCount(int mask)
    {
        int n = 0;
        while (mask != 0) { n += mask & 1; mask >>= 1; }
        return n;
    }

    // ───────────────────────── События геймплея (раздел 6) ─────────────────────────
    // Все события обёрнуты в Safe(): ошибка retention НИКОГДА не должна ломать базовые механики.

    private void Safe(string tag, Action action)
    {
        try { action(); }
        catch (Exception e) { Debug.LogError("[Retention] " + tag + ": " + e); }
    }

    // Покупка машины. listPrice — цена продавца (до скидки), paidPrice — сколько заплатили.
    public void OnCarBought(CarData data, int paidPrice, int listPrice) => Safe("OnCarBought", () =>
    {
        RefreshDailyIfNeeded();

        float discount = listPrice > 0 ? 1f - (float)paidPrice / listPrice : 0f;

        if (discount >= 0.10f) save.discountBuys10++;
        if (data != null)
        {
            save.segmentsBoughtMask |= 1 << (Mathf.Clamp(data.segment, 1, 5) - 1);
            if (data.baseBuyPrice >= MaxOpenBasePrice()) save.boughtMostExpensive = true;
        }

        AddMissionProgress(MissionType.BuyTwoCars, 1);
        if (discount >= 0.05f) AddMissionProgress(MissionType.BuyDiscountFive, 1);
        if (discount >= 0.10f) AddMissionProgress(MissionType.BuyDiscountTen, 1);
        if (discount >= 0.15f) SetCombinedFlag();

        CheckMoneyMilestone();
        Notify();
    });

    // Успешная сделка с торгом при покупке (для достижения «Мастер переговоров», раздел 5).
    public void OnPurchaseHaggled() => Safe("OnPurchaseHaggled", () =>
    {
        save.haggleBuys25++;
        Notify();
    });

    // Отремонтировано N дефектов.
    public void OnDefectsRepaired(int count) => Safe("OnDefectsRepaired", () =>
    {
        if (count <= 0) return;
        RefreshDailyIfNeeded();
        AddMissionProgress(MissionType.RepairThree, count);
        AddMissionProgress(MissionType.RepairSix, count);
        Notify();
    });

    // Осмотр машины (mission «Точный диагноз», раздел 4/6).
    public void OnCarInspected() => Safe("OnCarInspected", () =>
    {
        RefreshDailyIfNeeded();
        AddMissionProgress(MissionType.InspectTwo, 1);
        Notify();
    });

    // Продажа машины. received — сколько получили, viaBuyer — через покупателя, fairValue — справедливая цена V.
    public void OnCarSold(CarInstance car, int received, bool viaBuyer, int fairValue) => Safe("OnCarSold", () =>
    {
        RefreshDailyIfNeeded();
        if (car == null) { Notify(); return; }

        int invested = car.buyPrice + car.totalRepairPaid;
        int profit = received - invested;
        float profitPct = invested > 0 ? (float)profit / invested : 0f;
        bool aboveFair = viaBuyer && fairValue > 0 && received >= fairValue;
        bool above110 = viaBuyer && fairValue > 0 && received >= Mathf.RoundToInt(fairValue * 1.10f);
        var p = PlayerData.Instance;

        // Достижения.
        save.consecutiveProfitable = profit > 0 ? save.consecutiveProfitable + 1 : 0;
        if (profitPct >= 0.25f) save.dealsProfit25++;
        if (p != null && p.ReputationLevel >= 5) save.reachedAutoBaron = true;

        // Миссии.
        AddMissionProgress(MissionType.SellOne, 1);
        AddMissionProgress(MissionType.SellThree, 1);
        if (viaBuyer) AddMissionProgress(MissionType.SellViaBuyerTwo, 1);
        if (profitPct >= 0.20f) { AddMissionProgress(MissionType.SellProfitTwenty, 1); TryCompleteCombined(); }
        if (profitPct >= 0.30f) AddMissionProgress(MissionType.DealProfitThirty, 1);
        if (above110) AddMissionProgress(MissionType.SellBuyerAbove110, 1);
        if (aboveFair) AddMissionProgress(MissionType.SellBuyerAboveFairThree, 1);

        // «Дневная выручка».
        save.dayIncome += received;
        UpdateEarnMission();

        CheckMoneyMilestone();
        Notify();
    });

    // Куплено улучшение — достижения «На все руки»/«Торговая база»/«Гараж мечты» читают уровни напрямую.
    public void OnUpgradePurchased() => Safe("OnUpgradePurchased", () =>
    {
        var p = PlayerData.Instance;
        if (p != null && p.ReputationLevel >= 5) save.reachedAutoBaron = true;
        CheckMoneyMilestone();
        Notify();
    });

    // Любое изменение денег на счету (для «Капиталиста» и бейджей).
    public void OnMoneyChanged()
    {
        CheckMoneyMilestone();
        Notify();
    }

    // Достижение «Капиталист» фиксируется, как только на счету появился миллион (раздел 5).
    private void CheckMoneyMilestone()
    {
        var p = PlayerData.Instance;
        if (p != null && p.money >= 1000000) save.reachedMillion = true;
    }

    // ───────────────────────── Сохранение (раздел 8) ─────────────────────────

    private void Normalize()
    {
        if (save == null) save = new RetentionSave();
        if (save.missions == null) save.missions = new List<MissionSave>();
    }

    public RetentionSave Export()
    {
        Normalize();
        return save;
    }

    public void Import(RetentionSave data)
    {
        if (data == null) { ResetAll(); return; }
        save = data;
        Normalize();
        RefreshDailyIfNeeded();
        Notify();
    }

    public void ResetAll()
    {
        save = new RetentionSave();
        save.missionsDate = 0; // заставит RefreshDailyIfNeeded выдать миссии заново
        Normalize();
        RefreshDailyIfNeeded();
        Notify();
    }

    // ───────────────────────── Служебное ─────────────────────────

    private void Notify() => OnChanged?.Invoke();
    private void RaiseToast(string msg) => OnToast?.Invoke(msg);
}
