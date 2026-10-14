// ─────────────────────────────────────────────────────────────────────────────
// Retention-система WheelDealer — статические таблицы.
// Источник: docs/ai-context/WheelDealer_retention.md (разделы 2, 4, 5).
// Все числа вынесены сюда, чтобы править баланс без правки логики.
// ─────────────────────────────────────────────────────────────────────────────
public static class RetentionCatalog
{
    // ── Pz — средняя прибыль одной сделки в зоне (retention, раздел 2). Индекс = зона 1..5. ──
    private static readonly int[] PzByZone = { 0, 3500, 11000, 13500, 32000, 118000 };

    public static int PzForZone(int zone)
    {
        return (zone >= 1 && zone <= 5) ? PzByZone[zone] : PzByZone[1];
    }

    // ── Денежная база календаря = max(10 000; 0,5 Pz текущей зоны) (retention, раздел 3). ──
    public static int DailyMoneyBase(int zone)
    {
        int half = UnityEngine.Mathf.RoundToInt(0.5f * PzForZone(zone));
        return UnityEngine.Mathf.Max(10000, half);
    }

    // ── Дейли-миссии (retention, раздел 4) ──
    public static readonly string[] MissionNames =
    {
        "Покупатель дня",       // 0 BuyTwoCars
        "Мастер на все руки",   // 1 RepairThree
        "Первая сделка дня",    // 2 SellOne
        "Удачный торг",         // 3 BuyDiscountFive
        "Точный диагноз",       // 4 InspectTwo
        "Бойкая торговля",      // 5 SellThree
        "Без спешки",           // 6 SellViaBuyerTwo
        "Ремонтная смена",      // 7 RepairSix
        "Торг уместен",         // 8 BuyDiscountTen
        "Маржа по плану",       // 9 SellProfitTwenty
        "Мастер переговоров",   // 10 BuyDiscountAndProfit
        "Без компромиссов",     // 11 SellBuyerAbove110
        "Идеальный баланс",     // 12 DealProfitThirty
        "Дневная выручка",      // 13 EarnThreePz
        "Цена не ниже рынка"    // 14 SellBuyerAboveFairThree
    };

    public static readonly string[] MissionConditions =
    {
        "Купить 2 машины",
        "Отремонтировать 3 дефекта",
        "Продать 1 машину",
        "Купить машину со скидкой ≥5%",
        "Осмотреть 2 машины",
        "Продать 3 машины",
        "Продать 2 машины через покупателей",
        "Отремонтировать 6 дефектов",
        "Купить машину со скидкой ≥10%",
        "Продать машину с прибылью ≥20% от вложений",
        "Купить со скидкой ≥15% и продать с прибылью ≥20%",
        "Продать через покупателя не ниже 110% V",
        "Завершить сделку с прибылью ≥30% от вложений",
        "Заработать 3 Pz за день",
        "Продать 3 машины через покупателей не ниже V"
    };

    // Цель миссии. pzAtIssue — Pz зоны на момент выдачи (фиксируется, retention раздел 4).
    public static int MissionTarget(MissionType type, int pzAtIssue)
    {
        switch (type)
        {
            case MissionType.BuyTwoCars: return 2;
            case MissionType.RepairThree: return 3;
            case MissionType.SellOne: return 1;
            case MissionType.BuyDiscountFive: return 1;
            case MissionType.InspectTwo: return 2;
            case MissionType.SellThree: return 3;
            case MissionType.SellViaBuyerTwo: return 2;
            case MissionType.RepairSix: return 6;
            case MissionType.BuyDiscountTen: return 1;
            case MissionType.SellProfitTwenty: return 1;
            case MissionType.BuyDiscountAndProfit: return 1;
            case MissionType.SellBuyerAbove110: return 1;
            case MissionType.DealProfitThirty: return 1;
            case MissionType.EarnThreePz: return pzAtIssue * 3; // «заработать 3 Pz за день»
            case MissionType.SellBuyerAboveFairThree: return 3;
            default: return 1;
        }
    }

    // Коэффициент награды миссии (retention, раздел 4): лёгкая 0,5 Pz, средняя 1,0 Pz, сложная 1,5 Pz.
    public static float MissionRewardCoef(MissionDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MissionDifficulty.Easy: return 0.5f;
            case MissionDifficulty.Medium: return 1.0f;
            case MissionDifficulty.Hard: return 1.5f;
            default: return 0f;
        }
    }

    // ── Достижения (retention, раздел 5). Индексы 0..14. ──
    public static readonly string[] AchievementNames =
    {
        "Первый оборот",
        "Торг начинается",
        "Охотник за скидками",
        "Мастер переговоров",
        "Коллекционер",
        "Вершина рынка",
        "Полный гараж",
        "На все руки",
        "Торговая база",
        "Гараж мечты",
        "Капиталист",
        "Железная маржа",
        "Без потерь",
        "Автобарон",
        "Империя на колёсах"
    };

    public static readonly string[] AchievementConditions =
    {
        "Завершить первую продажу машины",
        "Купить машину со скидкой ≥10%",
        "10 покупок со скидкой ≥10%",
        "25 успешных сделок с торгом при покупке",
        "Купить по одной машине каждого класса (зоны)",
        "Купить самую дорогую доступную модель",
        "Иметь в гараже машины всех классов (зон)",
        "Диагност на максимальном уровне",
        "Склад запчастей на максимальном уровне",
        "Гараж на максимальном уровне",
        "Иметь на счету одновременно 1 000 000 ₽",
        "10 сделок с прибылью ≥25% от вложений",
        "10 сделок подряд с положительной прибылью",
        "Достичь репутации «Автобарон»",
        "Продать 100 машин"
    };

    // Фиксированные награды достижений в рублях (retention, раздел 5). Сумма = 1 025 000 ₽.
    public static readonly int[] AchievementRewards =
    {
        5000, 15000, 35000, 60000, 75000, 100000, 50000, 50000, 50000,
        75000, 100000, 75000, 75000, 150000, 110000
    };

    public const int AchievementCount = 15;
    public const int MissionPerDay = 3;
}
