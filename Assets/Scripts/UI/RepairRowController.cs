using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

/// <summary>Logic only: binds controls already present in DefectRowPrefab.</summary>
public sealed class RepairRowController : MonoBehaviour
{
    private CarInstance _car;
    private DefectInfo _defect;
    private int _cost;
    private TextMeshProUGUI _label;
    private Button _button;
    private TextMeshProUGUI _buttonLabel;

    public void Setup(CarInstance car, DefectInfo defect, int cost)
    {
        _car = car;
        _defect = defect;
        _cost = cost;
        _label = transform.Find("Text_DefectRow")?.GetComponent<TextMeshProUGUI>();
        _button = transform.Find("Btn_DefectRow")?.GetComponent<Button>();
        _buttonLabel = _button != null ? _button.GetComponentInChildren<TextMeshProUGUI>() : null;
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnPressed);
        }
        Refresh();
    }

    private void Update() { Refresh(); }

    private void OnPressed()
    {
        if (_defect == null) return;
        if (_defect.isRepairing)
        {
            YG2.RewardedAdvShow("instant_repair", () => RepairManager.Instance?.CompleteImmediately(_car, _defect));
            return;
        }
        RepairManager.Instance?.StartRepair(_car, _defect, _cost);
    }

    private void Refresh()
    {
        if (_defect == null || _label == null || _button == null) return;
        if (_defect.isFixed)
        {
            _label.text = $"{_defect.defectName} — Отремонтировано";
            _button.gameObject.SetActive(false);
            return;
        }

        _button.gameObject.SetActive(true);
        if (_defect.isRepairing)
        {
            _label.text = $"{_defect.defectName} — ремонт: {Mathf.CeilToInt(_defect.repairSecondsRemaining)} с";
            _button.interactable = true;
            if (_buttonLabel != null) _buttonLabel.text = "Завершить · реклама";
        }
        else
        {
            _label.text = $"{_defect.defectName} — {_cost:N0} ₽";
            _button.interactable = PlayerData.Instance != null && PlayerData.Instance.money >= _cost
                && RepairManager.Instance != null && !RepairManager.Instance.IsRepairing(_car);
            if (_buttonLabel != null) _buttonLabel.text = "Чинить";
        }
    }
}
