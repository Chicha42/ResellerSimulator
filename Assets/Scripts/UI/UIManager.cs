using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Тексты TopBar")]
    public TextMeshProUGUI textMoney;
    public TextMeshProUGUI textGarageSlots;
    public TextMeshProUGUI textReputation;

    [Header("Панели экранов")]
    public GameObject panelMarket;
    public GameObject panelGarage;
    public GameObject panelUpgrades;

    private Coroutine _noticeRoutine;

    private const int MissionRowCount = 3;
    private const int AchievementRowCount = 12;
    private const int DailyCardCount = 7;
    private static readonly int[] VisibleAchievementIndices = { 0, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };

    private Button _btnMissions;
    private Button _btnAchievements;
    private Button _btnDaily;
    private Button _btnCloseMissions;
    private Button _btnCloseAchievements;
    private Button _btnCloseDaily;
    private GameObject _panelMissions;
    private GameObject _panelAchievements;
    private GameObject _panelDaily;
    private MissionRow[] _missionRows;
    private AchievementRow[] _achievementRows;
    private DailyCard[] _dailyCards;
    private TextMeshProUGUI _dailyTitle;
    private Button _btnClaimDaily;
    private CanvasGroup _toastGroup;
    private TextMeshProUGUI _toastText;
    private Coroutine _toastRoutine;
    private bool _sceneObjectsBound;

    private sealed class MissionRow
    {
        public GameObject root;
        public Image background;
        public TextMeshProUGUI name;
        public TextMeshProUGUI condition;
        public TextMeshProUGUI progress;
        public TextMeshProUGUI reward;
        public GameObject done;
        public Button claim;
        public Image barFill;
    }

    private sealed class AchievementRow
    {
        public GameObject root;
        public Image background;
        public TextMeshProUGUI name;
        public TextMeshProUGUI condition;
        public TextMeshProUGUI progress;
        public TextMeshProUGUI reward;
        public GameObject done;
        public Button claim;
    }

    private sealed class DailyCard
    {
        public GameObject root;
        public Image background;
        public TextMeshProUGUI label;
        public TextMeshProUGUI status;
    }

    private static readonly Color ReadyColor = new Color32(0, 185, 84, 255);
    private static readonly Color RowColor = new Color32(42, 51, 74, 255);
    private static readonly Color CardColor = new Color32(31, 36, 50, 255);
    private static readonly Color NextCardColor = new Color32(42, 51, 74, 255);
    private static readonly Color MainTextColor = new Color32(244, 246, 250, 255);
    private static readonly Color MutedTextColor = new Color32(160, 168, 184, 255);
    private static readonly Color RewardTextColor = new Color32(255, 210, 77, 255);
    private static readonly Color MissionBackgroundColor = new Color32(27, 31, 43, 255);
    private static readonly Color ClaimedMissionBackgroundColor = new Color32(19, 22, 31, 255);
    private static readonly Color ClaimedTextColor = new Color32(105, 111, 124, 255);
    private static readonly Color ClaimedRewardColor = new Color32(125, 113, 81, 255);

    private bool BindSceneObjects()
    {
        _btnMissions = ButtonAt("RetentionBar/Btn_Missions");
        _btnAchievements = ButtonAt("RetentionBar/Btn_Achievements");
        _btnDaily = ButtonAt("RetentionBar/Btn_Daily");
        _panelMissions = ObjectAt("Modal_Missions");
        _panelAchievements = ObjectAt("Modal_Achievements");
        _panelDaily = ObjectAt("Modal_Daily");
        _btnCloseMissions = ButtonAt("Modal_Missions/Card/Btn_Close");
        _btnCloseAchievements = ButtonAt("Modal_Achievements/Card/Btn_Close");
        _btnCloseDaily = ButtonAt("Modal_Daily/Card/Btn_Close");
        _dailyTitle = TextAt("Modal_Daily/Card/Streak");
        _btnClaimDaily = ButtonAt("Modal_Daily/Card/Btn_ClaimDaily");
        _toastGroup = ComponentAt<CanvasGroup>("RetentionToast");
        _toastText = TextAt("RetentionToast/Text");

        _missionRows = new MissionRow[MissionRowCount];
        for (int i = 0; i < MissionRowCount; i++)
        {
            string path = $"Modal_Missions/Card/Scroll/Viewport/Content/MissionRow_{i}";
            _missionRows[i] = new MissionRow
            {
                root = ObjectAt(path),
                background = ComponentAt<Image>(path),
                name = TextAt(path + "/Name"),
                condition = TextAt(path + "/Cond"),
                progress = TextAt(path + "/Prog"),
                reward = TextAt(path + "/Reward"),
                done = ObjectAt(path + "/Done"),
                claim = ButtonAt(path + "/Btn_Claim"),
                barFill = ComponentAt<Image>(path + "/BarBg/BarFill")
            };
            if (!HasMissionRow(_missionRows[i])) return false;
        }

        _achievementRows = new AchievementRow[AchievementRowCount];
        for (int i = 0; i < AchievementRowCount; i++)
        {
            int achievementIndex = VisibleAchievementIndices[i];
            string path = $"Modal_Achievements/Card/Scroll/Viewport/Content/AchievementRow_{achievementIndex}";
            _achievementRows[i] = new AchievementRow
            {
                root = ObjectAt(path),
                background = ComponentAt<Image>(path),
                name = TextAt(path + "/Name"),
                condition = TextAt(path + "/Cond"),
                progress = TextAt(path + "/Prog"),
                reward = TextAt(path + "/Reward"),
                done = ObjectAt(path + "/Done"),
                claim = ButtonAt(path + "/Btn_Claim")
            };
            if (!HasAchievementRow(_achievementRows[i])) return false;
        }

        _dailyCards = new DailyCard[DailyCardCount];
        for (int i = 0; i < DailyCardCount; i++)
        {
            string path = $"Modal_Daily/Card/Days/DayCard_{i + 1}";
            _dailyCards[i] = new DailyCard
            {
                root = ObjectAt(path),
                background = ComponentAt<Image>(path),
                label = TextAt(path + "/Label"),
                status = TextAt(path + "/Status")
            };
            if (_dailyCards[i].root == null || _dailyCards[i].background == null || _dailyCards[i].label == null || _dailyCards[i].status == null) return false;
        }

        return _btnMissions != null && _btnAchievements != null && _btnDaily != null
            && _panelMissions != null && _panelAchievements != null && _panelDaily != null
            && _btnCloseMissions != null && _btnCloseAchievements != null && _btnCloseDaily != null
            && _dailyTitle != null && _btnClaimDaily != null && _toastGroup != null && _toastText != null;
    }

    private void BindActions()
    {
        _btnMissions.onClick.AddListener(() => TogglePanel(_panelMissions));
        _btnAchievements.onClick.AddListener(() => TogglePanel(_panelAchievements));
        _btnDaily.onClick.AddListener(() => TogglePanel(_panelDaily));
        _btnCloseMissions.onClick.AddListener(() => _panelMissions.SetActive(false));
        _btnCloseAchievements.onClick.AddListener(() => _panelAchievements.SetActive(false));
        _btnCloseDaily.onClick.AddListener(() => _panelDaily.SetActive(false));
        _btnClaimDaily.onClick.AddListener(ClaimDailyReward);

        for (int i = 0; i < _missionRows.Length; i++)
        {
            int index = i;
            _missionRows[i].claim.onClick.AddListener(() => ClaimMission(index));
        }
        for (int i = 0; i < _achievementRows.Length; i++)
        {
            int achievementIndex = VisibleAchievementIndices[i];
            _achievementRows[i].claim.onClick.AddListener(() => ClaimAchievement(achievementIndex));
        }
    }

    private void RefreshAll()
    {
        var manager = RetentionManager.Instance;
        if (manager == null || _missionRows == null) return;

        SetButtonLabel(_btnMissions, manager.ReadyMissionsCount > 0 ? $"Миссии · {manager.ReadyMissionsCount}" : "Миссии");
        SetButtonLabel(_btnAchievements, manager.ReadyAchievementsCount > 0 ? $"Достижения · {manager.ReadyAchievementsCount}" : "Достижения");
        SetButtonLabel(_btnDaily, manager.DailyReady ? "Награда ●" : "Награда");

        if (_panelMissions.activeSelf) RefreshMissions();
        if (_panelAchievements.activeSelf) RefreshAchievements();
        if (_panelDaily.activeSelf) RefreshDaily();
    }

    private void RefreshMissions()
    {
        var manager = RetentionManager.Instance;
        manager.RefreshDailyIfNeeded();
        for (int i = 0; i < _missionRows.Length; i++)
        {
            var row = _missionRows[i];
            var mission = manager.GetMission(i);
            row.root.SetActive(mission != null);
            if (mission == null) continue;

            string difficulty = mission.difficulty == 0 ? "Лёгкая" : mission.difficulty == 1 ? "Средняя" : "Сложная";
            bool ready = manager.IsMissionReady(mission);
            bool claimed = mission.claimed;
            row.background.color = claimed ? ClaimedMissionBackgroundColor : MissionBackgroundColor;
            row.name.text = RetentionCatalog.MissionNames[mission.type];
            row.name.color = claimed ? ClaimedTextColor : MainTextColor;
            row.condition.text = $"{difficulty} · {RetentionCatalog.MissionConditions[mission.type]}";
            row.condition.color = claimed ? ClaimedTextColor : MutedTextColor;
            row.progress.text = $"{mission.progress}/{mission.target}";
            row.progress.color = claimed ? ClaimedTextColor : MainTextColor;
            row.reward.text = $"Награда: {manager.MissionReward(mission):N0} ₽";
            row.reward.color = claimed ? ClaimedRewardColor : RewardTextColor;
            float progressRatio = mission.target > 0 ? Mathf.Clamp01((float)mission.progress / mission.target) : 1f;
            var fillRect = row.barFill.rectTransform;
            var barRect = fillRect.parent as RectTransform;
            if (barRect != null)
                fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barRect.rect.width * progressRatio);
            row.barFill.color = claimed ? ClaimedTextColor : ready ? ReadyColor : new Color32(61, 133, 228, 255);
            row.done.SetActive(claimed);
            var doneText = row.done.GetComponent<TextMeshProUGUI>();
            if (doneText != null) doneText.color = ClaimedTextColor;
            row.claim.gameObject.SetActive(!claimed);
            row.claim.interactable = ready;
            SetButtonColor(row.claim, ready ? ReadyColor : RowColor);
        }
    }

    private void RefreshAchievements()
    {
        var manager = RetentionManager.Instance;
        int nextIndex = -1;
        for (int i = 0; i < _achievementRows.Length; i++)
        {
            int achievementIndex = VisibleAchievementIndices[i];
            if (!manager.IsAchievementClaimed(achievementIndex) && !manager.IsAchievementComplete(achievementIndex))
            {
                nextIndex = i;
                break;
            }
        }

        for (int i = 0; i < _achievementRows.Length; i++)
        {
            var row = _achievementRows[i];
            int achievementIndex = VisibleAchievementIndices[i];
            manager.GetAchievementProgress(achievementIndex, out int current, out int maximum);
            bool complete = current >= maximum;
            bool claimed = manager.IsAchievementClaimed(achievementIndex);
            row.name.text = RetentionCatalog.AchievementNames[achievementIndex];
            row.name.color = claimed ? MutedTextColor : MainTextColor;
            row.condition.text = RetentionCatalog.AchievementConditions[achievementIndex];
            row.progress.text = $"{Mathf.Min(current, maximum)}/{maximum}";
            row.reward.text = $"{RetentionCatalog.AchievementRewards[achievementIndex]:N0} ₽";
            row.background.color = i == nextIndex ? NextCardColor : CardColor;
            row.done.SetActive(claimed);
            row.claim.gameObject.SetActive(!claimed);
            row.claim.interactable = complete;
            SetButtonColor(row.claim, complete ? ReadyColor : RowColor);
        }
    }

    private void RefreshDaily()
    {
        var manager = RetentionManager.Instance;
        int nextDay = manager.NextDailyDay;
        _dailyTitle.text = $"Серия: {manager.DailyStreak} дней подряд";
        if (manager.IsDailyBlockedByGarage) _dailyTitle.text += " · нет свободного места в гараже";

        for (int day = 1; day <= _dailyCards.Length; day++)
        {
            var card = _dailyCards[day - 1];
            card.label.text = manager.DailyRewardLabel(day);
            bool claimed = manager.CanClaimDailyToday
                ? day < nextDay
                : (manager.DailyStreak % 7 == 0 && manager.DailyStreak > 0) || day <= manager.DailyStreak;
            card.status.text = day == nextDay && manager.CanClaimDailyToday ? "▶" : claimed ? "✓" : "";
            card.background.color = day == nextDay ? NextCardColor : CardColor;
        }

        bool canClaim = manager.DailyReady;
        SetButtonLabel(_btnClaimDaily, canClaim ? $"Забрать день {nextDay}" : manager.CanClaimDailyToday ? "Нет места" : "Уже получено");
        _btnClaimDaily.interactable = canClaim;
        SetButtonColor(_btnClaimDaily, canClaim ? ReadyColor : RowColor);
    }

    private void ClaimMission(int index)
    {
        RetentionManager.Instance?.ClaimMission(index);
        RefreshAll();
    }

    private void ClaimAchievement(int index)
    {
        RetentionManager.Instance?.ClaimAchievement(index);
        RefreshAll();
    }

    private void ClaimDailyReward()
    {
        RetentionManager.Instance?.ClaimDailyReward();
        RefreshAll();
    }

    private void ShowToast(string message)
    {
        _toastText.text = message;
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        _toastGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.2f);
        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.unscaledDeltaTime;
            _toastGroup.alpha = Mathf.Clamp01(1f - elapsed / 0.6f);
            yield return null;
        }
        _toastGroup.alpha = 0f;
        _toastRoutine = null;
    }

    private void TogglePanel(GameObject panel)
    {
        bool opening = !panel.activeSelf;
        _panelMissions.SetActive(false);
        _panelAchievements.SetActive(false);
        _panelDaily.SetActive(false);
        if (!opening) return;

        if (panel == _panelMissions) RefreshMissions();
        else if (panel == _panelAchievements) RefreshAchievements();
        else RefreshDaily();
        panel.SetActive(true);
    }

    private static bool HasMissionRow(MissionRow row) => row.root != null && row.background != null && row.name != null && row.condition != null
        && row.progress != null && row.reward != null && row.done != null && row.claim != null && row.barFill != null;

    private static bool HasAchievementRow(AchievementRow row) => row.root != null && row.background != null && row.name != null
        && row.condition != null && row.progress != null && row.reward != null && row.done != null && row.claim != null;

    private Button ButtonAt(string path) => ComponentAt<Button>(path);
    private TextMeshProUGUI TextAt(string path) => ComponentAt<TextMeshProUGUI>(path);
    private GameObject ObjectAt(string path) => _retentionRoot.Find(path)?.gameObject;
    private T ComponentAt<T>(string path) where T : Component => _retentionRoot.Find(path)?.GetComponent<T>();

    private static void SetButtonLabel(Button button, string text)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
    }

    private static void SetButtonColor(Button button, Color color)
    {
        var image = button.targetGraphic as Image;
        if (image != null) image.color = color;
    }

    private Transform _retentionRoot;

    private void InitializeRetentionUI()
    {
        var canvas = GameObject.Find("Canvas");
        _retentionRoot = canvas != null ? canvas.transform.Find("RetentionUI") : null;
        _sceneObjectsBound = _retentionRoot != null && BindSceneObjects();
        if (!_sceneObjectsBound)
        {
            Debug.LogError("[Retention] Не найдены элементы интерфейса Retention на сцене.", this);
            return;
        }

        BindActions();
        var manager = RetentionManager.EnsureExists();
        manager.OnChanged += RefreshAll;
        manager.OnToast += ShowToast;
        RefreshAll();
    }

    private void DisposeRetentionUI()
    {
        var manager = RetentionManager.Instance;
        if (manager != null)
        {
            manager.OnChanged -= RefreshAll;
            manager.OnToast -= ShowToast;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateTopBar();
        ShowMarket(); // На старте открываем рынок
        InitializeRetentionUI();
    }

    private void OnDestroy()
    {
        DisposeRetentionUI();
    }

    // Обновление верхнего бара
    public void UpdateTopBar()
    {
        if (PlayerData.Instance == null) return;

        // Деньги с форматированием пробелов (25 000 Р)
        textMoney.text = $"{PlayerData.Instance.money:N0} Р";

        // Гараж: занято / всего мест
        int currentCars = PlayerData.Instance.garage.Count;
        int maxSlots = PlayerData.Instance.MaxGarageSlots;
        textGarageSlots.text = $"Гараж: {currentCars}/{maxSlots}";

        // Репутация растёт от продаж; счётчик показывает прогресс между уровнями.
        textReputation.text = $"Репутация: {PlayerData.Instance.ReputationTitle}\nУровень {PlayerData.Instance.ReputationLevel} · продаж: {PlayerData.Instance.totalSalesCount}";
    }

    public void ShowTemporaryNotice(TextMeshProUGUI notice, string message)
    {
        if (notice == null) return;
        notice.text = message;
        GameObject noticeRoot = notice.transform.parent != null ? notice.transform.parent.gameObject : notice.gameObject;
        noticeRoot.SetActive(true);
        if (_noticeRoutine != null) StopCoroutine(_noticeRoutine);
        _noticeRoutine = StartCoroutine(HideTemporaryNotice(noticeRoot));
    }

    private IEnumerator HideTemporaryNotice(GameObject noticeRoot)
    {
        yield return new WaitForSeconds(2.5f);
        if (noticeRoot != null) noticeRoot.SetActive(false);
        _noticeRoutine = null;
    }

    // Методы переключения экранов для кнопок
    public void ShowMarket()
    {
        panelMarket.SetActive(true);
        panelGarage.SetActive(false);
        panelUpgrades.SetActive(false);
    }

    public void ShowGarage()
    {
        panelMarket.SetActive(false);
        panelGarage.SetActive(true);
        panelUpgrades.SetActive(false);
    }

    public void ShowUpgrades()
    {
        panelMarket.SetActive(false);
        panelGarage.SetActive(false);
        panelUpgrades.SetActive(true);
        UpgradesManager.Instance?.Refresh();
    }
}
