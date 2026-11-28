using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamMachine.UnityBridge
{
    // Stores observer preferences independently of the simulation and its saves.
    public sealed class TrayWorkspace : IDisposable
    {
        [Serializable]
        public struct Layout
        {
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public bool Visible;
        }

        sealed class Tray
        {
            public string Name;
            public Vector2 MinimumSize;
            public Layout Layout;
            public Action<RectTransform> Open;
            public Action Close;
            public Action Refresh;
            public GameObject Root;
            public RectTransform Content;
        }

        readonly string _preferencePrefix;
        readonly Dictionary<string, Tray> _trays = new Dictionary<string, Tray>();

        public TrayWorkspace(string preferencePrefix)
        {
            _preferencePrefix = preferencePrefix;
        }

        public void Register(string id, string displayName, Vector2 minimumSize, Layout defaults,
            Action<RectTransform> open, Action close, Action refresh = null)
        {
            string saved = PlayerPrefs.GetString(_preferencePrefix + id, "");
            Layout layout = defaults;
            if (!string.IsNullOrEmpty(saved))
            {
                try
                {
                    Layout candidate = JsonUtility.FromJson<Layout>(saved);
                    if (IsValid(candidate)) layout = candidate;
                }
                catch (ArgumentException) { }
            }
            _trays.Add(id, new Tray
            {
                Name = displayName, MinimumSize = minimumSize, Layout = layout,
                Open = open, Close = close, Refresh = refresh
            });
        }

        static bool IsValid(Layout layout) =>
            layout.AnchorMin.x >= 0 && layout.AnchorMin.y >= 0 &&
            layout.AnchorMax.x <= 1 && layout.AnchorMax.y <= 1 &&
            layout.AnchorMin.x < layout.AnchorMax.x && layout.AnchorMin.y < layout.AnchorMax.y;

        public Layout ReadLayout(string id) => _trays[id].Layout;

        public void RestoreVisibility()
        {
            foreach (Tray tray in _trays.Values)
                if (tray.Layout.Visible && tray.Root == null) Open(tray);
        }

        public void Toggle(string id)
        {
            Layout layout = _trays[id].Layout;
            layout.Visible = !layout.Visible;
            SetLayout(id, layout);
        }

        public void SetLayout(string id, Layout layout)
        {
            if (!IsValid(layout)) throw new ArgumentException("Tray anchors must describe a rectangle inside the viewport.");
            Tray tray = _trays[id];
            tray.Layout = layout;
            if (layout.Visible)
            {
                if (tray.Root == null) Open(tray);
                else ApplyLayout(tray);
            }
            else Close(tray);
            PlayerPrefs.SetString(_preferencePrefix + id, JsonUtility.ToJson(layout));
            PlayerPrefs.Save();
        }

        public void RefreshVisible()
        {
            foreach (Tray tray in _trays.Values)
            {
                if (tray.Root == null) continue;
                ApplyLayout(tray);
                tray.Refresh?.Invoke();
            }
        }

        static void Open(Tray tray)
        {
            tray.Root = new GameObject(tray.Name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = tray.Root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = tray.Root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.35f;
            tray.Content = new GameObject("Content", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            tray.Content.SetParent(tray.Root.transform, false);
            tray.Content.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.94f);
            ApplyLayout(tray);
            tray.Open(tray.Content);
            tray.Refresh?.Invoke();
        }

        static void ApplyLayout(Tray tray)
        {
            var viewport = (RectTransform)tray.Root.transform;
            Vector2 size = viewport.rect.size;
            Vector2 minimum = new Vector2(tray.MinimumSize.x / Mathf.Max(1, size.x),
                tray.MinimumSize.y / Mathf.Max(1, size.y));
            Vector2 upper = tray.Layout.AnchorMax;
            Vector2 lower = Vector2.Max(Vector2.zero, Vector2.Min(tray.Layout.AnchorMin, upper - minimum));
            if (tray.Content.anchorMin != lower) tray.Content.anchorMin = lower;
            if (tray.Content.anchorMax != upper) tray.Content.anchorMax = upper;
            tray.Content.offsetMin = Vector2.zero;
            tray.Content.offsetMax = Vector2.zero;
        }

        static void Close(Tray tray)
        {
            if (tray.Root == null) return;
            tray.Close?.Invoke();
            tray.Root.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(tray.Root);
            else UnityEngine.Object.DestroyImmediate(tray.Root);
            tray.Root = null;
            tray.Content = null;
        }

        public void Dispose()
        {
            foreach (Tray tray in _trays.Values) Close(tray);
            _trays.Clear();
        }
    }
}
