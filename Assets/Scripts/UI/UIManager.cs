using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Nomina
{
    /// <summary>
    /// Central UI Manager that controls all UI panels and state transitions.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screen Panels")]
        [SerializeField] private GameObject mainView;
        [SerializeField] private GameObject onboardingView;
        [SerializeField] private GameObject settingsView;
        [SerializeField] private GameObject vocabularyView;
        [SerializeField] private GameObject labelDetailView;

        [Header("HUD Elements")]
        [SerializeField] private GameObject detectionOverlay;   // Crosshair / detection box
        [SerializeField] private GameObject detectionInfoPanel; // Shows current detection label
        [SerializeField] private TMPro.TextMeshProUGUI detectionLabel;
        [SerializeField] private TMPro.TextMeshProUGUI confidenceLabel;
        [SerializeField] private TMPro.TextMeshProUGUI translationLabel;
        [SerializeField] private TMPro.TextMeshProUGUI currentLanguageLabel;
        [SerializeField] private TMPro.TextMeshProUGUI scanningIndicator;

        private float scanAnimTimer;
        private int scanDotCount;
        private bool isScanning;

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button detectButton;
        [SerializeField] private UnityEngine.UI.Button settingsButton;
        [SerializeField] private UnityEngine.UI.Button vocabularyButton;
        [SerializeField] private UnityEngine.UI.Button clearLabelsButton;

        [Header("Label Detail")]
        [SerializeField] private TMPro.TextMeshProUGUI detailOriginalText;
        [SerializeField] private TMPro.TextMeshProUGUI detailTranslatedText;
        [SerializeField] private TMPro.TextMeshProUGUI detailLanguageText;
        [SerializeField] private UnityEngine.UI.Button detailListenButton;
        [SerializeField] private UnityEngine.UI.Button detailSaveButton;
        [SerializeField] private UnityEngine.UI.Button detailCloseButton;
        [SerializeField] private GameObject detailSavedIndicator;

        [Header("Save Confirmation")]
        [SerializeField] private GameObject saveConfirmationPopup;
        [SerializeField] private float confirmationDisplayTime = 1.5f;

        private ARLabel currentDetailLabel;

        private void Start()
        {
            // Enable Enhanced Touch support for new Input System
            EnhancedTouchSupport.Enable();

            // Wire up button listeners
            if (detectButton != null) detectButton.onClick.AddListener(OnDetectButtonClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            if (vocabularyButton != null) vocabularyButton.onClick.AddListener(OnVocabularyButtonClicked);
            if (clearLabelsButton != null) clearLabelsButton.onClick.AddListener(OnClearLabelsClicked);
            if (detailListenButton != null) detailListenButton.onClick.AddListener(OnDetailListenClicked);
            if (detailSaveButton != null) detailSaveButton.onClick.AddListener(OnDetailSaveClicked);
            if (detailCloseButton != null) detailCloseButton.onClick.AddListener(OnDetailCloseClicked);

            // Detection events are handled by AppManager (which calls UpdateDetectionDisplay)

            if (AppManager.Instance?.LanguageManager != null)
            {
                AppManager.Instance.LanguageManager.OnLanguageChanged += (lang) =>
                {
                    if (currentLanguageLabel != null)
                        currentLanguageLabel.text = lang.displayName;
                };
            }

            // Hide all overlays initially
            SetActive(labelDetailView, false);
            SetActive(saveConfirmationPopup, false);
            SetDetectionMode(false);
        }

        public void ShowMainView()
        {
            SetActive(mainView, true);
            SetActive(onboardingView, false);
            SetActive(settingsView, false);
            SetActive(vocabularyView, false);

            if (currentLanguageLabel != null && AppManager.Instance?.LanguageManager != null)
            {
                currentLanguageLabel.text = AppManager.Instance.LanguageManager.CurrentLanguageName;
            }
        }

        public void ShowOnboarding()
        {
            SetActive(onboardingView, true);
            SetActive(mainView, false);
            SetActive(settingsView, false);
            SetActive(vocabularyView, false);
        }

        public void ShowSettings()
        {
            SetActive(settingsView, true);
            SetActive(mainView, false);

            // Force the SettingsPanel to repopulate now that we know
            // AppManager and LanguageManager are fully initialized
            if (settingsView != null)
            {
                var sp = settingsView.GetComponent<Nomina.SettingsPanel>();
                if (sp != null)
                    sp.ForceRefresh();
            }
        }

        public void HideSettings()
        {
            SetActive(settingsView, false);
            SetActive(mainView, true);
        }

        public void ShowVocabulary()
        {
            SetActive(vocabularyView, true);
            SetActive(mainView, false);
        }

        public void HideVocabulary()
        {
            SetActive(vocabularyView, false);
            SetActive(mainView, true);
        }

        public void SetDetectionMode(bool active)
        {
            SetActive(detectionOverlay, active);
            SetActive(detectionInfoPanel, active);

            if (!active)
            {
                if (detectionLabel != null) detectionLabel.text = "";
                if (confidenceLabel != null) confidenceLabel.text = "";
            }
        }

        public void UpdateDetectionDisplay(string label, float confidence)
        {
            if (detectionLabel != null)
                detectionLabel.text = label;
            if (confidenceLabel != null)
                confidenceLabel.text = $"{(confidence * 100):F0}%";
        }

        public void UpdateTranslationDisplay(string translation)
        {
            if (translationLabel != null)
                translationLabel.text = translation;
        }

        /// <summary>
        /// Show animated scanning indicator while waiting for backend response.
        /// </summary>
        public void SetScanningState(bool scanning)
        {
            isScanning = scanning;
            if (scanningIndicator != null)
            {
                scanningIndicator.gameObject.SetActive(scanning);
                if (!scanning)
                    scanningIndicator.text = "";
            }
        }

        private void Update()
        {
            // Animate scanning indicator
            if (isScanning && scanningIndicator != null)
            {
                scanAnimTimer += Time.deltaTime;
                if (scanAnimTimer >= 0.4f)
                {
                    scanAnimTimer = 0;
                    scanDotCount = (scanDotCount + 1) % 4;
                    scanningIndicator.text = "Analyzing" + new string('.', scanDotCount);
                }
            }

            // Auto-update scanning state from ObjectDetector
            if (AppManager.Instance?.ObjectDetector != null && AppManager.Instance.IsDetecting)
            {
                bool currentlyScanning = AppManager.Instance.ObjectDetector.IsScanning;
                if (currentlyScanning != isScanning)
                {
                    SetScanningState(currentlyScanning);
                }
            }

            // Tap handling: check for AR label tap first, then trigger detection
            if (AppManager.Instance != null && AppManager.Instance.IsDetecting)
            {
                var activeTouches = Touch.activeTouches;
                if (activeTouches.Count == 1)
                {
                    var touch = activeTouches[0];
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && !IsPointerOverUI(touch.screenPosition))
                    {
                        // Check if tapping an AR label first
                        Camera cam = Camera.main;
                        if (cam != null)
                        {
                            Ray ray = cam.ScreenPointToRay(new Vector3(touch.screenPosition.x, touch.screenPosition.y, 0));
                            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                            {
                                var label = hit.collider.GetComponentInParent<ARLabel>();
                                if (label != null)
                                {
                                    label.OnTap();
                                    return; // Tapped a label — don't trigger new detection
                                }
                            }
                        }
                        // No label hit — trigger new detection
                        AppManager.Instance.TapToDetect();
                    }
                }
            }
        }

        /// <summary>
        /// Check if a screen position is over a UI element (button, panel, etc.)
        /// </summary>
        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return false;

            var pointerData = new UnityEngine.EventSystems.PointerEventData(eventSystem)
            {
                position = screenPosition
            };
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);
            return results.Count > 0;
        }

        public void ShowLabelDetail(ARLabel label)
        {
            currentDetailLabel = label;
            SetActive(labelDetailView, true);

            if (detailOriginalText != null) detailOriginalText.text = label.OriginalWord;
            if (detailTranslatedText != null) detailTranslatedText.text = label.TranslatedWord;
            if (detailLanguageText != null) detailLanguageText.text = label.LanguageCode;
            if (detailSavedIndicator != null) detailSavedIndicator.SetActive(label.IsSaved);
            if (detailSaveButton != null) detailSaveButton.interactable = !label.IsSaved;
        }

        public void HideLabelDetail()
        {
            SetActive(labelDetailView, false);
            currentDetailLabel = null;
        }

        public void ShowSaveConfirmation()
        {
            if (saveConfirmationPopup != null)
            {
                saveConfirmationPopup.SetActive(true);
                CancelInvoke(nameof(HideSaveConfirmation));
                Invoke(nameof(HideSaveConfirmation), confirmationDisplayTime);
            }
        }

        private void HideSaveConfirmation()
        {
            SetActive(saveConfirmationPopup, false);
        }

        // Button handlers
        private void OnDetectButtonClicked()
        {
            AppManager.Instance?.ToggleDetection();
        }

        private void OnSettingsButtonClicked()
        {
            ShowSettings();
        }

        private void OnVocabularyButtonClicked()
        {
            ShowVocabulary();
        }

        private void OnClearLabelsClicked()
        {
            AppManager.Instance?.ARLabelManager?.ClearAllLabels();
        }

        private void OnDetailListenClicked()
        {
            currentDetailLabel?.PlayPronunciation();
        }

        private void OnDetailSaveClicked()
        {
            currentDetailLabel?.SaveToVocabulary();
            if (detailSavedIndicator != null) detailSavedIndicator.SetActive(true);
            if (detailSaveButton != null) detailSaveButton.interactable = false;
        }

        private void OnDetailCloseClicked()
        {
            HideLabelDetail();
        }

        private void SetActive(GameObject obj, bool active)
        {
            if (obj != null) obj.SetActive(active);
        }
    }
}