using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nomina
{
    /// <summary>
    /// Compact right-side panel showing recently detected words.
    /// Tapping an entry plays pronunciation. Self-contained: creates its own Canvas and UI.
    /// </summary>
    public class WordHistoryPanel : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform panelRoot;
        private RectTransform contentParent;
        private Button toggleButton;
        private TextMeshProUGUI toggleText;
        private bool isExpanded = false;

        private List<WordEntry> entries = new List<WordEntry>();
        private const int maxEntries = 15;
        private float panelWidth = 200f;
        private float entryHeight = 70f;

        private struct WordEntry
        {
            public string original;
            public string translated;
            public string langCode;
            public GameObject uiObject;
        }

        public void Initialize()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            // Screen-space overlay canvas for the history panel
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2340);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Panel container — right side, between top bar and bottom buttons
            var panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(transform, false);
            panelRoot = panelObj.AddComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(1, 0.15f);
            panelRoot.anchorMax = new Vector2(1, 0.85f);
            panelRoot.pivot = new Vector2(1, 0.5f);
            panelRoot.offsetMin = new Vector2(-panelWidth, 0);
            panelRoot.offsetMax = Vector2.zero;

            var panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0.06f, 0.06f, 0.1f, 0.88f);

            // Header
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelObj.transform, false);
            var headerRT = headerObj.AddComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.offsetMin = new Vector2(8, -36);
            headerRT.offsetMax = new Vector2(-8, -4);

            var headerText = headerObj.AddComponent<TextMeshProUGUI>();
            headerText.text = "Recent";
            headerText.fontSize = 22;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = new Color(0.7f, 0.75f, 0.85f, 0.9f);
            headerText.alignment = TextAlignmentOptions.Center;

            // Scroll area
            var scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            var scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(6, 6);
            scrollRT.offsetMax = new Vector2(-6, -40);

            var scrollImg = scrollObj.AddComponent<Image>();
            scrollImg.color = Color.clear;
            scrollObj.AddComponent<Mask>().showMaskGraphic = false;

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.viewport = scrollRT;

            // Scrollable content
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);
            contentParent = contentObj.AddComponent<RectTransform>();
            contentParent.anchorMin = new Vector2(0, 1);
            contentParent.anchorMax = new Vector2(1, 1);
            contentParent.pivot = new Vector2(0.5f, 1);
            contentParent.offsetMin = Vector2.zero;
            contentParent.offsetMax = Vector2.zero;

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentParent;

            // Toggle tab — small button on right edge when collapsed, or left of panel when expanded
            var tabObj = new GameObject("ToggleTab");
            tabObj.transform.SetParent(transform, false);
            var tabRT = tabObj.AddComponent<RectTransform>();
            tabRT.anchorMin = new Vector2(1, 0.45f);
            tabRT.anchorMax = new Vector2(1, 0.55f);
            tabRT.pivot = new Vector2(1, 0.5f);
            tabRT.offsetMin = new Vector2(-36, 0);
            tabRT.offsetMax = new Vector2(0, 0);

            var tabImg = tabObj.AddComponent<Image>();
            tabImg.color = new Color(0.15f, 0.4f, 0.9f, 0.9f);

            toggleButton = tabObj.AddComponent<Button>();
            toggleButton.targetGraphic = tabImg;
            toggleButton.onClick.AddListener(TogglePanel);

            var arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(tabObj.transform, false);
            toggleText = arrowObj.AddComponent<TextMeshProUGUI>();
            var arrowRT = arrowObj.GetComponent<RectTransform>();
            arrowRT.anchorMin = Vector2.zero;
            arrowRT.anchorMax = Vector2.one;
            arrowRT.offsetMin = Vector2.zero;
            arrowRT.offsetMax = Vector2.zero;
            toggleText.text = "\u25C0"; // ◀
            toggleText.fontSize = 24;
            toggleText.alignment = TextAlignmentOptions.Center;
            toggleText.color = Color.white;

            // Start collapsed
            SetExpanded(false);
        }

        /// <summary>
        /// Add or update a word in the history panel.
        /// </summary>
        public void AddWord(string original, string translated, string langCode)
        {
            // Check for duplicate — move to bottom if exists
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].original.Equals(original, System.StringComparison.OrdinalIgnoreCase))
                {
                    var existing = entries[i];
                    existing.uiObject.transform.SetAsLastSibling();
                    var texts = existing.uiObject.GetComponentsInChildren<TextMeshProUGUI>();
                    if (texts.Length >= 1) texts[0].text = translated;
                    if (texts.Length >= 2) texts[1].text = original;
                    existing.translated = translated;
                    existing.langCode = langCode;
                    entries.RemoveAt(i);
                    entries.Add(existing);
                    return;
                }
            }

            // Remove oldest if at max capacity
            if (entries.Count >= maxEntries)
            {
                var oldest = entries[0];
                if (oldest.uiObject != null) Destroy(oldest.uiObject);
                entries.RemoveAt(0);
            }

            // Create new entry
            var entryObj = CreateEntryUI(original, translated, langCode);
            entries.Add(new WordEntry
            {
                original = original,
                translated = translated,
                langCode = langCode,
                uiObject = entryObj
            });

            // Auto-expand on first word
            if (entries.Count == 1 && !isExpanded)
            {
                SetExpanded(true);
            }
        }

        private GameObject CreateEntryUI(string original, string translated, string langCode)
        {
            var entryObj = new GameObject("Entry_" + original);
            entryObj.transform.SetParent(contentParent, false);

            var le = entryObj.AddComponent<LayoutElement>();
            le.preferredHeight = entryHeight;
            le.minHeight = entryHeight;

            var entryImg = entryObj.AddComponent<Image>();
            entryImg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

            // Tappable button
            var btn = entryObj.AddComponent<Button>();
            btn.targetGraphic = entryImg;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.3f, 0.6f, 0.9f);
            colors.pressedColor = new Color(0.15f, 0.25f, 0.5f, 0.9f);
            btn.colors = colors;

            string capturedOriginal = original;
            string capturedTranslated = translated;
            string capturedLang = langCode;
            btn.onClick.AddListener(() => OnEntryTapped(capturedOriginal, capturedTranslated, capturedLang));

            // Translated word — large, bold
            var transObj = new GameObject("Translated");
            transObj.transform.SetParent(entryObj.transform, false);
            var transText = transObj.AddComponent<TextMeshProUGUI>();
            transText.text = translated;
            transText.fontSize = 24;
            transText.fontStyle = FontStyles.Bold;
            transText.color = Color.white;
            transText.alignment = TextAlignmentOptions.Left;
            transText.overflowMode = TextOverflowModes.Ellipsis;
            var transRT = transObj.GetComponent<RectTransform>();
            transRT.anchorMin = new Vector2(0, 0.35f);
            transRT.anchorMax = new Vector2(1, 1);
            transRT.offsetMin = new Vector2(10, 0);
            transRT.offsetMax = new Vector2(-10, -4);

            // Original word — small, gray
            var origObj = new GameObject("Original");
            origObj.transform.SetParent(entryObj.transform, false);
            var origText = origObj.AddComponent<TextMeshProUGUI>();
            origText.text = original;
            origText.fontSize = 16;
            origText.color = new Color(0.6f, 0.65f, 0.7f, 0.8f);
            origText.alignment = TextAlignmentOptions.Left;
            origText.overflowMode = TextOverflowModes.Ellipsis;
            var origRT = origObj.GetComponent<RectTransform>();
            origRT.anchorMin = new Vector2(0, 0);
            origRT.anchorMax = new Vector2(1, 0.35f);
            origRT.offsetMin = new Vector2(10, 2);
            origRT.offsetMax = new Vector2(-10, 0);

            return entryObj;
        }

        private void OnEntryTapped(string original, string translated, string langCode)
        {
            // Play pronunciation
            AppManager.Instance?.SpeakWord(translated, langCode);
            Debug.Log($"[Nomina] History tap: {original} -> {translated}");
        }

        public void TogglePanel()
        {
            SetExpanded(!isExpanded);
        }

        private void SetExpanded(bool expanded)
        {
            isExpanded = expanded;

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(expanded);

            if (toggleText != null)
                toggleText.text = expanded ? "\u25B6" : "\u25C0"; // ▶ or ◀

            if (toggleButton != null)
            {
                var tabRT = toggleButton.GetComponent<RectTransform>();
                if (expanded)
                {
                    tabRT.offsetMin = new Vector2(-panelWidth - 36, 0);
                    tabRT.offsetMax = new Vector2(-panelWidth, 0);
                }
                else
                {
                    tabRT.offsetMin = new Vector2(-36, 0);
                    tabRT.offsetMax = new Vector2(0, 0);
                }
            }
        }

        public void ClearHistory()
        {
            foreach (var entry in entries)
            {
                if (entry.uiObject != null) Destroy(entry.uiObject);
            }
            entries.Clear();
        }
    }
}
