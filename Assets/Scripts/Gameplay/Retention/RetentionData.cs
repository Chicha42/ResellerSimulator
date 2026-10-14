using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// Retention-система WheelDealer — модель данных.
// Источник: docs/ai-context/WheelDealer_retention.md (разделы 3–5, 8).
// Здесь только сериализуемые структуры и перечисления, без игровой логики.
// ─────────────────────────────────────────────────────────────────────────────

// Сложность дейли-миссии (retention, раздел 4): лёгкая даёт 0,5 Pz, средняя 1,0 Pz, сложная 1,5 Pz.
public enum MissionDifficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

// Типы миссий. id 0–4 — лёгкие, 5–9 — средние, 10–14 — сложные (retention, раздел 4).
public enum MissionType
{
    BuyTwoCars = 0,            // Лёгкая: купить 2 машины
    RepairThree = 1,           // Лёгкая: отремонтировать 3 дефекта
    SellOne = 2,               // Лёгкая: продать 1 машину
    BuyDiscountFive = 3,       // Лёгкая: купить со скидкой ≥5%
    InspectTwo = 4,            // Лёгкая: осмотреть 2 машины
    SellThree = 5,             // Средняя: продать 3 машины
    SellViaBuyerTwo = 6,       // Средняя: продать 2 машины через покупателей
    RepairSix = 7,             // Средняя: отремонтировать 6 дефектов
    BuyDiscountTen = 8,        // Средняя: купить со скидкой ≥10%
    SellProfitTwenty = 9,      // Средняя: продать с прибылью ≥20% от вложений
    BuyDiscountAndProfit = 10, // Сложная: купить со скидкой ≥15% и продать с прибылью ≥20%
    SellBuyerAbove110 = 11,    // Сложная: продать через покупателя не ниже 110% V
    DealProfitThirty = 12,     // Сложная: сделка с прибылью ≥30% от вложений
    EarnThreePz = 13,          // Сложная: заработать 3 Pz за день
    SellBuyerAboveFairThree = 14 // Сложная: 3 машины через покупателя не ниже V
}

// Состояние одной дейли-миссии. Сериализуется в сейв (retention, раздел 8).
[System.Serializable]
public class MissionSave
{
    public int type;         // MissionType
    public int difficulty;   // MissionDifficulty
    public int target;       // сколько нужно набрать (зависит от типа и Pz на момент выдачи)
    public int progress;     // текущий прогресс
    public bool claimed;     // награда получена
    public int zoneAtIssue;  // зона на момент выдачи — Pz и награда фиксируются по ней
    public int flag;         // вспомогательный флаг (нужен составной миссии «Мастер переговоров»)
}

// Полное сериализуемое состояние retention-системы (retention, раздел 8).
[System.Serializable]
public class RetentionSave
{
    // ── Daily Missions ──
    public int missionsDate;                 // дата выдачи в формате yyyyMMdd
    public List<MissionSave> missions = new List<MissionSave>();
    public int dayIncome;                    // заработано за день (для «Дневная выручка»)

    // ── Daily Reward (календарь входов) ──
    public int lastDailyClaimDate;            // дата последнего получения, yyyyMMdd
    public int dailyStreak;                   // номер дня серии 0..7

    // ── Achievements (счётчики, раздел 5) ──
    public int discountBuys10;                // покупки со скидкой ≥10%
    public int haggleBuys25;                  // успешные сделки с торгом при покупке
    public int segmentsBoughtMask;            // битовая маска купленных классов (зон) 1..5
    public bool boughtMostExpensive;          // куплена самая дорогая доступная модель
    public int dealsProfit25;                 // сделки с прибылью ≥25%
    public int consecutiveProfitable;         // текущая серия прибыльных сделок
    public bool reachedAutoBaron;             // достигнута репутация «Автобарон» (ур. 5)
    public bool reachedMillion;               // на счету когда-либо было ≥ 1 000 000 ₽
    public int claimedAchievementsMask;       // битовая маска полученных наград (15 бит)
}
