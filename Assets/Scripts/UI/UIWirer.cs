using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nomina
{
    /// <summary>
    /// Helper that wires UIBuilder-created references into UIManager's serialized fields.
    /// Created dynamically by UIBuilder.
    /// </summary>
    public class UIWirer : MonoBehaviour
    {
        [HideInInspector] public UIManager uiManager;
        [HideInInspector] public GameObject mainView;
        [HideInInspector] public GameObject detectionOverlay;
        [HideInInspector] public GameObject detectionInfoPanel;
        [HideInInspector] public TextMeshProUGUI detectionLabel;
        [HideInInspector] public TextMeshProUGUI confidenceLabel;
        [HideInInspector] public TextMeshProUGUI currentLanguageLabel;
        [HideInInspector] public Button detectButton;
        [HideInInspector] public Button settingsButton;
        [HideInInspector] public Button vocabularyButton;
        [HideInInspector] public Button clearLabelsButton;

        private void Start()
        {
            if (uiManager == null) return;

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var type = uiManager.GetType();

            SetField(type, flags, "mainView", mainView);
            SetField(type, flags, "detectionOverlay", detectionOverlay);
            SetField(type, flags, "detectionInfoPanel", detectionInfoPanel);
            SetField(type, flags, "detectionLabel", detectionLabel);
            SetField(type, flags, "confidenceLabel", confidenceLabel);
            SetField(type, flags, "currentLanguageLabel", currentLanguageLabel);
            SetField(type, flags, "detectButton", detectButton);
            SetField(type, flags, "settingsButton", settingsButton);
            SetField(type, flags, "vocabularyButton", vocabularyButton);
            SetField(type, flags, "clearLabelsButton", clearLabelsButton);

            // Also set the onboarding and settings view references
            var canvas = uiManager.GetComponentInChildren<Canvas>()?.transform;
            if (canvas != null)
            {
                var onboardingView = canvas.Find("OnboardingView")?.gameObject;
                var settingsView = canvas.Find("SettingsView")?.gameObject;
                var vocabularyView = canvas.Find("VocabularyView")?.gameObject;

                SetField(type, flags, "onboardingView", onboardingView);
                SetField(type, flags, "settingsView", settingsView);
                SetField(type, flags, "vocabularyView", vocabularyView);
            }
        }

        private void SetField(System.Type type, System.Reflection.BindingFlags flags, string name, object value)
        {
            var field = type.GetField(name, flags);
            if (field != null && value != null)
            {
                field.SetValue(uiManager, value);
            }
        }
    }
}