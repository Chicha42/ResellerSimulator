using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Logic for scene-authored buyer rows; does not create UI objects.</summary>
public sealed class SaleSetupPanelUI : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI warningText;
    public TextMeshProUGUI[] buyerDetails;
    public Button[] buyerButtons;
    public TextMeshProUGUI[] buyerButtonLabels;
    public Button closeButton;
    public HaggleManager haggleManager;

    private CarInstance _car;
    private SaleListingData _listing;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Open(CarInstance car, SaleListingData listing)
    {
        _car = car;
        _listing = listing;
        int floor = PlayerData.RoundPrice(PlayerData.Instance.CalculateAsIsPrice(car) * 1.05f);
        int cap = PlayerData.RoundPrice(listing.fairValue * 2f);
        valueText.text = $"Справедливая цена: {listing.fairValue:N0} ₽ · минимум предложения: {floor:N0} ₽ · верхняя граница: {cap:N0} ₽";
        bool invalidPrice = listing.askingPrice > cap || listing.askingPrice < floor;
        if (listing.askingPrice > cap)
            warningText.text = "Цена слишком высокая. Покупатели не ответят.";
        else if (listing.askingPrice < floor)
            warningText.text = $"Цена ниже минимального предложения {floor:N0} ₽.";
        else
            warningText.text = $"Ваша цена: {listing.askingPrice:N0} ₽. Состав покупателей сохранится при смене цены.";

        for (int i = 0; i < buyerButtons.Length; i++)
        {
            var button = buyerButtons[i];
            var details = buyerDetails[i];
            var label = buyerButtonLabels[i];
            var buyer = i < listing.buyers.Count ? listing.buyers[i] : null;
            button.onClick.RemoveAllListeners();
            button.transform.parent.gameObject.SetActive(!invalidPrice);
            button.interactable = !invalidPrice && buyer != null && buyer.responded;
            if (invalidPrice)
            {
                details.text = "";
                label.text = "";
                continue;
            }
            if (buyer == null)
            {
                details.text = "";
                label.text = "Нет покупателя";
                continue;
            }

            details.text = buyer.responded
                ? $"{buyer.buyerName} · предложение {buyer.currentOffer:N0} ₽ · максимум {buyer.maxAcceptablePrice:N0} ₽"
                : $"{buyer.buyerName} · нет отклика · шанс {Mathf.RoundToInt(buyer.responseChance * 100f)}%";
            label.text = buyer.responded ? "Открыть торг" : "Нет отклика";
            if (buyer.responded)
                button.onClick.AddListener(() => OpenBuyer(buyer));
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close() => gameObject.SetActive(false);

    private void OpenBuyer(HaggleSession buyer)
    {
        Close();
        if (haggleManager != null) haggleManager.Open(_car, buyer);
    }
}
