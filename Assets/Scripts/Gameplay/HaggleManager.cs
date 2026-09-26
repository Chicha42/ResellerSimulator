using UnityEngine;

// Модель торга с покупателем (BALANCE.md, раздел 12.1).
public enum BuyerPersonality
{
    Prigimisty,   // Прижимистый
    Obychny,      // Обычный
    Shchedry,     // Щедрый
    Neterpelivy,  // Нетерпеливый
    Torgash       // Торгаш
}

public class HaggleSession
{
    public BuyerPersonality personality;
    public string buyerName;
    public float startMultMin;
    public float startMultMax;
    public float ceilingMult;     // Потолок цены, × от справедливой
    public float successChance;   // Базовый шанс успеха торга
    public int patienceMax;
    public int patienceLeft;
    public int fairValue;         // Справедливая цена машины
    public int currentOffer;      // Текущее предложение покупателя
    public bool finished;         // Покупатель ушёл / сделка закрыта
}

public enum HaggleMoveResult
{
    Success,      // Покупатель поднял цену
    Fail,         // Отказ, но покупатель остался
    BuyerLeft,    // Покупатель обиделся и ушёл
    NoPatience    // Не хватает терпения на реплику
}

public static class HaggleManager
{
    public static HaggleSession CreateSession(CarInstance car)
    {
        var s = new HaggleSession
        {
            personality = (BuyerPersonality)Random.Range(0, 5),
            fairValue = PlayerData.Instance.CalculateSellPrice(car)
        };

        switch (s.personality)
        {
            case BuyerPersonality.Prigimisty:
                s.buyerName = "Прижимистый";
                s.startMultMin = 0.70f; s.startMultMax = 0.82f;
                s.ceilingMult = 0.88f; s.successChance = 0.48f; s.patienceMax = 2;
                break;
            case BuyerPersonality.Obychny:
                s.buyerName = "Обычный";
                s.startMultMin = 0.88f; s.startMultMax = 0.98f;
                s.ceilingMult = 1.02f; s.successChance = 0.62f; s.patienceMax = 3;
                break;
            case BuyerPersonality.Shchedry:
                s.buyerName = "Щедрый";
                s.startMultMin = 0.98f; s.startMultMax = 1.08f;
                s.ceilingMult = 1.15f; s.successChance = 0.78f; s.patienceMax = 3;
                break;
            case BuyerPersonality.Neterpelivy:
                s.buyerName = "Нетерпеливый";
                s.startMultMin = 0.82f; s.startMultMax = 0.96f;
                s.ceilingMult = 0.98f; s.successChance = 0.42f; s.patienceMax = 2;
                break;
            default: // Torgash
                s.buyerName = "Торгаш";
                s.startMultMin = 0.78f; s.startMultMax = 0.92f;
                s.ceilingMult = 1.10f; s.successChance = 0.72f; s.patienceMax = 4;
                break;
        }

        s.patienceLeft = s.patienceMax;
        s.currentOffer = Mathf.RoundToInt(s.fairValue * Random.Range(s.startMultMin, s.startMultMax));
        return s;
    }

    // Одна реплика торга. aggressive: «Маловато, подумай» (2 терпения, шанс -20 п.п.)
    public static HaggleMoveResult Haggle(HaggleSession s, bool aggressive)
    {
        if (s.finished) return HaggleMoveResult.BuyerLeft;

        int cost = aggressive ? 2 : 1;
        if (s.patienceLeft < cost) return HaggleMoveResult.NoPatience;

        s.patienceLeft -= cost;

        float chance = s.successChance - (aggressive ? 0.20f : 0f);
        chance = Mathf.Max(0.15f, chance);

        if (Random.value < chance)
        {
            int ceiling = Mathf.RoundToInt(s.fairValue * s.ceilingMult);
            s.currentOffer = Mathf.RoundToInt(Mathf.Lerp(s.currentOffer, ceiling, 0.5f));
            // Не поднимаем выше потолка
            if (s.currentOffer > ceiling) s.currentOffer = ceiling;
            return HaggleMoveResult.Success;
        }

        // Неудача
        if (aggressive && Random.value < 0.35f)
        {
            s.finished = true;
            return HaggleMoveResult.BuyerLeft;
        }

        if (s.patienceLeft <= 0)
        {
            s.finished = true;
            return HaggleMoveResult.BuyerLeft;
        }

        return HaggleMoveResult.Fail;
    }

    // Сюрприз на сделке (BALANCE.md, раздел 12.3): срез цены 8-18%
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
