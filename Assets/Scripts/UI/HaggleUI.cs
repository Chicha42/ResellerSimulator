using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Чат торга с покупателем (BALANCE.md, раздел 12.1). Строится в рантайме в стиле прототипа.
public class HaggleUI : MonoBehaviour
{
    public static HaggleUI Instance { get; private set; }

    private const int ReopenCooldownTicks = 5; // Раздел 12.1: новый покупатель не раньше 5 действий

    private TextMeshProUGUI _title, _offer, _patience, _chat, _info;
    private Button _btnSoft, _btnHard, _btnAccept, _btnLeave, _btnClose;

    private readonly List<string> _messages = new List<string>();
    private HaggleSession _session;
    private CarInstance _currentCar;

    public void Build(Transform parent)
    {
        Instance = this;
        var rt = (RectTransform)transform;
        transform.SetParent(parent, false);
        UIFactory.Stretch(rt);

        var dim = UIFactory.Image("Dim", transform, UIFactory.Overlay);
        UIFactory.Stretch(dim.rectTransform);

        var card = UIFactory.Image("Card", transform, UIFactory.PanelBg);
        var crt = card.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(880f, 640f);

        _title = UIFactory.Text(crt, "Title", "", 30, UIFactory.TextMain);
        AnchorTop(_title.rectTransform, -20f, 820f, 40f);

        _offer = UIFactory.Text(crt, "Offer", "", 38, UIFactory.Yellow);
        AnchorTop(_offer.rectTransform, -68f, 820f, 50f);

        _patience = UIFactory.Text(crt, "Patience", "", 20, UIFactory.TextMuted);
        AnchorTop(_patience.rectTransform, -122f, 820f, 28f);

        var chatBg = UIFactory.Image("Chat", crt, UIFactory.CardBg);
        var chr = chatBg.rectTransform;
        chr.anchorMin = chr.anchorMax = new Vector2(0.5f, 0.5f);
        chr.pivot = new Vector2(0.5f, 0.5f);
        chr.sizeDelta = new Vector2(830f, 240f);
        chr.anchoredPosition = new Vector2(0f, 0f);

        _chat = UIFactory.Text(chr, "ChatText", "", 20, UIFactory.TextMain, TextAlignmentOptions.TopLeft);
        var cr = _chat.rectTransform;
        UIFactory.Stretch(cr, 18f);

        _info = UIFactory.Text(crt, "Info", "", 18, UIFactory.TextMuted);
        AnchorBottom(_info.rectTransform, 22f, 820f, 34f);

        _btnAccept = UIFactory.Button(crt, "Btn_Accept", "Продать", UIFactory.Blue, Color.white, new Vector2(380f, 56f), OnAccept);
        AnchorBottom(_btnAccept.GetComponent<RectTransform>(), 84f, 380f, 56f, -200f);

        _btnLeave = UIFactory.Button(crt, "Btn_Leave", "Уйти", UIFactory.RowBg, UIFactory.TextMain, new Vector2(380f, 56f), OnLeave);
        AnchorBottom(_btnLeave.GetComponent<RectTransform>(), 84f, 380f, 56f, 200f);

        _btnSoft = UIFactory.Button(crt, "Btn_Soft", "💬 Может, дороже?", UIFactory.Green, Color.white, new Vector2(380f, 56f), () => OnHaggle(false), 20);
        AnchorBottom(_btnSoft.GetComponent<RectTransform>(), 150f, 380f, 56f, -200f);

        _btnHard = UIFactory.Button(crt, "Btn_Hard", "😏 Маловато, подумай", UIFactory.Red, Color.white, new Vector2(380f, 56f), () => OnHaggle(true), 20);
        AnchorBottom(_btnHard.GetComponent<RectTransform>(), 150f, 380f, 56f, 200f);

        _btnClose = UIFactory.Button(crt, "Btn_Close", "✕", UIFactory.RowBg, UIFactory.TextMain, new Vector2(52f, 52f), Close, 24);
        var cl = _btnClose.GetComponent<RectTransform>();
        cl.anchorMin = cl.anchorMax = new Vector2(1f, 1f);
        cl.pivot = new Vector2(1f, 1f);
        cl.anchoredPosition = new Vector2(-14f, -14f);

        gameObject.SetActive(false);
    }

    private static void AnchorTop(RectTransform rt, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private static void AnchorBottom(RectTransform rt, float y, float w, float h, float x = 0f)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    public void Open(CarInstance car)
    {
        _currentCar = car;
        _messages.Clear();
        _session = null;

        // Раздел 6.1: переоткрытие не раньше, чем через 5 тактов после создания сессии
        int readyAt = PlayerData.Instance.lastHaggleSessionTick + ReopenCooldownTicks;
        if (PlayerData.Instance.actionTick < readyAt)
        {
            int wait = readyAt - PlayerData.Instance.actionTick;
            _title.text = "Покупателей пока нет";
            _offer.text = "";
            _patience.text = "";
            _chat.text = "";
            _info.text = $"Новый покупатель появится через {wait} действ.";
            SetActionButtons(false, false, false);
            gameObject.SetActive(true);
            return;
        }

        PlayerData.Instance.lastHaggleSessionTick = PlayerData.Instance.actionTick;
        _session = HaggleManager.CreateSession(car);

        AddMessage($"{_session.buyerName} покупатель: Здравствуйте! Заберу за {_session.currentOffer:N0} ₽.");
        _info.text = "";
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
        // Держим последние 6 сообщений, чтобы влезало в окошко
        while (_messages.Count > 6) _messages.RemoveAt(0);
    }

    private void Refresh()
    {
        if (_session == null) return;

        _title.text = $"Покупатель: {_session.buyerName}";
        _offer.text = $"Текущее предложение: {_session.currentOffer:N0} ₽";
        _patience.text = $"Терпение: {_session.patienceLeft}/{_session.patienceMax}";
        _chat.text = string.Join("\n", _messages);

        bool alive = !_session.finished;
        bool canSoft = alive && _session.patienceLeft >= 1;
        bool canHard = alive && _session.patienceLeft >= 2;
        SetActionButtons(canSoft, canHard, alive);
    }

    private void SetActionButtons(bool soft, bool hard, bool accept)
    {
        _btnSoft.interactable = soft;
        _btnHard.interactable = hard;
        _btnAccept.interactable = accept;
        _btnAccept.GetComponentInChildren<TextMeshProUGUI>().text =
            _session != null ? $"Продать за {_session.currentOffer:N0} ₽" : "Продать";
    }

    private void OnHaggle(bool aggressive)
    {
        if (_session == null || _session.finished) return;

        AddMessage($"Вы: {(aggressive ? "😏 Маловато, подумай" : "💬 Может, дороже?")}");

        var result = HaggleManager.Haggle(_session, aggressive);
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

        int finalPrice = HaggleManager.ApplySurprise(_session.currentOffer, out float cut);
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
        PlayerData.Instance.TickWorld();
        UIManager.Instance?.UpdateTopBar();

        // Закрываем окно осмотра машины и обновляем гараж
        GarageManager.Instance?.carDetailsModal?.Close();
        GarageManager.Instance?.RefreshGarage();

        Debug.Log($"Сделка с покупателем: {_currentCar.data.carName} за {finalPrice:N0} ₽");
        Close();
    }
}
