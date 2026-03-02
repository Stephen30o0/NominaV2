using UnityEngine;
using TMPro;

namespace Nomina
{
    /// <summary>
    /// Represents a single AR label anchored in 3D space.
    /// Displays the original word, translation, and provides interaction (tap to hear, long press to delete).
    /// Always faces the camera (billboard).
    /// </summary>
    public class ARLabel : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string originalWord;
        [SerializeField] private string translatedWord;
        [SerializeField] private string languageCode;
        [SerializeField] private bool isSaved = false;

        [Header("UI References (auto-found if null)")]
        [SerializeField] private TextMeshProUGUI translationText;
        [SerializeField] private TextMeshProUGUI originalText;

        public string OriginalWord => originalWord;
        public string TranslatedWord => translatedWord;
        public string LanguageCode => languageCode;
        public bool IsSaved => isSaved;

        private Camera mainCamera;
        private float tapStartTime;
        private bool isTapping;
        private const float longPressDuration = 0.8f;

        public void Initialize(string original, string translated, string langCode)
        {
            originalWord = original;
            translatedWord = translated;
            languageCode = langCode;

            // Try to find text components in children
            if (translationText == null || originalText == null)
            {
                var texts = GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    translationText = texts[0];
                    originalText = texts[1];
                }
                else if (texts.Length == 1)
                {
                    translationText = texts[0];
                }
            }

            UpdateDisplay();
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            // Billboard: face the camera (canvas front = camera forward)
            if (mainCamera != null)
            {
                // World-space Canvas text is visible when looking along +Z.
                // So set the label's forward = camera's forward to face the viewer.
                Vector3 camForward = mainCamera.transform.forward;
                camForward.y = 0; // Keep upright
                if (camForward.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(camForward.normalized, Vector3.up);
                }
            }
        }

        private void UpdateDisplay()
        {
            if (translationText != null)
                translationText.text = translatedWord;
            if (originalText != null)
                originalText.text = originalWord;
        }

        /// <summary>
        /// Update the translation and language on an existing label (for re-detection).
        /// </summary>
        public void UpdateTranslation(string newTranslation, string newLangCode)
        {
            if (newTranslation == translatedWord && newLangCode == languageCode) return;
            translatedWord = newTranslation;
            languageCode = newLangCode;
            UpdateDisplay();
        }

        /// <summary>
        /// Called when the label is tapped — shows detail popup with listen/save options.
        /// </summary>
        public void OnTap()
        {
            // Play pronunciation immediately on tap
            PlayPronunciation();

            // Also show detail panel if available
            if (AppManager.Instance?.UIManager != null)
            {
                AppManager.Instance.UIManager.ShowLabelDetail(this);
            }
        }

        /// <summary>
        /// Called on long press — deletes this label.
        /// </summary>
        public void OnLongPress()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.ARLabelManager.RemoveLabel(this);
            }
        }

        public void MarkAsSaved()
        {
            isSaved = true;
            // Could update visual indicator here (e.g. green checkmark)
        }

        public void PlayPronunciation()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.SpeakWord(translatedWord, languageCode);
            }
        }

        public void SaveToVocabulary()
        {
            if (AppManager.Instance != null && !isSaved)
            {
                AppManager.Instance.SaveWord(originalWord, translatedWord, languageCode);
                MarkAsSaved();
            }
        }
    }
}