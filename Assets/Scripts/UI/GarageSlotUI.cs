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
        textCondition.text = $"{car.conditionName} · {car.conditionPercent}%";
        textPrice.text = $"{car.buyPrice:N0} ₽";

        // Заполнение полоски и цвет по файлу баланса
        conditionBarFill.fillAmount = car.conditionPercent / 100f;
        conditionBarFill.color = GetConditionColor(car.conditionPercent);

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