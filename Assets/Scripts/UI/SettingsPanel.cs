using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nomina
{
    /// <summary>
    /// Settings panel — lets the user pick target language and adjust preferences.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform languageListContainer;
        [SerializeField] private GameObject languageItemPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI headerText;

        [Header("Settings Options")]
        [SerializeField] private Toggle debugModeToggle;
        [SerializeField] private Slider speechRateSlider;
        [SerializeField] private TextMeshProUGUI speechRateLabel;

        private List<GameObject> languageItems = new List<GameObject>();

        private void OnEnable()
        {
            // Guard: fields may not be wired yet during UIBuilder.Awake
            if (languageListContainer == null) return;
            PopulateLanguageList();
            LoadSettings();
        }

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => AppManager.Instance?.UIManager?.HideSettings());

            if (debugModeToggle != null)
                debugModeToggle.onValueChanged.AddListener(OnDebugModeChanged);

            if (speechRateSlider != null)
            {
                speechRateSlider.onValueChanged.AddListener(OnSpeechRateChanged);
                speechRateSlider.minValue = 0.5f;
                speechRateSlider.maxValue = 2.0f;
                speechRateSlider.value = PlayerPrefs.GetFloat("SpeechRate", 1.0f);
            }

            // Ensure language list is populated (OnEnable may have fired before wiring)
            if (languageListContainer != null && languageItems.Count == 0)
                PopulateLanguageList();
        }

        /// <summary>
        /// Called by UIManager.ShowSettings to force a refresh when we are certain
        /// AppManager and LanguageManager are initialized.
        /// </summary>
        public void ForceRefresh()
        {
            if (languageListContainer != null)
                PopulateLanguageList();
            LoadSettings();
        }

        private void PopulateLanguageList()
        {
            // Clear existing items
            foreach (var item in languageItems)
            {
                if (item != null) Destroy(item);
            }
            languageItems.Clear();

            if (AppManager.Instance?.LanguageManager == null) return;

            var languages = AppManager.Instance.LanguageManager.SupportedLanguages;
            string currentCode = AppManager.Instance.LanguageManager.CurrentLanguageCode;

            for (int i = 0; i < languages.Count; i++)
            {
                int index = i; // Capture for closure
                var lang = languages[i];
                bool isSelected = (lang.code == currentCode);

                GameObject item;
                if (languageItemPrefab != null)
                {
                    item = Instantiate(languageItemPrefab, languageListContainer);
                }
                else
                {
                    // Create a simple button with text
                    item = new GameObject($"Lang_{lang.code}");
                    item.transform.SetParent(languageListContainer, false);

                    var rt = item.AddComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(0, 70);

                    // LayoutElement so the VerticalLayoutGroup gives us height
                    var le = item.AddComponent<LayoutElement>();
                    le.minHeight = 70;
                    le.preferredHeight = 70;

                    var image = item.AddComponent<Image>();
                    image.color = isSelected
                        ? new Color(0.2f, 0.5f, 1f, 0.5f)
                        : new Color(0.18f, 0.18f, 0.24f, 1f);

                    var button = item.AddComponent<Button>();
                    button.targetGraphic = image;
                    button.onClick.AddListener(() => SelectLanguage(index));

                    // Add text
                    var textObj = new GameObject("Text");
                    textObj.transform.SetParent(item.transform, false);
                    var text = textObj.AddComponent<TextMeshProUGUI>();
                    text.text = isSelected
                        ? $"\u2713  {lang.displayName}  ({lang.nativeName})"
                        : $"    {lang.displayName}  ({lang.nativeName})";
                    text.fontSize = 24;
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.color = isSelected ? new Color(0.4f, 0.8f, 1f) : Color.white;

                    var textRT = textObj.GetComponent<RectTransform>();
                    textRT.anchorMin = Vector2.zero;
                    textRT.anchorMax = Vector2.one;
                    textRT.offsetMin = new Vector2(16, 0);
                    textRT.offsetMax = new Vector2(-16, 0);
                }

                languageItems.Add(item);
            }

            // Force layout rebuild so items are visible immediately
            if (languageListContainer != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(languageListContainer.GetComponent<RectTransform>());
            }

            Debug.Log($"[Nomina] Settings: Populated {languageItems.Count} languages");
        }

        private void SelectLanguage(int index)
        {
            AppManager.Instance?.LanguageManager?.SetLanguage(index);
            PopulateLanguageList(); // Refresh to show selection
        }

        private void OnDebugModeChanged(bool enabled)
        {
            PlayerPrefs.SetInt("DebugMode", enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (AppManager.Instance?.ObjectDetector != null)
            {
                // Toggle debug mode on object detector
                // The ObjectDetector's debugMode field would need to be exposed
            }
        }

        private void OnSpeechRateChanged(float rate)
        {
            PlayerPrefs.SetFloat("SpeechRate", rate);
            PlayerPrefs.Save();

            if (speechRateLabel != null)
                speechRateLabel.text = $"{rate:F1}x";

            AppManager.Instance?.TTSManager?.SetSpeechRate(rate);
        }

        private void LoadSettings()
        {
            if (debugModeToggle != null)
                debugModeToggle.isOn = PlayerPrefs.GetInt("DebugMode", 0) == 1;

            if (speechRateSlider != null)
                speechRateSlider.value = PlayerPrefs.GetFloat("SpeechRate", 1.0f);
        }
    }
}