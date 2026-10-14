using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
// Retention-система WheelDealer — ОТРИСОВКА UI (runtime).
// Источник: docs/ai-context/WheelDealer_retention.md (раздел 7).
//
// ВАЖНО: здесь только создание и обновление UI. Всю логику держит RetentionManager.
// Когда нарисуешь эти экраны в сцене — просто удали этот файл и вызов Build()
// в UIManager.Start(): логика и сохранения останутся работать.
// ─────────────────────────────────────────────────────────────────────────────
public class RetentionUI : MonoBehaviour
{
    public static RetentionUI Instance { get; private set; }

    private RectTransform _root;
    private GameObject _bottomBar;
    private Button _btnMissions, _btnAchievements, _btnDaily;

    private GameObject _panelMissions, _panelAchievements, _panelDaily;
    private RectTransform _contentMissions, _contentAchievements, _contentDaily;
    private Button _btnClaimDaily;
    private TextMeshProUGUI _dailyTitle;

    private CanvasGroup _toastGroup;
    private TextMeshProUGUI _toastText;
    private Coroutine _toastRoutine;

    private static readonly Vector2 ModalSize = new Vector2(980f, 680f);

    // ───────────────────────── Построение ─────────────────────────

    public void Build(Transform parent)
    {
        Instance = this;

        _root = (RectTransform)transform;
        transform.SetParent(parent, false);
        UIFactory.Stretch(_root);
        transform.SetAsLastSibling(); // нижние кнопки и модалки поверх остальных панелей

        BuildBottomBar();
        BuildListPanel("Modal_Missions", "Ежедневные миссии", out _panelMissions, out _contentMissions);
        BuildListPanel("Modal_Achievements", "Достижения", out _panelAchievements, out _contentAchievements);
        BuildDailyPanel();
        BuildToast();

        var rm = RetentionManager.EnsureExists();
        rm.OnChanged += RefreshAll;
        rm.OnToast += ShowToast;

        RefreshAll();
    }

    private void OnDestroy()
    {
        var rm = RetentionManager.Instance;
        if (rm != null)
        {
            rm.OnChanged -= RefreshAll;
            rm.OnToast -= ShowToast;
        }
        if (Instance == this) Instance = null;
    }

    // Три кнопки внизу по центру — не задевают верхний бар и списки панелей.
    private void BuildBottomBar()
    {
        var bar = UIFactory.Image("RetentionBar", _root, new Color(0f, 0f, 0f, 0.45f));
        _bottomBar = bar.gameObject;
        bar.raycastTarget = false; // клики блокируют только сами кнопки, не подложка
        var rt = bar.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(880f, 78f);
        rt.anchoredPosition = new Vector2(0f, 12f);

        _btnMissions = UIFactory.Button(_bottomBar.transform, "Btn_Missions", "Миссии",
            UIFactory.RowBg, UIFactory.TextMain, new Vector2(276f, 60f), () => TogglePanel(_panelMissions));
        PlaceInBar(_btnMissions, -292f);

        _btnAchievements = UIFactory.Button(_bottomBar.transform, "Btn_Achievements", "Достижения",
            UIFactory.RowBg, UIFactory.TextMain, new Vector2(276f, 60f), () => TogglePanel(_panelAchievements));
        PlaceInBar(_btnAchievements, 0f);

        _btnDaily = UIFactory.Button(_bottomBar.transform, "Btn_Daily", "Награда",
            UIFactory.RowBg, UIFactory.TextMain, new Vector2(276f, 60f), () => TogglePanel(_panelDaily));
        PlaceInBar(_btnDaily, 292f);
    }

    private static void PlaceInBar(Button b, float x)
    {
        var rt = b.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    // Полноэкранная модалка со скролл-списком (для миссий и достижений).
    private void BuildListPanel(string name, string title, out GameObject panel, out RectTransform content)
    {
        panel = CreateModal(name, title, out RectTransform card);
        content = CreateScroll(card, ModalSize.y - 150f);
        panel.SetActive(false);
    }

    private GameObject CreateModal(string name, string title, out RectTransform card)
    {
        var panel = UIFactory.NewUIObject(name, _root);
        UIFactory.Stretch((RectTransform)panel.transform);

        var dim = UIFactory.Image("Dim", panel.transform, UIFactory.Overlay);
        UIFactory.Stretch(dim.rectTransform);

        var cardImg = UIFactory.Image("Card", panel.transform, UIFactory.PanelBg);
        card = cardImg.rectTransform;
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = ModalSize;

        var titleText = UIFactory.Text(card, "Title", title, 34, UIFactory.TextMain);
        Point(titleText.rectTransform, 0.5f, 1f, 0.5f, 1f, 0f, -18f, ModalSize.x - 120f, 46f);

        var close = UIFactory.Button(card, "Btn_Close", "✕", UIFactory.RowBg, UIFactory.TextMain,
            new Vector2(52f, 52f), () => panel.SetActive(false), 24);
        Point(close.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -16f, -16f, 52f, 52f);

        return panel;
    }

    private RectTransform CreateScroll(RectTransform card, float height)
    {
        var scrollGo = UIFactory.NewUIObject("Scroll", card);
        var srt = (RectTransform)scrollGo.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.sizeDelta = new Vector2(ModalSize.x - 60f, height);
        srt.anchoredPosition = new Vector2(0f, -14f);

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        var viewport = UIFactory.NewUIObject("Viewport", scrollGo.transform);
        var vrt = (RectTransform)viewport.transform;
        UIFactory.Stretch(vrt);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = vrt;

        var content = UIFactory.NewUIObject("Content", viewport.transform);
        var crt = (RectTransform)content.transform;
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = crt;
        return crt;
    }

    private GameObject CreateListRow(Transform content, float height)
    {
        var row = UIFactory.Image("Row", content, UIFactory.CardBg);
        var rt = row.rectTransform;
        rt.sizeDelta = new Vector2(0f, height);
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        return row.gameObject;
    }

    // ───────────────────────── Обновление ─────────────────────────

    private void RefreshAll()
    {
        var rm = RetentionManager.Instance;
        if (rm == null) return;

        int mReady = rm.ReadyMissionsCount;
        int aReady = rm.ReadyAchievementsCount;
        UIFactory.SetButtonLabel(_btnMissions, mReady > 0 ? $"Миссии · {mReady}" : "Миссии");
        UIFactory.SetButtonLabel(_btnAchievements, aReady > 0 ? $"Достижения · {aReady}" : "Достижения");
        UIFactory.SetButtonLabel(_btnDaily, rm.DailyReady ? "Награда ●" : "Награда");

        if (_panelMissions != null && _panelMissions.activeSelf) RebuildMissions();
        if (_panelAchievements != null && _panelAchievements.activeSelf) RebuildAchievements();
        if (_panelDaily != null && _panelDaily.activeSelf) RebuildDaily();
    }

    private void RebuildMissions()
    {
        ClearContent(_contentMissions);
        var rm = RetentionManager.Instance;
        rm.RefreshDailyIfNeeded();

        for (int i = 0; i < rm.MissionCount; i++)
        {
            var m = rm.GetMission(i);
            if (m == null) continue;

            var row = CreateListRow(_contentMissions, 150f);
            var rt = (RectTransform)row.transform;

            string diff = m.difficulty == 0 ? "Лёгкая" : m.difficulty == 1 ? "Средняя" : "Сложная";
            var name = UIFactory.Text(rt, "Name", $"{RetentionCatalog.MissionNames[m.type]}", 24, UIFactory.TextMain, TextAlignmentOptions.Left);
            Point(name.rectTransform, 0f, 1f, 0f, 1f, 18f, -12f, 560f, 32f);
            var sub = UIFactory.Text(rt, "Cond", $"{diff} · {RetentionCatalog.MissionConditions[m.type]}", 18, UIFactory.TextMuted, TextAlignmentOptions.Left);
            Point(sub.rectTransform, 0f, 1f, 0f, 1f, 18f, -48f, 620f, 26f);

            float frac = m.target > 0 ? Mathf.Clamp01((float)m.progress / m.target) : 1f;
            var barBg = UIFactory.Image("BarBg", rt, UIFactory.RowBg);
            Point(barBg.rectTransform, 0f, 0f, 0f, 0f, 18f, 18f, 360f, 16f);
            var fill = UIFactory.Image("BarFill", barBg.rectTransform, rm.IsMissionReady(m) ? UIFactory.Green : UIFactory.Blue);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(frac, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var prog = UIFactory.Text(rt, "Prog", $"{m.progress}/{m.target}", 20, UIFactory.TextMain, TextAlignmentOptions.Left);
            Point(prog.rectTransform, 0f, 0f, 0f, 0f, 390f, 14f, 160f, 26f);

            var reward = UIFactory.Text(rt, "Reward", $"Награда: {rm.MissionReward(m):N0} ₽", 20, UIFactory.Yellow, TextAlignmentOptions.Right);
            Point(reward.rectTransform, 1f, 1f, 1f, 1f, -18f, -14f, 260f, 28f);

            int index = i;
            if (m.claimed)
            {
                var done = UIFactory.Text(rt, "Done", "✓ Получено", 20, UIFactory.Green, TextAlignmentOptions.Right);
                Point(done.rectTransform, 1f, 0.5f, 1f, 0.5f, -18f, 0f, 180f, 40f);
            }
            else
            {
                var claim = UIFactory.Button(rt, "Btn_Claim", "Забрать",
                    rm.IsMissionReady(m) ? UIFactory.Green : UIFactory.RowBg, UIFactory.TextMain,
                    new Vector2(160f, 48f), () =>
                    {
                        RetentionManager.Instance.ClaimMission(index);
                        RefreshAll();
                    }, 20);
                Point(claim.GetComponent<RectTransform>(), 1f, 0.5f, 1f, 0.5f, -18f, 0f, 160f, 48f);
                claim.interactable = rm.IsMissionReady(m);
            }
        }
    }

    private void RebuildAchievements()
    {
        ClearContent(_contentAchievements);
        var rm = RetentionManager.Instance;

        // Ближайшее незавершённое достижение подсвечиваем (retention, раздел 7).
        int nextIndex = -1;
        for (int i = 0; i < RetentionCatalog.AchievementCount; i++)
        {
            if (rm.IsAchievementClaimed(i) || rm.IsAchievementComplete(i)) continue;
            nextIndex = i;
            break;
        }

        for (int i = 0; i < RetentionCatalog.AchievementCount; i++)
        {
            rm.GetAchievementProgress(i, out int cur, out int max);
            bool complete = cur >= max;
            bool claimed = rm.IsAchievementClaimed(i);

            var row = CreateListRow(_contentAchievements, 116f);
            var rt = (RectTransform)row.transform;

            if (i == nextIndex)
            {
                var hl = row.GetComponent<Image>();
                if (hl != null) hl.color = new Color32(0x2A, 0x33, 0x4A, 0xFF); // подсветка «следующий шаг»
            }

            var name = UIFactory.Text(rt, "Name", RetentionCatalog.AchievementNames[i], 24,
                claimed ? UIFactory.TextMuted : UIFactory.TextMain, TextAlignmentOptions.Left);
            Point(name.rectTransform, 0f, 1f, 0f, 1f, 18f, -12f, 560f, 32f);

            var cond = UIFactory.Text(rt, "Cond", RetentionCatalog.AchievementConditions[i], 18, UIFactory.TextMuted, TextAlignmentOptions.Left);
            Point(cond.rectTransform, 0f, 1f, 0f, 1f, 18f, -46f, 640f, 26f);

            var prog = UIFactory.Text(rt, "Prog", $"{Mathf.Min(cur, max)}/{max}", 20, UIFactory.TextMain, TextAlignmentOptions.Left);
            Point(prog.rectTransform, 0f, 0f, 0f, 0f, 18f, 14f, 200f, 26f);

            var reward = UIFactory.Text(rt, "Reward", $"{RetentionCatalog.AchievementRewards[i]:N0} ₽", 20, UIFactory.Yellow, TextAlignmentOptions.Right);
            Point(reward.rectTransform, 1f, 1f, 1f, 1f, -18f, -14f, 220f, 28f);

            int index = i;
            if (claimed)
            {
                var done = UIFactory.Text(rt, "Done", "✓ Получено", 20, UIFactory.TextMuted, TextAlignmentOptions.Right);
                Point(done.rectTransform, 1f, 0.5f, 1f, 0.5f, -18f, 0f, 180f, 40f);
            }
            else
            {
                var claim = UIFactory.Button(rt, "Btn_Claim", "Забрать",
                    complete ? UIFactory.Green : UIFactory.RowBg, UIFactory.TextMain,
                    new Vector2(160f, 48f), () =>
                    {
                        RetentionManager.Instance.ClaimAchievement(index);
                        RefreshAll();
                    }, 20);
                Point(claim.GetComponent<RectTransform>(), 1f, 0.5f, 1f, 0.5f, -18f, 0f, 160f, 48f);
                claim.interactable = complete;
            }
        }
    }

    // ───────────────────────── Календарь входов ─────────────────────────

    private void BuildDailyPanel()
    {
        _panelDaily = CreateModal("Modal_Daily", "Календарь входов", out RectTransform card);

        _dailyTitle = UIFactory.Text(card, "Streak", "", 22, UIFactory.TextMuted);
        Point(_dailyTitle.rectTransform, 0.5f, 1f, 0.5f, 1f, 0f, -66f, 800f, 30f);

        var rowGo = UIFactory.NewUIObject("Days", card);
        var rrt = (RectTransform)rowGo.transform;
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.5f);
        rrt.pivot = new Vector2(0.5f, 0.5f);
        rrt.sizeDelta = new Vector2(860f, 220f);
        rrt.anchoredPosition = new Vector2(0f, 40f);
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        _dailyCards = new List<RectTransform>();
        for (int day = 1; day <= 7; day++)
        {
            var dayCard = UIFactory.Image($"Day{day}", rowGo.transform, UIFactory.CardBg);
            var drt = dayCard.rectTransform;
            drt.sizeDelta = new Vector2(112f, 200f);
            var le = dayCard.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 112f; le.preferredHeight = 200f;

            var dn = UIFactory.Text(drt, "Day", $"День {day}", 20, UIFactory.TextMain);
            Point(dn.rectTransform, 0.5f, 1f, 0.5f, 1f, 0f, -12f, 100f, 26f);

            var dl = UIFactory.Text(drt, "Label", "", 16, UIFactory.TextMuted);
            var dlrt = dl.rectTransform;
            dlrt.anchorMin = Vector2.zero; dlrt.anchorMax = Vector2.one;
            dlrt.offsetMin = new Vector2(8f, 40f); dlrt.offsetMax = new Vector2(-8f, -42f);

            var st = UIFactory.Text(drt, "Status", "", 22, UIFactory.Green);
            Point(st.rectTransform, 0.5f, 0f, 0.5f, 0f, 0f, 12f, 100f, 28f);

            _dailyCards.Add(drt);
        }

        _btnClaimDaily = UIFactory.Button(card, "Btn_ClaimDaily", "Забрать", UIFactory.Green, Color.white,
            new Vector2(320f, 60f), () =>
            {
                RetentionManager.Instance.ClaimDailyReward();
                RefreshAll();
            }, 24);
        Point(_btnClaimDaily.GetComponent<RectTransform>(), 0.5f, 0f, 0.5f, 0f, 0f, 30f, 320f, 60f);

        _panelDaily.SetActive(false);
    }

    private List<RectTransform> _dailyCards;

    private void RebuildDaily()
    {
        var rm = RetentionManager.Instance;
        int nextDay = rm.NextDailyDay;
        bool canClaim = rm.DailyReady;

        _dailyTitle.text = $"Серия: {rm.DailyStreak} дней подряд";
        if (rm.IsDailyBlockedByGarage) _dailyTitle.text += " · нет свободного места в гараже";

        for (int day = 1; day <= 7; day++)
        {
            var drt = _dailyCards[day - 1];
            var label = drt.Find("Label").GetComponent<TextMeshProUGUI>();
            var status = drt.Find("Status").GetComponent<TextMeshProUGUI>();
            label.text = rm.DailyRewardLabel(day);

            // Полученные дни серии: до текущего дня, если сегодня ещё не забрано;
            // иначе — все дни до номера серии включительно.
            bool claimed;
            if (rm.CanClaimDailyToday) claimed = day < nextDay;
            else claimed = (rm.DailyStreak % 7 == 0 && rm.DailyStreak > 0) || day <= rm.DailyStreak;

            if (day == nextDay && rm.CanClaimDailyToday) status.text = "▶";
            else if (claimed) status.text = "✓";
            else status.text = "";

            var img = drt.GetComponent<Image>();
            if (img != null)
                img.color = day == nextDay ? new Color32(0x2A, 0x33, 0x4A, 0xFF) : UIFactory.CardBg;
        }

        UIFactory.SetButtonLabel(_btnClaimDaily,
            canClaim ? $"Забрать день {nextDay}" : rm.CanClaimDailyToday ? "Нет места" : "Уже получено");
        _btnClaimDaily.interactable = canClaim;
    }

    // ───────────────────────── Всплывающие уведомления (раздел 7) ─────────────────────────

    private void BuildToast()
    {
        var toast = UIFactory.Image("RetentionToast", _root, new Color32(0x1B, 0x1F, 0x2B, 0xF2));
        var rt = toast.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(620f, 56f);
        rt.anchoredPosition = new Vector2(0f, -90f);
        _toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        _toastGroup.alpha = 0f;
        toast.raycastTarget = false;

        _toastText = UIFactory.Text(rt, "Text", "", 22, UIFactory.TextMain);
        _toastText.raycastTarget = false;
        UIFactory.Stretch(_toastText.rectTransform, 10f);
    }

    private void ShowToast(string msg)
    {
        if (_toastText == null) return;
        _toastText.text = msg;
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        _toastGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.2f);
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            _toastGroup.alpha = Mathf.Clamp01(1f - t / 0.6f);
            yield return null;
        }
        _toastGroup.alpha = 0f;
    }

    // ───────────────────────── Служебное ─────────────────────────

    private void TogglePanel(GameObject panel)
    {
        bool opening = !panel.activeSelf;
        _panelMissions.SetActive(false);
        _panelAchievements.SetActive(false);
        _panelDaily.SetActive(false);

        if (!opening) return;

        if (panel == _panelMissions) RebuildMissions();
        else if (panel == _panelAchievements) RebuildAchievements();
        else RebuildDaily();

        panel.SetActive(true);
    }

    private static void ClearContent(Transform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    // Точечный якорь: anchor и pivot задаются явно.
    private static void Point(RectTransform rt, float ax, float ay, float px, float py, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = new Vector2(px, py);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }
}
