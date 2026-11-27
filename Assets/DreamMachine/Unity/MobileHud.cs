using UnityEngine;
using UnityEngine.UI;

namespace DreamMachine.UnityBridge
{
    /// <summary>
    /// Touch-friendly board: beliefs, appendices, emotion dials, and the experiment button.
    /// Sliders are gauges. The mind overwrites them; they are not on/off switches.
    /// </summary>
    public sealed class MobileHud : MonoBehaviour
    {
        Text _status;
        Text _beliefs;
        Text _result;
        Text _title;
        RawImage _eye;
        RawImage _seg;
        Slider _curiosity;
        Slider _frustration;
        Slider _satisfaction;
        Slider _fear;
        Slider _fatigue;
        Text _affectReadout;
        RectTransform _board;
        RectTransform _persistentActions;
        Font _font;
        bool _portrait;

        public readonly System.Collections.Generic.List<Button> PersonButtons = new System.Collections.Generic.List<Button>();

        public void Build(Font font)
        {
            _font = font;
            _named = new System.Collections.Generic.Dictionary<string, RectTransform>();
            var canvasGo = new GameObject("HUD");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.35f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _persistentActions = new GameObject("Simulation entry points", typeof(RectTransform), typeof(Canvas),
                typeof(GraphicRaycaster)).GetComponent<RectTransform>();
            _persistentActions.SetParent(canvasGo.transform, false);
            _persistentActions.anchorMin = _persistentActions.anchorMax = new Vector2(0f, 1f);
            _persistentActions.pivot = new Vector2(0f, 1f);
            _persistentActions.anchoredPosition = new Vector2(14f, -14f);
            _persistentActions.sizeDelta = new Vector2(410f, 55f);
            Canvas actionCanvas = _persistentActions.GetComponent<Canvas>();
            actionCanvas.overrideSorting = true;
            actionCanvas.sortingOrder = 30;

            _board = Panel(canvasGo.transform, "Board", new Color(0.07f, 0.08f, 0.10f, 0.88f));
            _title = Label(_board, "Title", 34, FontStyle.Bold, TextAnchor.MiddleLeft);
            _status = Label(_board, "Status", 24, FontStyle.Normal, TextAnchor.UpperLeft);
            var selectors = new GameObject("Selectors");
            selectors.transform.SetParent(_board, false);
            var selectorRect = selectors.AddComponent<RectTransform>();
            _eye = Preview(_board, "Eye");
            _seg = Preview(_board, "Seg");
            _curiosity = Gauge(_board, "Curiosity", new Color(0.95f, 0.78f, 0.28f));
            _frustration = Gauge(_board, "Frustration", new Color(0.85f, 0.42f, 0.28f));
            _satisfaction = Gauge(_board, "Satisfaction", new Color(0.45f, 0.75f, 0.45f));
            _fear = Gauge(_board, "Fear of falling", new Color(0.55f, 0.45f, 0.85f));
            _fatigue = Gauge(_board, "Fatigue", new Color(0.45f, 0.55f, 0.62f));
            _affectReadout = Label(_board, "Affect", 22, FontStyle.Italic, TextAnchor.MiddleLeft);
            var beliefScroll = Scroll(_board, "Beliefs", out _beliefs);
            var resultScroll = Scroll(_board, "Result", out _result);
            _beliefs.fontSize = 24;
            _result.fontSize = 24;
            _named["Title"] = _title.rectTransform;
            _named["Status"] = _status.rectTransform;
            _named["Affect"] = _affectReadout.rectTransform;
            Remember(selectorRect);
            Remember(beliefScroll);
            Remember(resultScroll);
            Layout(Screen.height >= Screen.width);
        }

        System.Collections.Generic.Dictionary<string, RectTransform> _named;

        void Remember(RectTransform rect)
        {
            _named[rect.name] = rect;
        }

        public Button AddPersonButton(string name, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform selectors = _named["Selectors"];
            var button = MakeButton(selectors, name, onClick);
            PersonButtons.Add(button);
            LayoutPersonButtons();
            return button;
        }

        public void AddSimulationEntryPoints(UnityEngine.Events.UnityAction toggle, UnityEngine.Events.UnityAction reset)
        {
            PlaceButton(MakeButton(_persistentActions, "Simulation", toggle), 0f, 0.53f);
            PlaceButton(MakeButton(_persistentActions, "Reset layout", reset), 0.53f, 1f);

            void PlaceButton(Button button, float from, float to)
            {
                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(from, 0f);
                rect.anchorMax = new Vector2(to, 1f);
                rect.offsetMin = new Vector2(2f, 0f);
                rect.offsetMax = new Vector2(-2f, 0f);
            }
        }

        public void AddLivingEntryPoints(UnityEngine.Events.UnityAction experience, UnityEngine.Events.UnityAction research)
        {
            _persistentActions.sizeDelta = new Vector2(740f, 55f);
            MakeButton(_persistentActions, "Life & experience", experience);
            MakeButton(_persistentActions, "Living / research", research);
            for (int i = 0; i < _persistentActions.childCount; i++)
            {
                var rect = (RectTransform)_persistentActions.GetChild(i);
                rect.anchorMin = new Vector2(i * .25f, 0f);
                rect.anchorMax = new Vector2((i + 1) * .25f, 1f);
                rect.offsetMin = new Vector2(2f, 0f);
                rect.offsetMax = new Vector2(-2f, 0f);
            }
        }

        public void ShowResearch(bool visible) => _board.gameObject.SetActive(visible);

        public Button AddActionButton(string name, UnityEngine.Events.UnityAction onClick)
        {
            if (!_named.ContainsKey("Actions"))
            {
                var go = new GameObject("Actions");
                go.transform.SetParent(_board, false);
                var rect = go.AddComponent<RectTransform>();
                _named["Actions"] = rect;
            }
            var button = MakeButton(_named["Actions"], name, onClick);
            Layout(_portrait);
            return button;
        }

        public void LayoutPersonButtons()
        {
            int n = PersonButtons.Count;
            if (n == 0) return;
            for (int i = 0; i < n; i++)
            {
                var rect = PersonButtons[i].GetComponent<RectTransform>();
                float w = 1f / n;
                rect.anchorMin = new Vector2(i * w, 0f);
                rect.anchorMax = new Vector2((i + 1) * w, 1f);
                rect.offsetMin = new Vector2(4f, 4f);
                rect.offsetMax = new Vector2(-4f, -4f);
            }
        }

        public void LayoutActionButtons()
        {
            if (!_named.ContainsKey("Actions")) return;
            var parent = _named["Actions"];
            int n = parent.childCount;
            for (int i = 0; i < n; i++)
            {
                var rect = parent.GetChild(i).GetComponent<RectTransform>();
                float w = 1f / n;
                rect.anchorMin = new Vector2(i * w, 0f);
                rect.anchorMax = new Vector2((i + 1) * w, 1f);
                rect.offsetMin = new Vector2(4f, 4f);
                rect.offsetMax = new Vector2(-4f, -4f);
            }
        }

        void Update()
        {
            bool portrait = Screen.height >= Screen.width;
            if (portrait != _portrait) Layout(portrait);
        }

        void Layout(bool portrait)
        {
            _portrait = portrait;
            if (portrait)
            {
                _board.anchorMin = new Vector2(0.02f, 0.01f);
                _board.anchorMax = new Vector2(0.98f, 0.56f);
            }
            else
            {
                _board.anchorMin = new Vector2(0.58f, 0.02f);
                _board.anchorMax = new Vector2(0.99f, 0.98f);
            }
            _board.offsetMin = Vector2.zero;
            _board.offsetMax = Vector2.zero;
            Place("Title", 0.90f, 1.00f);
            Place("Status", 0.80f, 0.90f);
            Place("Selectors", 0.72f, 0.80f);
            Place("Eye", 0.56f, 0.72f);
            Place("Seg", 0.56f, 0.72f);
            var eye = _named["Eye"];
            var seg = _named["Seg"];
            eye.anchorMin = new Vector2(0.03f, 0.56f);
            eye.anchorMax = new Vector2(0.49f, 0.72f);
            seg.anchorMin = new Vector2(0.51f, 0.56f);
            seg.anchorMax = new Vector2(0.97f, 0.72f);
            Place("Curiosity", 0.50f, 0.55f);
            Place("Frustration", 0.45f, 0.50f);
            Place("Satisfaction", 0.40f, 0.45f);
            Place("Fear of falling", 0.35f, 0.40f);
            Place("Fatigue", 0.30f, 0.35f);
            Place("Affect", 0.26f, 0.30f);
            Place("Beliefs", 0.14f, 0.26f);
            Place("Actions", 0.07f, 0.14f);
            Place("Result", 0.01f, 0.07f);
            LayoutActionButtons();
        }

        void Place(string name, float minY, float maxY)
        {
            RectTransform rect;
            if (!_named.TryGetValue(name, out rect) || rect == null) return;
            rect.anchorMin = new Vector2(0.03f, minY);
            rect.anchorMax = new Vector2(0.97f, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public void SetStatus(string title, string status)
        {
            _title.text = title;
            _status.text = status;
        }

        public void SetBeliefs(string text)
        {
            _beliefs.text = text;
        }

        public void SetResult(string text)
        {
            _result.text = text;
        }

        public void SetPreviews(Texture eye, Texture seg)
        {
            _eye.texture = eye;
            _seg.texture = seg;
        }

        public void SetEmotions(float curiosity, float frustration, float satisfaction, float fear, float fatigue, float valence, float arousal, float sleep)
        {
            _curiosity.SetValueWithoutNotify(curiosity);
            _frustration.SetValueWithoutNotify(frustration);
            _satisfaction.SetValueWithoutNotify(satisfaction);
            _fear.SetValueWithoutNotify(fear);
            _fatigue.SetValueWithoutNotify(fatigue);
            _affectReadout.text = "valence " + valence.ToString("0.00")
                + "   arousal " + arousal.ToString("0.00")
                + "   sleep " + sleep.ToString("0.00");
        }

        public void HighlightPerson(string id)
        {
            for (int i = 0; i < PersonButtons.Count; i++)
            {
                var image = PersonButtons[i].GetComponent<Image>();
                bool on = PersonButtons[i].name == id;
                image.color = on ? new Color(0.85f, 0.62f, 0.22f, 1f) : new Color(0.18f, 0.20f, 0.24f, 1f);
            }
        }

        Button MakeButton(RectTransform parent, string name, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.20f, 0.24f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            var text = Label(go.transform, "Caption", 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.text = name;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 24;
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }

        Slider Gauge(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            _named[name] = rect;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.13f, 0.15f, 1f);
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(8f, 8f);
            fillRect.offsetMax = new Vector2(-8f, -8f);
            var fill = fillGo.AddComponent<Image>();
            fill.color = color;
            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = bg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;
            var colors = slider.colors;
            colors.disabledColor = Color.white;
            slider.colors = colors;
            var caption = Label(go.transform, "Caption", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            caption.text = "  " + name;
            caption.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            var cap = caption.rectTransform;
            cap.anchorMin = Vector2.zero;
            cap.anchorMax = Vector2.one;
            cap.offsetMin = Vector2.zero;
            cap.offsetMax = Vector2.zero;
            return slider;
        }

        RawImage Preview(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            _named[name] = rect;
            var image = go.AddComponent<RawImage>();
            image.color = Color.white;
            var caption = Label(go.transform, name + "Label", 18, FontStyle.Bold, TextAnchor.UpperLeft);
            caption.text = name == "Eye" ? " Eye" : " IDs";
            var cap = caption.rectTransform;
            cap.anchorMin = new Vector2(0f, 0.75f);
            cap.anchorMax = Vector2.one;
            return image;
        }

        RectTransform Scroll(RectTransform parent, string name, out Text body)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.11f, 0.13f, 1f);
            var scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(go.transform, false);
            var viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(8f, 8f);
            viewRect.offsetMax = new Vector2(-8f, -8f);
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);
            body = Label(content.transform, "Body", 24, FontStyle.Normal, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.sizeDelta = new Vector2(0f, 800f);
            var fitter = body.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            scroll.viewport = viewRect;
            scroll.content = bodyRect;
            return rect;
        }

        RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        Text Label(Transform parent, string name, int size, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = new Color(0.93f, 0.94f, 0.95f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
    }
}
