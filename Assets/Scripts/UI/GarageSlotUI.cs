using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GarageSlotUI : MonoBehaviour
{
    [Header("Контейнеры")]
    public GameObject filledView;
    public GameObject emptyView;

    [Header("Данные авто (Filled)")]
    public TextMeshProUGUI textModelName;
    public TextMeshProUGUI textCondition;
    public Image conditionBarFill;
    public TextMeshProUGUI textPrice;

    [Header("Бейджи")]
    public GameObject badgeDefects;
    public TextMeshProUGUI textDefectsCount;
    public GameObject badgeReady;

    [Header("Кнопки")]
    public Button btnOpenDetails;
    public Button btnGoToMarket;

    private CarInstance _car;

    // Настройка карточки с машиной
    public void SetupFilled(CarInstance car, System.Action<CarInstance> onSelect)
    {
        _car = car;
        filledView.SetActive(true);
        emptyView.SetActive(false);

        textModelName.text = car.data.carName;
        float repairProgress = PlayerData.Instance.CalculateRepairProgress(car);
        int conditionPercent = CurrentConditionPercent(car, repairProgress);
        textCondition.text = $"{ConditionName(conditionPercent)} · {conditionPercent}%";
        textPrice.text = $"{car.buyPrice:N0} ₽";

        conditionBarFill.fillAmount = conditionPercent / 100f;
        conditionBarFill.color = GetConditionColor(conditionPercent);

        // Бейджи дефектов / готовности
        int brokenCount = car.defects.FindAll(d => !d.isFixed).Count;
        if (brokenCount > 0)
        {
            badgeDefects.SetActive(true);
            badgeReady.SetActive(false);
            textDefectsCount.text = $"{brokenCount}";
        }
        else
        {
            badgeDefects.SetActive(false);
            badgeReady.SetActive(true);
        }

        btnOpenDetails.onClick.RemoveAllListeners();
        btnOpenDetails.onClick.AddListener(() => onSelect?.Invoke(_car));
    }

    private void Update() => RefreshRepairProgress();

    public void RefreshRepairProgress()
    {
        if (_car == null || filledView == null || !filledView.activeInHierarchy || PlayerData.Instance == null) return;
        float repairProgress = PlayerData.Instance.CalculateRepairProgress(_car);
        int pct = CurrentConditionPercent(_car, repairProgress);
        textCondition.text = $"{ConditionName(pct)} · {pct}%";
        conditionBarFill.fillAmount = pct / 100f;
        conditionBarFill.color = GetConditionColor(pct);
    }

    private static int CurrentConditionPercent(CarInstance car, float repairProgress)
    {
        return Mathf.Clamp(Mathf.RoundToInt(car.conditionPercent + Mathf.Clamp01(repairProgress) * (100 - car.conditionPercent)), 0, 100);
    }

    private static string ConditionName(int percent) => percent >= 90 ? "Отличное"
        : percent >= 70 ? "Хорошее"
        : percent >= 50 ? "Среднее"
        : percent >= 25 ? "Плохое" : "Убитое";

    // Настройка пустого слота с кнопкой перехода на рынок
    public void SetupEmpty()
    {
        filledView.SetActive(false);
        emptyView.SetActive(true);

        btnGoToMarket.onClick.RemoveAllListeners();
        btnGoToMarket.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowMarket();
        });
    }

    // Точные цвета состояний из HTML-баланса
    private Color GetConditionColor(int pct)
    {
        if (pct >= 90) return new Color32(0, 185, 84, 255);    // #00b954 Отличное
        if (pct >= 70) return new Color32(139, 224, 77, 255);  // #8be04d Хорошее
        if (pct >= 50) return new Color32(255, 210, 77, 255);  // #ffd24d Среднее
        if (pct >= 25) return new Color32(255, 157, 77, 255);  // #ff9d4d Плохое
        return new Color32(255, 92, 92, 255);                   // #ff5c5c Убитое
    }
}
