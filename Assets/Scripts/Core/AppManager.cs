using UnityEngine;

namespace Nomina
{
    /// <summary>
    /// Central app manager - singleton that coordinates all Nomina subsystems.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private ObjectDetector objectDetector;
        [SerializeField] private TranslationManager translationManager;
        [SerializeField] private ARLabelManager arLabelManager;
        [SerializeField] private LanguageManager languageManager;
        [SerializeField] private VocabularyManager vocabularyManager;
        [SerializeField] private TTSManager ttsManager;
        [SerializeField] private UIManager uiManager;

        public ObjectDetector ObjectDetector => objectDetector;
        public TranslationManager TranslationManager => translationManager;
        public ARLabelManager ARLabelManager => arLabelManager;
        public LanguageManager LanguageManager => languageManager;
        public VocabularyManager VocabularyManager => vocabularyManager;
        public TTSManager TTSManager => ttsManager;
        public UIManager UIManager => uiManager;

        private WordHistoryPanel wordHistoryPanel;

        [Header("State")]
        [SerializeField] private bool isDetecting = false;
        public bool IsDetecting => isDetecting;

        // Auto-translation tracking
        private string lastTranslatedWord = "";
        private string currentTranslation = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to detection events for auto-translation
            if (objectDetector != null)
            {
                objectDetector.OnObjectDetected += OnDetectionUpdated;
            }

            // Create word history panel
            var panelObj = new GameObject("WordHistoryPanel");
            wordHistoryPanel = panelObj.AddComponent<WordHistoryPanel>();
            wordHistoryPanel.Initialize();

            if (!PlayerPrefs.HasKey("OnboardingComplete"))
            {
                uiManager.ShowOnboarding();
            }
            else
            {
                uiManager.ShowMainView();
            }
        }

        private void OnDetectionUpdated(string label, float confidence)
        {
            // Empty label means nothing recognized
            if (string.IsNullOrEmpty(label))
            {
                uiManager?.UpdateDetectionDisplay("Not recognized", 0f);
                uiManager?.UpdateTranslationDisplay("Tap on an object to identify it");
                return;
            }

            // Update HUD with detected English word
            uiManager?.UpdateDetectionDisplay(label, confidence);

            // Auto-translate to current target language
            if (translationManager != null && languageManager != null)
            {
                lastTranslatedWord = label;
                currentTranslation = "";
                string targetLang = languageManager.CurrentLanguageCode;
                translationManager.Translate(label, "en", targetLang, (translation) =>
                {
                    if (translation != null && label == lastTranslatedWord)
                    {
                        currentTranslation = translation;
                        uiManager?.UpdateTranslationDisplay(translation);

                        // Place AR label at screen center
                        if (arLabelManager != null)
                        {
                            arLabelManager.AutoPlaceLabel(label, translation, targetLang);
                        }

                        // Add to word history panel
                        if (wordHistoryPanel != null)
                        {
                            wordHistoryPanel.AddWord(label, translation, targetLang);
                        }
                    }
                });
            }
        }

        public void StartDetection()
        {
            isDetecting = true;
            objectDetector.StartDetection();
            uiManager.SetDetectionMode(true);
            uiManager?.UpdateTranslationDisplay("Tap on an object to identify it");
        }

        public void StopDetection()
        {
            isDetecting = false;
            objectDetector.StopDetection();
            uiManager.SetDetectionMode(false);
        }

        public void ToggleDetection()
        {
            if (isDetecting) StopDetection();
            else StartDetection();
        }

        /// <summary>
        /// Called when user taps the screen to identify an object.
        /// Triggers a single Azure detection for whatever is at screen center.
        /// </summary>
        public void TapToDetect()
        {
            if (!isDetecting) return;
            if (objectDetector == null) return;
            
            objectDetector.DetectOnce();
        }

        /// <summary>
        /// Called when the user taps to anchor a detected object label in AR space.
        /// </summary>
        public void AnchorCurrentDetection(Vector2 screenPosition)
        {
            if (!isDetecting) return;

            string detectedObject = objectDetector.CurrentDetection;
            float confidence = objectDetector.CurrentConfidence;
            if (string.IsNullOrEmpty(detectedObject) || confidence < 0.3f) return;

            string targetLang = languageManager.CurrentLanguageCode;

            // Use cached translation if available for the current detection
            if (!string.IsNullOrEmpty(currentTranslation) && detectedObject == lastTranslatedWord)
            {
                arLabelManager.PlaceLabel(screenPosition, detectedObject, currentTranslation, targetLang);
            }
            else
            {
                translationManager.Translate(detectedObject, "en", targetLang, (translation) =>
                {
                    if (translation != null)
                    {
                        arLabelManager.PlaceLabel(screenPosition, detectedObject, translation, targetLang);
                    }
                });
            }
        }

        public void SaveWord(string originalWord, string translatedWord, string languageCode)
        {
            vocabularyManager.SaveWord(originalWord, translatedWord, languageCode);
            uiManager.ShowSaveConfirmation();
        }

        public void SpeakWord(string word, string languageCode)
        {
            ttsManager.Speak(word, languageCode);
        }

        public void CompleteOnboarding()
        {
            PlayerPrefs.SetInt("OnboardingComplete", 1);
            PlayerPrefs.Save();
            uiManager.ShowMainView();
        }
    }
}