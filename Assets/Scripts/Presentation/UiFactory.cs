using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BPD.Presentation
{
    public enum TextRole
    {
        Title,
        Body,
        Button,
        Caption
    }

    public static class UiFactory
    {
        public const int TitleSize = 28;
        public const int BodySize = 22;
        public const int ButtonSize = 20;
        public const int CaptionSize = 18;

        public const float ButtonMinHeight = 72f;
        public const float ButtonPreferredHeight = 80f;
        public const float CompactRowMinHeight = 36f;
        public const float CompactCardMinHeight = 52f;

        static Font _font;

        public static Canvas CreateAdaptiveCanvas(string name, out RectTransform safeArea, out AdaptiveShell shell)
        {
            var root = new GameObject(
                name,
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(AdaptiveShell));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            shell = root.GetComponent<AdaptiveShell>();

            var bg = CreatePanel(root.transform, "Background", new Color(0.08f, 0.09f, 0.11f, 1f));
            Stretch(bg.rectTransform);

            var safeGo = new GameObject("SafeArea", typeof(RectTransform));
            safeGo.transform.SetParent(root.transform, false);
            safeArea = safeGo.GetComponent<RectTransform>();
            Stretch(safeArea);
            shell.Bind(safeArea);

            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(go);
        }

        public static Font GetFont()
        {
            if (_font != null)
            {
                return _font;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return _font;
        }

        public static Text CreateText(Transform parent, string name, string content, TextRole role, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = GetFont();
            text.text = content;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
            ApplyRole(text, role);
            return text;
        }

        public static void ApplyRole(Text text, TextRole role)
        {
            // Never use best-fit for chrome — it blurs legacy bitmap fonts under non-integer sizes.
            text.resizeTextForBestFit = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;

            switch (role)
            {
                case TextRole.Title:
                    text.fontSize = TitleSize;
                    text.fontStyle = FontStyle.Bold;
                    break;
                case TextRole.Body:
                    text.fontSize = BodySize;
                    text.fontStyle = FontStyle.Normal;
                    break;
                case TextRole.Button:
                    text.fontSize = ButtonSize;
                    text.fontStyle = FontStyle.Bold;
                    break;
                case TextRole.Caption:
                    text.fontSize = CaptionSize;
                    text.fontStyle = FontStyle.Bold;
                    text.color = new Color(0.95f, 0.96f, 0.98f, 1f);
                    break;
            }
        }

        public static Button CreateFlexibleButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = ButtonMinHeight;
            layout.preferredHeight = ButtonPreferredHeight;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 0f;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.24f, 0.28f, 1f);
            // Solid color UI — avoid filtering blur from stretched sprites.
            image.useSpriteMesh = false;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.30f, 0.33f, 0.38f, 1f);
            colors.pressedColor = new Color(0.18f, 0.20f, 0.24f, 1f);
            button.colors = colors;

            var text = CreateText(go.transform, "Label", label, TextRole.Button, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 16, 16, 10, 10);
            var textLayout = text.GetComponent<LayoutElement>();
            textLayout.ignoreLayout = true;

            return button;
        }

        /// <summary>Compact selectable row for symptom tree / therapy board (lives in content, not ActionsBand).</summary>
        public static Button CreateCompactRowButton(Transform parent, string name, string label, float minHeight = CompactRowMinHeight)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = minHeight;
            layout.preferredHeight = minHeight;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 0f;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.18f, 0.19f, 0.22f, 1f);
            image.useSpriteMesh = false;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.28f, 0.30f, 0.34f, 1f);
            colors.pressedColor = new Color(0.14f, 0.15f, 0.18f, 1f);
            button.colors = colors;

            var text = CreateText(go.transform, "Label", label, TextRole.Caption, TextAnchor.MiddleLeft);
            text.alignment = TextAnchor.MiddleLeft;
            Stretch(text.rectTransform, 10, 10, 4, 4);
            var textLayout = text.GetComponent<LayoutElement>();
            textLayout.ignoreLayout = true;

            return button;
        }

        public static HorizontalLayoutGroup CreateHorizontal(
            Transform parent,
            string name,
            float spacing,
            RectOffset padding,
            bool forceExpandWidth = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;
            le.minHeight = 80f;
            le.preferredHeight = -1f;

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = forceExpandWidth;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static VerticalLayoutGroup CreateColumn(Transform parent, string name, float spacing = 4f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.11f, 0.12f, 0.14f, 0.95f);
            var le = go.GetComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;
            le.minWidth = 80f;
            le.minHeight = 60f;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>
        /// Scrollable content host with RectMask2D so tall trees/boards do not paint over actions.
        /// </summary>
        public static ScrollRect CreateScrollHost(Transform parent, string name, out RectTransform content)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            var rootLe = root.GetComponent<LayoutElement>();
            rootLe.flexibleWidth = 1f;
            rootLe.flexibleHeight = 1f;
            rootLe.minHeight = 80f;
            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.01f);
            rootImage.raycastTarget = true;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(root.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            Stretch(viewportRt);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 3f;
            vlg.padding = new RectOffset(0, 0, 0, 4);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.inertia = true;
            return scroll;
        }

        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Slider CreateSlider(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            var layout = root.GetComponent<LayoutElement>();
            layout.minHeight = 18f;
            layout.preferredHeight = 20f;
            layout.flexibleWidth = 1f;

            var slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.interactable = false;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), 4, 4, 4, 4);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.45f, 0.7f, 0.75f, 1f);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();
            return slider;
        }

        public static RectTransform CreateBand(Transform parent, string name, float flexibleHeight, float minHeight)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, minHeight);

            var le = go.GetComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.flexibleHeight = flexibleHeight;
            le.minHeight = minHeight;
            le.preferredHeight = minHeight;
            return rt;
        }

        public static VerticalLayoutGroup CreateVertical(
            Transform parent,
            string name,
            float spacing,
            RectOffset padding,
            bool forceExpandHeight = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = forceExpandHeight;
            return layout;
        }

        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            rt.localScale = Vector3.one;
        }

        /// <summary>
        /// Splits "Title — subtitle" Loc strings onto two lines for readable buttons.
        /// </summary>
        public static string ToMultilineActionLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return label;
            }

            int em = label.IndexOf('—');
            if (em < 0)
            {
                em = label.IndexOf('-');
            }

            if (em <= 0 || em >= label.Length - 1)
            {
                return label;
            }

            string left = label.Substring(0, em).TrimEnd();
            string right = label.Substring(em + 1).TrimStart(' ', '—', '-');
            return string.IsNullOrEmpty(right) ? left : $"{left}\n{right}";
        }
    }
}
