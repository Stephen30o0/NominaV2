using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nomina
{
    /// <summary>
    /// Vocabulary list panel — displays saved words with filtering and sorting.
    /// Inspired by LingoLens's saved words UI.
    /// </summary>
    public class VocabularyPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject vocabItemPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private Button clearAllButton;

        [Header("Filters")]
        [SerializeField] private TMP_Dropdown languageFilter;
        [SerializeField] private TMP_Dropdown sortDropdown;

        [Header("Empty State")]
        [SerializeField] private GameObject emptyStatePanel;
        [SerializeField] private TextMeshProUGUI emptyStateText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI wordCountLabel;

        private List<GameObject> vocabItems = new List<GameObject>();
        private string filterLanguage = "all";
        private bool sortNewestFirst = true;

        private void OnEnable()
        {
            RefreshList();
            SetupFilters();
        }

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => AppManager.Instance?.UIManager?.HideVocabulary());

            if (clearAllButton != null)
                clearAllButton.onClick.AddListener(OnClearAll);

            if (languageFilter != null)
                languageFilter.onValueChanged.AddListener(OnLanguageFilterChanged);

            if (sortDropdown != null)
                sortDropdown.onValueChanged.AddListener(OnSortChanged);

            // Subscribe to vocabulary changes
            if (AppManager.Instance?.VocabularyManager != null)
            {
                AppManager.Instance.VocabularyManager.OnWordSaved += (entry) => RefreshList();
                AppManager.Instance.VocabularyManager.OnWordRemoved += (entry) => RefreshList();
                AppManager.Instance.VocabularyManager.OnVocabularyCleared += RefreshList;
            }
        }

        private void SetupFilters()
        {
            if (languageFilter != null)
            {
                languageFilter.ClearOptions();
                var options = new List<string> { "All Languages" };

                if (AppManager.Instance?.LanguageManager != null)
                {
                    foreach (var lang in AppManager.Instance.LanguageManager.SupportedLanguages)
                    {
                        options.Add(lang.displayName);
                    }
                }
                languageFilter.AddOptions(options);
            }

            if (sortDropdown != null)
            {
                sortDropdown.ClearOptions();
                sortDropdown.AddOptions(new List<string> { "Newest First", "Oldest First", "A-Z", "Z-A" });
            }
        }

        public void RefreshList()
        {
            // Clear existing
            foreach (var item in vocabItems)
            {
                if (item != null) Destroy(item);
            }
            vocabItems.Clear();

            if (AppManager.Instance?.VocabularyManager == null) return;

            var entries = GetFilteredEntries();

            // Update word count
            if (wordCountLabel != null)
                wordCountLabel.text = $"{entries.Count} word{(entries.Count == 1 ? "" : "s")}";

            // Show empty state if no entries
            if (emptyStatePanel != null)
                emptyStatePanel.SetActive(entries.Count == 0);

            if (entries.Count == 0)
            {
                if (emptyStateText != null)
                    emptyStateText.text = "No saved words yet.\nTap a label in AR view, then tap Save!";
                return;
            }

            // Create list items
            foreach (var entry in entries)
            {
                CreateVocabItem(entry);
            }

            // Force layout rebuild so items are visible immediately
            if (listContainer != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(listContainer.GetComponent<RectTransform>());
            }

            Debug.Log($"[Nomina] Vocabulary: Displaying {entries.Count} entries");
        }

        private List<VocabularyManager.VocabEntry> GetFilteredEntries()
        {
            var vocab = AppManager.Instance.VocabularyManager;
            List<VocabularyManager.VocabEntry> entries;

            if (filterLanguage == "all")
                entries = new List<VocabularyManager.VocabEntry>(vocab.Entries);
            else
                entries = vocab.GetEntriesByLanguage(filterLanguage);

            // Sort
            switch (sortDropdown?.value ?? 0)
            {
                case 0: // Newest first
                    entries.Sort((a, b) => string.Compare(b.dateAdded, a.dateAdded));
                    break;
                case 1: // Oldest first
                    entries.Sort((a, b) => string.Compare(a.dateAdded, b.dateAdded));
                    break;
                case 2: // A-Z
                    entries.Sort((a, b) => string.Compare(a.originalWord, b.originalWord));
                    break;
                case 3: // Z-A
                    entries.Sort((a, b) => string.Compare(b.originalWord, a.originalWord));
                    break;
            }

            return entries;
        }

        private void CreateVocabItem(VocabularyManager.VocabEntry entry)
        {
            GameObject item;
            if (vocabItemPrefab != null)
            {
                item = Instantiate(vocabItemPrefab, listContainer);
            }
            else
            {
                // Create a default item
                item = new GameObject($"VocabItem_{entry.id}");
                item.transform.SetParent(listContainer, false);

                var rt = item.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 80);

                // LayoutElement so the VerticalLayoutGroup gives us height
                var itemLE = item.AddComponent<LayoutElement>();
                itemLE.minHeight = 80;
                itemLE.preferredHeight = 80;

                var bg = item.AddComponent<Image>();
                bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

                var layout = item.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(16, 16, 8, 8);
                layout.spacing = 12;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;

                // Translation column
                var translationObj = new GameObject("Translation");
                translationObj.transform.SetParent(item.transform, false);
                var transText = translationObj.AddComponent<TextMeshProUGUI>();
                transText.text = $"<b>{entry.translatedWord}</b>\n<size=18>{entry.originalWord}</size>";
                transText.fontSize = 24;
                transText.color = Color.white;
                transText.overflowMode = TextOverflowModes.Ellipsis;
                var transLE = translationObj.AddComponent<LayoutElement>();
                transLE.flexibleWidth = 1;
                transLE.minWidth = 100;

                // Language badge
                var langObj = new GameObject("Language");
                langObj.transform.SetParent(item.transform, false);
                var langText = langObj.AddComponent<TextMeshProUGUI>();
                langText.text = entry.languageName;
                langText.fontSize = 16;
                langText.color = new Color(0.5f, 0.8f, 1f);
                langText.alignment = TextAlignmentOptions.MidlineRight;
                var langLE = langObj.AddComponent<LayoutElement>();
                langLE.preferredWidth = 80;

                // Listen button
                var listenObj = new GameObject("ListenBtn");
                listenObj.transform.SetParent(item.transform, false);
                var listenImg = listenObj.AddComponent<Image>();
                listenImg.color = new Color(0.3f, 0.7f, 1f, 0.8f);
                var listenBtn = listenObj.AddComponent<Button>();
                string capturedWord = entry.translatedWord;
                string capturedLang = entry.languageCode;
                listenBtn.onClick.AddListener(() =>
                    AppManager.Instance?.SpeakWord(capturedWord, capturedLang));
                var listenLE = listenObj.AddComponent<LayoutElement>();
                listenLE.preferredWidth = 50;
                listenLE.preferredHeight = 50;

                // Listen icon text
                var listenTextObj = new GameObject("Icon");
                listenTextObj.transform.SetParent(listenObj.transform, false);
                var listenText = listenTextObj.AddComponent<TextMeshProUGUI>();
                listenText.text = "\u25B6"; // Play triangle
                listenText.fontSize = 20;
                listenText.alignment = TextAlignmentOptions.Center;
                listenText.color = Color.white;
                var listenTextRT = listenTextObj.GetComponent<RectTransform>();
                listenTextRT.anchorMin = Vector2.zero;
                listenTextRT.anchorMax = Vector2.one;
                listenTextRT.offsetMin = Vector2.zero;
                listenTextRT.offsetMax = Vector2.zero;

                // Delete button
                var deleteObj = new GameObject("DeleteBtn");
                deleteObj.transform.SetParent(item.transform, false);
                var deleteImg = deleteObj.AddComponent<Image>();
                deleteImg.color = new Color(1f, 0.3f, 0.3f, 0.6f);
                var deleteBtn = deleteObj.AddComponent<Button>();
                string capturedId = entry.id;
                deleteBtn.onClick.AddListener(() =>
                    AppManager.Instance?.VocabularyManager?.RemoveWord(capturedId));
                var deleteLE = deleteObj.AddComponent<LayoutElement>();
                deleteLE.preferredWidth = 50;
                deleteLE.preferredHeight = 50;

                var deleteTextObj = new GameObject("Icon");
                deleteTextObj.transform.SetParent(deleteObj.transform, false);
                var deleteText = deleteTextObj.AddComponent<TextMeshProUGUI>();
                deleteText.text = "X"; // X mark
                deleteText.fontSize = 20;
                deleteText.alignment = TextAlignmentOptions.Center;
                deleteText.color = Color.white;
                var deleteTextRT = deleteTextObj.GetComponent<RectTransform>();
                deleteTextRT.anchorMin = Vector2.zero;
                deleteTextRT.anchorMax = Vector2.one;
                deleteTextRT.offsetMin = Vector2.zero;
                deleteTextRT.offsetMax = Vector2.zero;
            }

            vocabItems.Add(item);
        }

        private void OnLanguageFilterChanged(int index)
        {
            if (index == 0)
            {
                filterLanguage = "all";
            }
            else
            {
                var languages = AppManager.Instance?.LanguageManager?.SupportedLanguages;
                if (languages != null && index - 1 < languages.Count)
                {
                    filterLanguage = languages[index - 1].code;
                }
            }
            RefreshList();
        }

        private void OnSortChanged(int index)
        {
            RefreshList();
        }

        private void OnClearAll()
        {
            AppManager.Instance?.VocabularyManager?.ClearAll();
        }
    }
}