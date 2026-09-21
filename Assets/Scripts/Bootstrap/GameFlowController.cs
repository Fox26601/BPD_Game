using System;
using System.Collections.Generic;
using BPD.Core;
using BPD.Endings;
using BPD.Level;
using BPD.Localization;
using BPD.Presentation;
using BPD.Progression;
using BPD.Run;
using BPD.Skills;
using BPD.Split;
using BPD.Stats;
using BPD.Therapy;
using UnityEngine;
using UnityEngine.UI;

namespace BPD.Bootstrap
{
    /// <summary>
    /// Runtime-built vertical slice of the full core loop (stub content).
    /// </summary>
    public sealed class GameFlowController : MonoBehaviour
    {
        enum FlowScreen
        {
            CharacterSelect,
            Level,
            Split,
            Meta,
            Therapy,
            Ending
        }

        GameSession _session;
        GameConfig _config;
        ModifierRegistry _modifiers;
        ProgressionService _progression;
        LevelController _level;
        ICardEffect _cardEffect;
        ISplitMinigame _activeSplit;
        TherapyMergeEngine _therapy;
        TherapyCard _therapySlotA;
        TherapyCard _therapySlotB;
        TherapySelectionPhase _therapyPhase = TherapySelectionPhase.EmotionPairing;
        CoreStat _activeSplitStat;
        DbtSkillId _lastUnlockedSkill = DbtSkillId.None;
        int _metaSymptomIndex;

        Canvas _canvas;
        AdaptiveShell _shell;
        RectTransform _root;
        RectTransform _actionsHost;
        GridLayoutGroup _actionsGrid;
        LayoutElement _contentBandLe;
        LayoutElement _actionsBandLe;
        FlowScreen _screen;

        // Shared HUD
        readonly Dictionary<CoreStat, Slider> _statSliders = new Dictionary<CoreStat, Slider>();
        readonly Dictionary<CoreStat, Text> _statLabels = new Dictionary<CoreStat, Text>();
        Text _headerText;
        Text _bodyText;
        Text _hintText;
        Text _statusText;
        RectTransform _contentScrollRoot;
        RectTransform _contentDynamicHost;
        Text _therapyEmotionSlotText;
        Text _therapyNeedSlotText;
        readonly List<BoardCardUi> _therapyBoardCards = new List<BoardCardUi>();
        readonly Dictionary<BpdSymptom, Button> _symptomTreeButtons = new Dictionary<BpdSymptom, Button>();
        static readonly Color CompactNormal = new Color(0.18f, 0.19f, 0.22f, 1f);
        static readonly Color CompactSelected = new Color(0.34f, 0.42f, 0.48f, 1f);
        static readonly Color CompactDimmed = new Color(0.18f, 0.19f, 0.22f, 0.45f);

        sealed class BoardCardUi
        {
            public TherapyCard Card;
            public Button Button;
            public Image Image;
            public Text Label;
            public bool Interactive;
            public Color BaseColor;
        }

        // Dynamic action buttons + arrow bindings
        sealed class UiAction
        {
            public string BaseLabel;
            public Action OnClick;
            public ArrowSlot ForcedSlot = ArrowSlot.None;
            public ArrowSlot Slot = ArrowSlot.None;
            public bool IsBack;
            public bool IsMerge;
            public TherapyCard BoundTherapyCard;
            public Button Button;
            public Image Image;
            public Text Label;
            public Color BaseColor;
        }

        readonly List<UiAction> _actions = new List<UiAction>();
        readonly List<Button> _actionButtons = new List<Button>(); // layout helper (button refs)
        bool _focusMode;
        int _focusIndex;
        static readonly Color ButtonNormal = new Color(0.22f, 0.24f, 0.28f, 1f);
        static readonly Color ButtonFocused = new Color(0.34f, 0.42f, 0.48f, 1f);

        // Card drag
        RectTransform _cardRect;
        Vector2 _cardStart;
        bool _dragging;
        Vector2 _dragOrigin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrap()
        {
            var existing = FindAnyObjectByType<GameFlowController>();
            if (existing != null)
            {
                // Domain reload (e.g. deleted CrispUiScaler) keeps DDOL HUD with a missing script slot.
                if (existing.HasBrokenHud())
                {
                    existing.RebuildHudAfterDomainReload();
                }

                return;
            }

            var go = new GameObject("GameFlow");
            go.AddComponent<GameFlowController>();
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            EnsureSession();
            _config = _session.Config;
            _modifiers = new ModifierRegistry();
            _progression = new ProgressionService(_config);
            _cardEffect = new DefaultCardEffect(_modifiers);
            _level = new LevelController(_config, _cardEffect);
            _therapy = TherapyMergeEngine.LoadOrCreateStub();
            BuildUi();
            ShowCharacterSelect();
        }

        bool HasBrokenHud()
        {
            if (_canvas == null)
            {
                return true;
            }

            var behaviours = _canvas.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null)
                {
                    return true;
                }
            }

            return false;
        }

        void RebuildHudAfterDomainReload()
        {
            DestroyHud();
            EnsureSession();
            if (_config == null && _session != null)
            {
                _config = _session.Config;
            }

            if (_modifiers == null)
            {
                _modifiers = new ModifierRegistry();
            }

            if (_progression == null && _config != null)
            {
                _progression = new ProgressionService(_config);
            }

            if (_cardEffect == null && _modifiers != null)
            {
                _cardEffect = new DefaultCardEffect(_modifiers);
            }

            if (_level == null && _config != null && _cardEffect != null)
            {
                _level = new LevelController(_config, _cardEffect);
            }

            if (_therapy == null)
            {
                _therapy = TherapyMergeEngine.LoadOrCreateStub();
            }

            BuildUi();
            ShowCharacterSelect();
        }

        void DestroyHud()
        {
            if (_shell != null)
            {
                _shell.OrientationChanged -= OnOrientationChanged;
                _shell = null;
            }

            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
                _canvas = null;
            }

            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas != null && canvas.gameObject.name == "HUD")
                {
                    Destroy(canvas.gameObject);
                }
            }

            _root = null;
            _actionsHost = null;
            _actionsGrid = null;
            _actionButtons.Clear();
            _actions.Clear();
            _statSliders.Clear();
            _statLabels.Clear();
            _headerText = null;
            _bodyText = null;
            _hintText = null;
            _statusText = null;
            _cardRect = null;
        }

        void EnsureSession()
        {
            _session = FindAnyObjectByType<GameSession>();
            if (_session == null)
            {
                var go = new GameObject("GameSession");
                _session = go.AddComponent<GameSession>();
            }
        }

        void Update()
        {
            if (_screen == FlowScreen.Level && _level.Outcome == LevelOutcome.InProgress)
            {
                HandleLevelPointerInput();
            }

            HandleArrowActions();
            _activeSplit?.Tick(Time.deltaTime);
        }

        void HandleLevelPointerInput()
        {
            if (!GameInput.TryGetPointerPosition(out var pointerPos))
            {
                return;
            }

            if (GameInput.WasPointerPressed() &&
                RectTransformUtility.RectangleContainsScreenPoint(_cardRect, pointerPos, null))
            {
                _dragging = true;
                _dragOrigin = pointerPos;
            }

            if (_dragging && GameInput.IsPointerPressed())
            {
                float dx = pointerPos.x - _dragOrigin.x;
                _cardRect.anchoredPosition = _cardStart + new Vector2(dx, 0f);
            }

            if (_dragging && GameInput.WasPointerReleased())
            {
                float dx = pointerPos.x - _dragOrigin.x;
                _dragging = false;
                _cardRect.anchoredPosition = _cardStart;
                float threshold = Mathf.Max(120f, Screen.width * 0.15f);
                if (dx <= -threshold)
                {
                    ChooseLeft();
                }
                else if (dx >= threshold)
                {
                    ChooseRight();
                }
            }
        }

        void HandleArrowActions()
        {
            if (_actions.Count == 0)
            {
                return;
            }

            if (_focusMode)
            {
                if (GameInput.WasPressedUp())
                {
                    MoveFocus(-1);
                    return;
                }

                if (GameInput.WasPressedDown())
                {
                    MoveFocus(1);
                    return;
                }

                if (GameInput.WasPressedLeft())
                {
                    var back = FindAction(ArrowSlot.Left) ?? FindBackAction();
                    back?.OnClick?.Invoke();
                    return;
                }

                if (GameInput.WasPressedRight() || GameInput.WasPressedConfirm())
                {
                    ActivateFocused();
                }

                return;
            }

            if (GameInput.WasPressedUp())
            {
                FindAction(ArrowSlot.Up)?.OnClick?.Invoke();
            }
            else if (GameInput.WasPressedLeft())
            {
                FindAction(ArrowSlot.Left)?.OnClick?.Invoke();
            }
            else if (GameInput.WasPressedRight())
            {
                FindAction(ArrowSlot.Right)?.OnClick?.Invoke();
            }
            else if (GameInput.WasPressedDown())
            {
                FindAction(ArrowSlot.Down)?.OnClick?.Invoke();
            }
        }

        UiAction FindAction(ArrowSlot slot)
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                if (_actions[i].Slot == slot)
                {
                    return _actions[i];
                }
            }

            return null;
        }

        UiAction FindBackAction()
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                if (_actions[i].IsBack)
                {
                    return _actions[i];
                }
            }

            return null;
        }

        void MoveFocus(int delta)
        {
            if (_actions.Count == 0)
            {
                return;
            }

            _focusIndex = (_focusIndex + delta + _actions.Count) % _actions.Count;
            ApplyFocusVisuals();
        }

        void ActivateFocused()
        {
            if (_focusIndex < 0 || _focusIndex >= _actions.Count)
            {
                return;
            }

            _actions[_focusIndex].OnClick?.Invoke();
        }

        void ApplyFocusVisuals()
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                var action = _actions[i];
                if (action.Image == null)
                {
                    continue;
                }

                action.Image.color = _focusMode && i == _focusIndex ? ButtonFocused : action.BaseColor;
            }
        }

        void BuildUi()
        {
            DestroyHud();
            UiFactory.EnsureEventSystem();
            _canvas = UiFactory.CreateAdaptiveCanvas("HUD", out var safeArea, out _shell);
            DontDestroyOnLoad(_canvas.gameObject);
            _shell.OrientationChanged += OnOrientationChanged;

            var shellLayout = UiFactory.CreateVertical(
                safeArea,
                "Shell",
                8f,
                new RectOffset(16, 16, 12, 12),
                forceExpandHeight: true);
            _root = shellLayout.GetComponent<RectTransform>();

            // Header
            var headerBand = UiFactory.CreateBand(_root, "HeaderBand", 0.06f, 40f);
            _headerText = UiFactory.CreateText(headerBand, "Header", Loc.Get("ui.brand"), TextRole.Title, TextAnchor.MiddleCenter);
            UiFactory.Stretch(_headerText.rectTransform);
            var headerLe = _headerText.GetComponent<LayoutElement>();
            headerLe.flexibleHeight = 1f;
            headerLe.minHeight = 36f;

            // Stats 2x2 — compact but readable
            var statsBand = UiFactory.CreateBand(_root, "StatsBand", 0.15f, 96f);
            BuildStatGrid(statsBand);

            // Content: short body + scrollable dynamic host + card + hint + status
            var contentBand = UiFactory.CreateBand(_root, "ContentBand", 0.34f, 120f);
            _contentBandLe = contentBand.GetComponent<LayoutElement>();
            var contentLayout = UiFactory.CreateVertical(
                contentBand,
                "Content",
                4f,
                new RectOffset(0, 0, 0, 0));

            _bodyText = UiFactory.CreateText(contentLayout.transform, "Body", string.Empty, TextRole.Body, TextAnchor.UpperCenter);
            var bodyLe = _bodyText.GetComponent<LayoutElement>();
            bodyLe.flexibleHeight = 0f;
            bodyLe.minHeight = 28f;
            bodyLe.preferredHeight = 36f;

            var scroll = UiFactory.CreateScrollHost(contentLayout.transform, "DynamicScroll", out _contentDynamicHost);
            _contentScrollRoot = scroll.GetComponent<RectTransform>();
            var scrollLe = scroll.GetComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 80f;

            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            cardGo.transform.SetParent(contentLayout.transform, false);
            _cardRect = cardGo.GetComponent<RectTransform>();
            var cardLe = cardGo.GetComponent<LayoutElement>();
            cardLe.flexibleHeight = 0.45f;
            cardLe.minHeight = 80f;
            cardLe.flexibleWidth = 1f;
            cardGo.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.24f, 1f);
            _cardStart = Vector2.zero;
            var cardLabel = UiFactory.CreateText(cardGo.transform, "CardLabel", string.Empty, TextRole.Body, TextAnchor.MiddleCenter);
            UiFactory.Stretch(cardLabel.rectTransform, 16, 16, 12, 12);
            var cardLabelLe = cardLabel.GetComponent<LayoutElement>();
            cardLabelLe.ignoreLayout = true;

            _hintText = UiFactory.CreateText(contentLayout.transform, "Hint", string.Empty, TextRole.Caption, TextAnchor.MiddleCenter);
            _hintText.color = new Color(0.82f, 0.84f, 0.88f, 1f);
            var hintLe = _hintText.GetComponent<LayoutElement>();
            hintLe.minHeight = 22f;
            hintLe.preferredHeight = 24f;
            hintLe.flexibleHeight = 0f;

            _statusText = UiFactory.CreateText(contentLayout.transform, "Status", string.Empty, TextRole.Caption, TextAnchor.MiddleCenter);
            var statusLe = _statusText.GetComponent<LayoutElement>();
            statusLe.minHeight = 22f;
            statusLe.preferredHeight = 24f;
            statusLe.flexibleHeight = 0f;

            // Actions — enough flex for 3 character buttons without clipping
            var actionsBand = UiFactory.CreateBand(_root, "ActionsBand", 0.45f, 200f);
            _actionsBandLe = actionsBand.GetComponent<LayoutElement>();
            var actionsGo = new GameObject("Actions", typeof(RectTransform), typeof(GridLayoutGroup));
            actionsGo.transform.SetParent(actionsBand, false);
            _actionsHost = actionsGo.GetComponent<RectTransform>();
            UiFactory.Stretch(_actionsHost);
            _actionsGrid = actionsGo.GetComponent<GridLayoutGroup>();
            _actionsGrid.padding = new RectOffset(2, 2, 2, 2);
            _actionsGrid.spacing = new Vector2(8f, 8f);
            _actionsGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _actionsGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _actionsGrid.childAlignment = TextAnchor.MiddleCenter;
            _actionsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            RefreshActionLayout();
            SetContentDynamicVisible(false);
        }

        void BuildStatGrid(RectTransform parent)
        {
            var gridGo = new GameObject("Stats", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(parent, false);
            UiFactory.Stretch(gridGo.GetComponent<RectTransform>());
            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(2, 2, 2, 2);
            grid.cellSize = new Vector2(400f, StatGridFitter.MinCellHeight);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            // Fit cells to available width via layout rebuild helper
            var fitter = gridGo.AddComponent<StatGridFitter>();
            fitter.Bind(grid);

            foreach (CoreStat stat in Enum.GetValues(typeof(CoreStat)))
            {
                var cell = new GameObject(stat.ToString(), typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image));
                cell.transform.SetParent(gridGo.transform, false);
                cell.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.16f, 0.9f);
                var v = cell.GetComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(8, 8, 6, 6);
                v.spacing = 4f;
                v.childAlignment = TextAnchor.MiddleCenter;
                v.childControlWidth = true;
                v.childControlHeight = true;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;

                var label = UiFactory.CreateText(cell.transform, "Label", Loc.Get(StatKey(stat)), TextRole.Caption, TextAnchor.MiddleCenter);
                var labelLe = label.GetComponent<LayoutElement>();
                labelLe.minHeight = 18f;
                labelLe.preferredHeight = 22f;

                var slider = UiFactory.CreateSlider(cell.transform, "Meter");
                _statSliders[stat] = slider;
                _statLabels[stat] = label;
            }
        }

        void OnOrientationChanged(bool portrait)
        {
            RefreshActionLayout();
            Canvas.ForceUpdateCanvases();
        }

        void RefreshActionLayout()
        {
            if (_actionsGrid == null)
            {
                return;
            }

            bool portrait = _shell == null || _shell.IsPortrait;
            int count = Mathf.Max(1, _actionButtons.Count);

            float hostWidth = _actionsHost != null ? _actionsHost.rect.width : Screen.width;
            if (hostWidth < 10f)
            {
                hostWidth = Screen.width * 0.9f;
            }

            // Readable-first: never 3 columns for long labels; prefer 1 col when few actions or narrow.
            int cols;
            if (portrait || count <= 3 || hostWidth < 1100f)
            {
                cols = 1;
            }
            else
            {
                cols = 2;
            }

            _actionsGrid.constraintCount = cols;

            float spacingX = _actionsGrid.spacing.x;
            float cellW = (hostWidth - _actionsGrid.padding.horizontal - spacingX * (cols - 1)) / cols;
            cellW = Mathf.Clamp(cellW, 200f, 900f);

            bool multiline = false;
            foreach (var action in _actions)
            {
                if (action.BaseLabel != null && action.BaseLabel.IndexOf('\n') >= 0)
                {
                    multiline = true;
                    break;
                }
            }

            float cellH = cols == 1
                ? (multiline ? 88f : 72f)
                : (multiline ? 88f : 72f);
            _actionsGrid.cellSize = new Vector2(cellW, cellH);
        }

        static string StatKey(CoreStat stat) => stat switch
        {
            CoreStat.Wholeness => "ui.stat.wholeness",
            CoreStat.Stability => "ui.stat.stability",
            CoreStat.Safety => "ui.stat.safety",
            CoreStat.Closeness => "ui.stat.closeness",
            _ => "ui.stat.wholeness"
        };

        void ClearActions()
        {
            foreach (var action in _actions)
            {
                if (action.Button != null)
                {
                    Destroy(action.Button.gameObject);
                }
            }

            _actions.Clear();
            _actionButtons.Clear();
            _focusMode = false;
            _focusIndex = 0;
            RefreshActionLayout();
        }

        void ClearContentDynamic()
        {
            if (_contentDynamicHost == null)
            {
                return;
            }

            for (int i = _contentDynamicHost.childCount - 1; i >= 0; i--)
            {
                Destroy(_contentDynamicHost.GetChild(i).gameObject);
            }

            _therapyBoardCards.Clear();
            _symptomTreeButtons.Clear();
            _therapyEmotionSlotText = null;
            _therapyNeedSlotText = null;
            SetContentDynamicVisible(false);
        }

        void SetContentDynamicVisible(bool visible)
        {
            if (_contentScrollRoot == null)
            {
                return;
            }

            _contentScrollRoot.gameObject.SetActive(visible);
            var le = _contentScrollRoot.GetComponent<LayoutElement>();
            le.flexibleHeight = visible ? 1f : 0f;
            le.minHeight = visible ? 120f : 0f;
            le.preferredHeight = visible ? -1f : 0f;

            if (_cardRect != null)
            {
                var cardLe = _cardRect.GetComponent<LayoutElement>();
                // Card shares content with scroll only on Level; Meta/Therapy hide the card.
                bool cardActive = _cardRect.gameObject.activeSelf;
                cardLe.flexibleHeight = cardActive && !visible ? 0.55f : (cardActive ? 0.25f : 0f);
                cardLe.minHeight = cardActive ? 80f : 0f;
                cardLe.preferredHeight = cardActive ? -1f : 0f;
            }
        }

        void ApplyShellLayout(FlowScreen screen)
        {
            if (_contentBandLe == null || _actionsBandLe == null)
            {
                return;
            }

            switch (screen)
            {
                case FlowScreen.Meta:
                case FlowScreen.Therapy:
                    // Prefer content (tree/board); actions stay compact (2–3 buttons).
                    _contentBandLe.flexibleHeight = 0.55f;
                    _contentBandLe.minHeight = 260f;
                    _contentBandLe.preferredHeight = 260f;
                    _actionsBandLe.flexibleHeight = 0.28f;
                    _actionsBandLe.minHeight = 168f;
                    _actionsBandLe.preferredHeight = 168f;
                    break;
                case FlowScreen.CharacterSelect:
                    _contentBandLe.flexibleHeight = 0.28f;
                    _contentBandLe.minHeight = 100f;
                    _contentBandLe.preferredHeight = 100f;
                    _actionsBandLe.flexibleHeight = 0.50f;
                    _actionsBandLe.minHeight = 220f;
                    _actionsBandLe.preferredHeight = 220f;
                    break;
                default:
                    _contentBandLe.flexibleHeight = 0.34f;
                    _contentBandLe.minHeight = 120f;
                    _contentBandLe.preferredHeight = 120f;
                    _actionsBandLe.flexibleHeight = 0.45f;
                    _actionsBandLe.minHeight = 200f;
                    _actionsBandLe.preferredHeight = 200f;
                    break;
            }
        }

        static string BuildSeverityBar(int severity, int max)
        {
            const int segments = 5;
            if (max <= 0)
            {
                return new string('░', segments);
            }

            int filled = Math.Clamp((int)Math.Round(severity * (double)segments / max), 0, segments);
            int empty = segments - filled;
            return new string('█', filled) + new string('░', empty);
        }

        UiAction AddAction(string label, Action onClick, ArrowSlot forcedSlot = ArrowSlot.None, bool isBack = false)
        {
            var button = UiFactory.CreateFlexibleButton(_actionsHost, "Action", label);
            button.onClick.AddListener(() => onClick());
            var image = button.GetComponent<Image>();
            image.color = ButtonNormal;
            var entry = new UiAction
            {
                BaseLabel = label,
                OnClick = onClick,
                ForcedSlot = forcedSlot,
                IsBack = isBack,
                Button = button,
                Image = image,
                Label = button.GetComponentInChildren<Text>(),
                BaseColor = ButtonNormal
            };
            _actions.Add(entry);
            _actionButtons.Add(button);
            RefreshActionLayout();
            return entry;
        }

        void FinalizeActions(bool forceFocusMode = false)
        {
            AssignArrowHints(forceFocusMode);
            RefreshActionLayout();
            ApplyFocusVisuals();
        }

        void AssignArrowHints(bool forceFocusMode)
        {
            int count = _actions.Count;
            _focusMode = forceFocusMode || count > 4;

            if (_focusMode)
            {
                for (int i = 0; i < count; i++)
                {
                    var action = _actions[i];
                    action.Slot = action.IsBack ? ArrowSlot.Left : ArrowSlot.None;
                    ApplyLabel(action);
                }

                _focusIndex = 0;
                for (int i = 0; i < count; i++)
                {
                    if (!_actions[i].IsBack)
                    {
                        _focusIndex = i;
                        break;
                    }
                }

                return;
            }

            // Clear then assign forced or auto slots
            for (int i = 0; i < count; i++)
            {
                _actions[i].Slot = ArrowSlot.None;
            }

            bool anyForced = false;
            for (int i = 0; i < count; i++)
            {
                if (_actions[i].ForcedSlot != ArrowSlot.None)
                {
                    _actions[i].Slot = _actions[i].ForcedSlot;
                    anyForced = true;
                }
            }

            if (!anyForced)
            {
                ArrowSlot[] order = count switch
                {
                    1 => new[] { ArrowSlot.Up },
                    2 => new[] { ArrowSlot.Left, ArrowSlot.Right },
                    3 => new[] { ArrowSlot.Up, ArrowSlot.Left, ArrowSlot.Right },
                    _ => new[] { ArrowSlot.Up, ArrowSlot.Left, ArrowSlot.Right, ArrowSlot.Down }
                };

                for (int i = 0; i < count && i < order.Length; i++)
                {
                    _actions[i].Slot = order[i];
                }
            }

            for (int i = 0; i < count; i++)
            {
                ApplyLabel(_actions[i]);
            }
        }

        static void ApplyLabel(UiAction action)
        {
            if (action.Label == null)
            {
                return;
            }

            if (action.Slot == ArrowSlot.None)
            {
                // Focus-mode navigable items: show focus mark only via color; Back still gets ←
                action.Label.text = action.IsBack
                    ? Loc.Format("ui.key.hint", Loc.Get("ui.key.left"), action.BaseLabel)
                    : action.BaseLabel;
                return;
            }

            string key = action.Slot switch
            {
                ArrowSlot.Up => Loc.Get("ui.key.up"),
                ArrowSlot.Left => Loc.Get("ui.key.left"),
                ArrowSlot.Right => Loc.Get("ui.key.right"),
                ArrowSlot.Down => Loc.Get("ui.key.down"),
                _ => string.Empty
            };
            action.Label.text = Loc.Format("ui.key.hint", key, action.BaseLabel);
        }

        void RefreshStats()
        {
            if (_session.Run == null)
            {
                return;
            }

            foreach (var pair in _statSliders)
            {
                pair.Value.value = _session.Run.GetStat(pair.Key);
            }
        }

        void ShowCharacterSelect()
        {
            _screen = FlowScreen.CharacterSelect;
            ApplyShellLayout(_screen);
            _cardRect.gameObject.SetActive(false);
            ClearContentDynamic();
            _headerText.text = Loc.Get("ui.brand");
            _bodyText.text = Loc.Get("ui.character.select");
            _hintText.text = Loc.Get("ui.character.controls_hint");
            _statusText.text = string.Empty;
            ClearActions();
            AddAction(UiFactory.ToMultilineActionLabel(Loc.Get("ui.character.a")), () => BeginRun(CharacterId.CharacterA));
            AddAction(UiFactory.ToMultilineActionLabel(Loc.Get("ui.character.b")), () => BeginRun(CharacterId.CharacterB));
            AddAction(UiFactory.ToMultilineActionLabel(Loc.Get("ui.character.c")), () => BeginRun(CharacterId.CharacterC));
            FinalizeActions();
        }

        void BeginRun(CharacterId character)
        {
            _session.StartRun(character);
            _lastUnlockedSkill = DbtSkillId.None;
            StartLevel(forceLoseDeck: false);
        }

        void StartLevel(bool forceLoseDeck)
        {
            _screen = FlowScreen.Level;
            ApplyShellLayout(_screen);
            ClearContentDynamic();
            _cardRect.gameObject.SetActive(true);
            _cardRect.anchoredPosition = _cardStart;
            var deck = DeckLoader.LoadDeckForLevel(
                _session.Run.LevelIndex,
                _config.CardsPerLevel,
                _config,
                forceLoseDeck,
                _session.Run);
            _level.Begin(deck, _session.Run);
            _headerText.text = Loc.Format("ui.level.header", _session.Run.LevelIndex + 1);
            RefreshLevelCard();
            RefreshStats();
            RefreshLevelActions();
        }

        void RefreshLevelActions()
        {
            if (_screen != FlowScreen.Level || _level.Outcome != LevelOutcome.InProgress)
            {
                return;
            }

            var card = _level.CurrentCard;
            if (card == null)
            {
                ClearActions();
                FinalizeActions();
                return;
            }

            bool hasSkill = _progression.CanUseSkillOnCard(_session.Run, card);
            _hintText.text = Loc.Get(hasSkill ? "ui.level.swipe_hint_skill" : "ui.level.swipe_hint");
            _statusText.text = Loc.Format("ui.level.xp", _session.Run.LevelXp, _config.XpToCompleteLevel);

            ClearActions();
            AddAction(card.LeftActionName, ChooseLeft, ArrowSlot.Left);
            if (hasSkill)
            {
                var skillId = _progression.ResolveSkillForCard(_session.Run, card);
                int charges = _progression.GetCharges(_session.Run, skillId);
                string skillName = string.IsNullOrWhiteSpace(card.SkillActionName)
                    ? DbtSkillCatalog.GetDisplayName(skillId)
                    : card.SkillActionName;
                AddAction(Loc.Format("ui.action.skill_charges", skillName, charges), TryChooseSkill, ArrowSlot.Up);
            }

            AddAction(card.RightActionName, ChooseRight, ArrowSlot.Right);
            FinalizeActions();
        }

        void RefreshLevelCard()
        {
            var card = _level.CurrentCard;
            if (card == null)
            {
                _bodyText.text = _level.Outcome == LevelOutcome.Won
                    ? Loc.Get("ui.level.win")
                    : Loc.Get("ui.level.lose");
                return;
            }

            _bodyText.text = card.SituationText;
        }

        void ChooseLeft()
        {
            if (_screen != FlowScreen.Level || _level.Outcome != LevelOutcome.InProgress)
            {
                return;
            }

            _level.ChooseLeft(_session.Run);
            AfterChoice();
        }

        void ChooseRight()
        {
            if (_screen != FlowScreen.Level || _level.Outcome != LevelOutcome.InProgress)
            {
                return;
            }

            _level.ChooseRight(_session.Run);
            AfterChoice();
        }

        void TryChooseSkill()
        {
            if (_screen != FlowScreen.Level || _level.Outcome != LevelOutcome.InProgress)
            {
                return;
            }

            var card = _level.CurrentCard;
            var skill = _progression.ResolveSkillForCard(_session.Run, card);
            if (skill == DbtSkillId.None)
            {
                return;
            }

            if (!_progression.TryConsumeSkillCharge(_session.Run, skill))
            {
                return;
            }

            _level.ChooseSkill(_session.Run);
            AfterChoice();
        }

        void AfterChoice()
        {
            RefreshStats();
            RefreshLevelCard();

            if (_level.Outcome == LevelOutcome.Won)
            {
                _progression.GrantLevelWinXp(_session.Run);
                _progression.MarkLevelCleared(_session.Run);
                if (_progression.HasReachedEnding(_session.Run))
                {
                    ShowEnding();
                }
                else
                {
                    ShowMeta(fromSplitSuccess: false);
                }
            }
            else if (_level.Outcome == LevelOutcome.Lost)
            {
                var fail = _level.FailStat ?? _level.ResolveLowestStat(_session.Run);
                BeginSplit(fail);
            }
            else
            {
                RefreshLevelActions();
            }
        }

        void BeginSplit(CoreStat failStat)
        {
            _screen = FlowScreen.Split;
            ApplyShellLayout(_screen);
            _activeSplitStat = failStat;
            _cardRect.gameObject.SetActive(false);
            ClearContentDynamic();
            _activeSplit = SplitMinigameFactory.Create(failStat);
            _activeSplit.Begin(OnSplitComplete);
            RefreshSplitUi();
        }

        void RefreshSplitUi()
        {
            if (_activeSplit == null)
            {
                return;
            }

            _headerText.text = Loc.Get(_activeSplit.TitleKey);
            string stepLine = Loc.Format(
                "ui.split.step",
                _activeSplit.CurrentStepIndex + 1,
                _activeSplit.TotalSteps);
            _bodyText.text = Loc.Get(_activeSplit.CurrentStepPromptKey) + "\n\n" + stepLine;
            _hintText.text = Loc.Get("ui.split.controls_hint");
            string edu = Loc.Get(SplitEduKey(_activeSplitStat));
            string crisis = string.IsNullOrEmpty(_activeSplit.CrisisResourceKey)
                ? string.Empty
                : "\n" + Loc.Get(_activeSplit.CrisisResourceKey);
            _statusText.text = edu + crisis;
            ClearActions();
            AddAction(Loc.Get(_activeSplit.SkillActionKey), OnSplitSkillChoice, ArrowSlot.Left);
            AddAction(Loc.Get(_activeSplit.FailActionKey), () => _activeSplit.SubmitFailure(), ArrowSlot.Right);
            FinalizeActions();
        }

        void OnSplitSkillChoice()
        {
            if (_activeSplit == null)
            {
                return;
            }

            int before = _activeSplit.CurrentStepIndex;
            _activeSplit.SubmitSuccess();
            if (_activeSplit != null && _screen == FlowScreen.Split && _activeSplit.CurrentStepIndex > before)
            {
                RefreshSplitUi();
            }
        }

        static string SplitEduKey(CoreStat failStat) => failStat switch
        {
            CoreStat.Safety => "ui.split.edu.safety",
            CoreStat.Stability => "ui.split.edu.stability",
            CoreStat.Wholeness => "ui.split.edu.wholeness",
            CoreStat.Closeness => "ui.split.edu.closeness",
            _ => "ui.split.edu.wholeness"
        };

        void OnSplitComplete(bool success)
        {
            _activeSplit = null;
            if (success)
            {
                var unlocked = SplitSkillRewards.SkillForFailStat(_activeSplitStat);
                _progression.GrantSplitSuccessXp(_session.Run);
                _progression.UnlockSkill(_session.Run, unlocked);
                _session.Run.RecordSplitSurvive(_activeSplitStat);
                _lastUnlockedSkill = unlocked;

                float floor = Mathf.Max(_config.SplitStabilizeFloor, _config.StartingStatValue * 0.4f);
                foreach (CoreStat stat in Enum.GetValues(typeof(CoreStat)))
                {
                    if (_session.Run.GetStat(stat) <= _config.FailThreshold)
                    {
                        _session.Run.SetStat(stat, floor, _config);
                    }
                }

                _level.ResumeAfterSplit();
                _screen = FlowScreen.Level;
                _cardRect.gameObject.SetActive(true);
                RefreshLevelCard();
                RefreshStats();
                RefreshLevelActions();
                _statusText.text = Loc.Format(
                    "ui.meta.skill_unlocked",
                    DbtSkillCatalog.GetDisplayName(unlocked),
                    DbtSkillCatalog.GetDescription(unlocked));
                _hintText.text = Loc.Get("ui.split.success");
            }
            else
            {
                _progression.MarkLevelRestart(_session.Run);
                _statusText.text = Loc.Get("ui.split.fail");
                _session.Run.RestoreLevelStartSnapshot(_config);
                StartLevel(forceLoseDeck: false);
            }
        }

        void ShowEnding()
        {
            _screen = FlowScreen.Ending;
            ApplyShellLayout(_screen);
            _cardRect.gameObject.SetActive(false);
            ClearContentDynamic();
            RefreshStats();
            var fantasy = EndingResolver.ResolveDominantFantasy(_session.Run, _config);
            _headerText.text = Loc.Get(EndingResolver.TitleKey(fantasy));
            _bodyText.text = Loc.Get(EndingResolver.BodyKey(fantasy, _session.Run.Character));
            _hintText.text = Loc.Get("ui.ending.hint");
            _statusText.text = fantasy + "\n" + Loc.Get("ui.crisis.resources");
            ClearActions();
            AddAction(Loc.Get("ui.ending.restart"), ShowCharacterSelect, ArrowSlot.Up);
            FinalizeActions();
        }

        void ShowTherapy()
        {
            _screen = FlowScreen.Therapy;
            ApplyShellLayout(_screen);
            _cardRect.gameObject.SetActive(false);
            TherapySelection.Clear(ref _therapySlotA, ref _therapySlotB);
            _therapyPhase = _session.Run.UnlockedEmotions.Count > 0
                ? TherapySelectionPhase.NeedPairing
                : TherapySelectionPhase.EmotionPairing;
            _headerText.text = Loc.Get("ui.therapy.title");
            RefreshTherapyUi(preserveStatus: false);
        }

        void SwitchTherapyPhase(TherapySelectionPhase phase)
        {
            _therapyPhase = phase;
            TherapySelection.Clear(ref _therapySlotA, ref _therapySlotB);
            RefreshTherapyUi(preserveStatus: false);
        }

        void RefreshTherapyUi(bool preserveStatus, string rewardBody = null)
        {
            if (_screen != FlowScreen.Therapy)
            {
                return;
            }

            string statusKeep = preserveStatus ? _statusText.text : string.Empty;
            _bodyText.text = !string.IsNullOrEmpty(rewardBody)
                ? rewardBody
                : Loc.Get(_therapyPhase == TherapySelectionPhase.NeedPairing
                    ? "ui.therapy.phase_need"
                    : "ui.therapy.phase_emotion");
            var bodyLe = _bodyText.GetComponent<LayoutElement>();
            bodyLe.preferredHeight = string.IsNullOrEmpty(rewardBody) ? 36f : 64f;
            bodyLe.minHeight = bodyLe.preferredHeight;
            _hintText.text = Loc.Get("ui.therapy.controls_hint");
            ClearActions();
            ClearContentDynamic();
            BuildTherapyBoard();

            if (_therapyPhase == TherapySelectionPhase.EmotionPairing)
            {
                if (_session.Run.UnlockedEmotions.Count > 0)
                {
                    AddAction(
                        Loc.Get("ui.therapy.use_emotions"),
                        () => SwitchTherapyPhase(TherapySelectionPhase.NeedPairing),
                        ArrowSlot.Up);
                }
            }
            else
            {
                AddAction(
                    Loc.Get("ui.therapy.new_chain"),
                    () => SwitchTherapyPhase(TherapySelectionPhase.EmotionPairing),
                    ArrowSlot.Up);
            }

            var merge = AddAction(Loc.Get("ui.therapy.merge"), MergeTherapySelection, ArrowSlot.Right);
            merge.IsMerge = true;
            AddAction(Loc.Get("ui.therapy.back"), () => ShowMeta(false), ArrowSlot.Left, isBack: true);
            FinalizeActions(forceFocusMode: true);
            HighlightSelectedTherapyBoard();
            RefreshTherapyLockedSlots();

            if (preserveStatus && !string.IsNullOrEmpty(statusKeep))
            {
                _statusText.text = statusKeep;
            }
            else
            {
                RefreshTherapySelectionStatus();
            }
        }

        void BuildTherapyBoard()
        {
            SetContentDynamicVisible(true);
            var board = UiFactory.CreateHorizontal(
                _contentDynamicHost,
                "TherapyBoard",
                spacing: 6f,
                padding: new RectOffset(0, 0, 0, 0));
            board.GetComponent<LayoutElement>().flexibleHeight = 0f;
            board.GetComponent<LayoutElement>().minHeight = 120f;
            board.GetComponent<LayoutElement>().preferredHeight = -1f;

            var sitCol = UiFactory.CreateColumn(board.transform, "ColSituation");
            var thoughtCol = UiFactory.CreateColumn(board.transform, "ColThought");
            var behaviorCol = UiFactory.CreateColumn(board.transform, "ColBehavior");

            bool emotionPhase = _therapyPhase == TherapySelectionPhase.EmotionPairing;

            UiFactory.CreateText(
                sitCol.transform,
                "H",
                Loc.Get(emotionPhase ? "ui.therapy.col_situation" : "ui.therapy.col_emotion"),
                TextRole.Caption,
                TextAnchor.MiddleCenter).GetComponent<LayoutElement>().preferredHeight = 22f;
            UiFactory.CreateText(thoughtCol.transform, "H", Loc.Get("ui.therapy.col_thought"), TextRole.Caption, TextAnchor.MiddleCenter)
                .GetComponent<LayoutElement>().preferredHeight = 22f;
            UiFactory.CreateText(behaviorCol.transform, "H", Loc.Get("ui.therapy.col_behavior"), TextRole.Caption, TextAnchor.MiddleCenter)
                .GetComponent<LayoutElement>().preferredHeight = 22f;

            if (emotionPhase)
            {
                foreach (var card in _therapy.StubCards)
                {
                    if (card.Kind == TherapyCardKind.Situation)
                    {
                        AddTherapyBoardCard(sitCol.transform, card, interactive: true);
                    }
                    else if (card.Kind == TherapyCardKind.Thought)
                    {
                        AddTherapyBoardCard(thoughtCol.transform, card, interactive: true);
                    }
                    else if (card.Kind == TherapyCardKind.Behavior)
                    {
                        AddTherapyBoardCard(behaviorCol.transform, card, interactive: false);
                    }
                }
            }
            else
            {
                foreach (var emotionId in _session.Run.UnlockedEmotions)
                {
                    if (_therapy.TryGetCard(emotionId, out var emotion))
                    {
                        AddTherapyBoardCard(sitCol.transform, emotion, interactive: true);
                    }
                }

                foreach (var card in _therapy.StubCards)
                {
                    if (card.Kind == TherapyCardKind.Thought)
                    {
                        AddTherapyBoardCard(thoughtCol.transform, card, interactive: false);
                    }
                    else if (card.Kind == TherapyCardKind.Behavior)
                    {
                        AddTherapyBoardCard(behaviorCol.transform, card, interactive: true);
                    }
                }
            }

            var slots = UiFactory.CreateHorizontal(
                _contentDynamicHost,
                "LockedSlots",
                spacing: 8f,
                padding: new RectOffset(0, 0, 4, 0));
            var slotsLe = slots.GetComponent<LayoutElement>();
            slotsLe.flexibleHeight = 0f;
            slotsLe.minHeight = 40f;
            slotsLe.preferredHeight = 44f;

            _therapyEmotionSlotText = UiFactory.CreateText(
                slots.transform,
                "EmotionSlot",
                Loc.Get("ui.therapy.locked_emotion"),
                TextRole.Caption,
                TextAnchor.MiddleCenter);
            _therapyEmotionSlotText.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _therapyEmotionSlotText.GetComponent<LayoutElement>().minHeight = 40f;

            _therapyNeedSlotText = UiFactory.CreateText(
                slots.transform,
                "NeedSlot",
                Loc.Get("ui.therapy.locked_need"),
                TextRole.Caption,
                TextAnchor.MiddleCenter);
            _therapyNeedSlotText.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _therapyNeedSlotText.GetComponent<LayoutElement>().minHeight = 40f;
            ResetDynamicScroll();
        }

        void AddTherapyBoardCard(Transform column, TherapyCard card, bool interactive)
        {
            var captured = card;
            string label = Loc.Get(card.TextKey);
            var button = UiFactory.CreateCompactRowButton(
                column,
                "TherapyCard_" + card.Id,
                label,
                UiFactory.CompactCardMinHeight);
            var image = button.GetComponent<Image>();
            var baseColor = interactive ? CompactNormal : CompactDimmed;
            image.color = baseColor;
            button.interactable = interactive;
            if (interactive)
            {
                button.onClick.AddListener(() => SelectTherapyCard(captured));
            }

            _therapyBoardCards.Add(new BoardCardUi
            {
                Card = captured,
                Button = button,
                Image = image,
                Label = button.GetComponentInChildren<Text>(),
                Interactive = interactive,
                BaseColor = baseColor
            });
        }

        void SelectTherapyCard(TherapyCard card)
        {
            TherapySelection.ApplyClick(_therapyPhase, card, ref _therapySlotA, ref _therapySlotB);
            RefreshTherapySelectionStatus();
            HighlightSelectedTherapyBoard();
            RefreshTherapyLockedSlots();
        }

        void RefreshTherapySelectionStatus()
        {
            if (_therapySlotA == null && _therapySlotB == null)
            {
                _statusText.text = string.Empty;
                return;
            }

            var parts = new List<string>(2);
            if (_therapySlotA != null)
            {
                parts.Add(Loc.Get(_therapySlotA.TextKey));
            }

            if (_therapySlotB != null)
            {
                parts.Add(Loc.Get(_therapySlotB.TextKey));
            }

            _statusText.text = Loc.Format("ui.therapy.selected", string.Join(" + ", parts));
        }

        void HighlightSelectedTherapyBoard()
        {
            for (int i = 0; i < _therapyBoardCards.Count; i++)
            {
                var entry = _therapyBoardCards[i];
                if (!entry.Interactive || entry.Image == null || entry.Label == null)
                {
                    continue;
                }

                bool selected = TherapySelection.IsSelected(_therapySlotA, _therapySlotB, entry.Card);
                string plain = Loc.Get(entry.Card.TextKey);
                entry.Label.text = selected
                    ? Loc.Format("ui.therapy.selected_mark", plain)
                    : plain;
                entry.Image.color = selected ? CompactSelected : entry.BaseColor;
            }
        }

        void RefreshTherapyLockedSlots()
        {
            if (_therapyEmotionSlotText == null || _therapyNeedSlotText == null)
            {
                return;
            }

            if (_therapySlotA != null && _therapySlotA.Kind == TherapyCardKind.Emotion)
            {
                _therapyEmotionSlotText.text = Loc.Format("ui.therapy.slot_emotion", Loc.Get(_therapySlotA.TextKey));
            }
            else if (_session.Run.UnlockedEmotions.Count > 0)
            {
                string emotionName = null;
                foreach (var id in _session.Run.UnlockedEmotions)
                {
                    if (_therapy.TryGetCard(id, out var emotion))
                    {
                        emotionName = Loc.Get(emotion.TextKey);
                        break;
                    }
                }

                _therapyEmotionSlotText.text = emotionName != null
                    ? Loc.Format("ui.therapy.slot_emotion", emotionName)
                    : Loc.Get("ui.therapy.locked_emotion");
            }
            else
            {
                _therapyEmotionSlotText.text = Loc.Get("ui.therapy.locked_emotion");
            }

            if (_session.Run.UnlockedNeeds.Count > 0)
            {
                string needName = null;
                foreach (var id in _session.Run.UnlockedNeeds)
                {
                    if (_therapy.TryGetCard(id, out var need))
                    {
                        needName = Loc.Get(need.TextKey);
                        break;
                    }
                }

                _therapyNeedSlotText.text = needName != null
                    ? Loc.Format("ui.therapy.slot_need", needName)
                    : Loc.Get("ui.therapy.locked_need");
            }
            else
            {
                _therapyNeedSlotText.text = Loc.Get("ui.therapy.locked_need");
            }
        }

        void ShowMeta(bool fromSplitSuccess)
        {
            _screen = FlowScreen.Meta;
            ApplyShellLayout(_screen);
            _cardRect.gameObject.SetActive(false);
            RefreshStats();
            _headerText.text = Loc.Get("ui.meta.title");

            string skillLine = string.Empty;
            if (_lastUnlockedSkill != DbtSkillId.None)
            {
                skillLine = Loc.Format(
                    "ui.meta.skill_unlocked",
                    DbtSkillCatalog.GetDisplayName(_lastUnlockedSkill),
                    DbtSkillCatalog.GetDescription(_lastUnlockedSkill));
            }

            var symptoms = (BpdSymptom[])Enum.GetValues(typeof(BpdSymptom));
            if (_metaSymptomIndex < 0 || _metaSymptomIndex >= symptoms.Length)
            {
                _metaSymptomIndex = 0;
            }

            var focused = symptoms[_metaSymptomIndex];
            // Keep body short — edu lives under the tree inside the scroll host.
            _bodyText.text = Loc.Format("ui.meta.xp", _session.Run.UnspentXp) +
                             (string.IsNullOrEmpty(skillLine) ? string.Empty : "\n" + skillLine);
            var bodyLe = _bodyText.GetComponent<LayoutElement>();
            bodyLe.preferredHeight = string.IsNullOrEmpty(skillLine) ? 28f : 52f;
            bodyLe.minHeight = bodyLe.preferredHeight;

            _statusText.text = Loc.Format("ui.meta.memories", _session.Run.TraumaticMemoriesUnlocked);
            _hintText.text = Loc.Get("ui.meta.controls_hint");

            ClearActions();
            ClearContentDynamic();
            BuildSymptomTree(symptoms, focused);

            AddAction(
                Loc.Format("ui.meta.reduce_symptom", Loc.Get(SymptomCatalog.DisplayNameKey(focused))),
                () =>
                {
                    if (_progression.TryReduceSymptom(_session.Run, focused))
                    {
                        ShowMeta(fromSplitSuccess);
                    }
                },
                ArrowSlot.Up);
            AddAction(Loc.Get("ui.meta.therapy"), ShowTherapy, ArrowSlot.Left);
            AddAction(Loc.Get("ui.meta.next_level"), () =>
            {
                _progression.AdvanceToNextLevel(_session.Run);
                _lastUnlockedSkill = DbtSkillId.None;
                StartLevel(forceLoseDeck: false);
            }, ArrowSlot.Right);
            FinalizeActions();
        }

        void BuildSymptomTree(BpdSymptom[] symptoms, BpdSymptom focused)
        {
            SetContentDynamicVisible(true);
            var title = UiFactory.CreateText(
                _contentDynamicHost,
                "TreeTitle",
                Loc.Get("ui.meta.symptom_tree"),
                TextRole.Caption,
                TextAnchor.MiddleLeft);
            var titleLe = title.GetComponent<LayoutElement>();
            titleLe.minHeight = 20f;
            titleLe.preferredHeight = 22f;

            int max = _config.MaxSymptomSeverity;
            for (int i = 0; i < symptoms.Length; i++)
            {
                var symptom = symptoms[i];
                int index = i;
                int sev = _session.Run.GetSymptomSeverity(symptom);
                string row = Loc.Format(
                    "ui.meta.symptom_row",
                    Loc.Get(SymptomCatalog.DisplayNameKey(symptom)),
                    BuildSeverityBar(sev, max),
                    sev,
                    max);
                var button = UiFactory.CreateCompactRowButton(_contentDynamicHost, "Symptom_" + symptom, row);
                button.onClick.AddListener(() =>
                {
                    _metaSymptomIndex = index;
                    ShowMeta(false);
                });
                _symptomTreeButtons[symptom] = button;
                if (symptom == focused)
                {
                    button.GetComponent<Image>().color = CompactSelected;
                }
            }

            string detail = Loc.Get(SymptomCatalog.DisplayNameKey(focused)) + "\n" +
                            Loc.Get(SymptomCatalog.EduKey(focused)) + "\n" +
                            Loc.Get(SymptomCatalog.ModifierKey(focused));
            var detailText = UiFactory.CreateText(
                _contentDynamicHost,
                "SymptomDetail",
                detail,
                TextRole.Caption,
                TextAnchor.UpperLeft);
            var detailLe = detailText.GetComponent<LayoutElement>();
            detailLe.minHeight = 48f;
            detailLe.preferredHeight = 72f;
            ResetDynamicScroll();
        }

        void ResetDynamicScroll()
        {
            if (_contentScrollRoot == null || _contentDynamicHost == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentDynamicHost);
            var scroll = _contentScrollRoot.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        void MergeTherapySelection()
        {
            if (!TherapySelection.HasCompletePair(_therapySlotA, _therapySlotB))
            {
                _statusText.text = Loc.Get("therapy.error.null");
                return;
            }

            var result = _therapy.TryMerge(_therapySlotA, _therapySlotB);
            if (!result.Success)
            {
                _statusText.text = Loc.Get(result.FailureReasonKey);
                return;
            }

            string mergeResult = Loc.Format("ui.therapy.result", Loc.Get(result.Produced.TextKey));
            string rewardBody = string.Empty;
            string hintExtra = string.Empty;

            if (result.Produced.Kind == TherapyCardKind.Emotion)
            {
                _session.Run.UnlockedEmotions.Add(result.Produced.Id);
                if (_therapy.TryGetCard(result.Produced.Id, out var storedEmotion))
                {
                    storedEmotion.Locked = false;
                }

                rewardBody = Loc.Format(
                    "ui.therapy.unlocked_emotion",
                    Loc.Get(result.Produced.TextKey));
                if (_session.Run.TryUnlockMemory(result.Produced.MemoryId))
                {
                    hintExtra = Loc.Get("ui.therapy.memory");
                }

                _therapyPhase = TherapySelectionPhase.NeedPairing;
                _therapySlotA = result.Produced;
                _therapySlotB = null;
            }
            else if (result.Produced.Kind == TherapyCardKind.Need)
            {
                _session.Run.UnlockedNeeds.Add(result.Produced.Id);
                rewardBody = Loc.Format(
                    "ui.therapy.unlocked_need",
                    Loc.Get(result.Produced.TextKey));
                if (!string.IsNullOrEmpty(result.Produced.SkillUnlockId) &&
                    Enum.TryParse(result.Produced.SkillUnlockId, out DbtSkillId skill))
                {
                    _progression.UnlockSkill(_session.Run, skill);
                    _lastUnlockedSkill = skill;
                    string skillLine = Loc.Format(
                        "ui.meta.skill_unlocked",
                        DbtSkillCatalog.GetDisplayName(skill),
                        DbtSkillCatalog.GetDescription(skill));
                    rewardBody += "\n" + skillLine;
                    hintExtra = skillLine;
                }

                _therapyPhase = _session.Run.UnlockedEmotions.Count > 0
                    ? TherapySelectionPhase.NeedPairing
                    : TherapySelectionPhase.EmotionPairing;
                TherapySelection.Clear(ref _therapySlotA, ref _therapySlotB);
            }
            else
            {
                TherapySelection.Clear(ref _therapySlotA, ref _therapySlotB);
            }

            RefreshTherapyUi(preserveStatus: true, rewardBody: rewardBody);
            _statusText.text = mergeResult;
            if (!string.IsNullOrEmpty(hintExtra))
            {
                _hintText.text = Loc.Get("ui.therapy.controls_hint") + "\n" + hintExtra;
            }
        }
    }
}
