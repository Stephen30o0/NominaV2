using UnityEngine;
using UnityEngine.UI;

namespace Nomina
{
    /// <summary>
    /// Animated detection crosshair/reticle that shows in the center of the screen
    /// when detection mode is active. Pulses to indicate scanning.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DetectionReticle : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Image[] cornerBrackets;  // 4 corner bracket images
        [SerializeField] private Image centerDot;
        [SerializeField] private Image scanLine;           // Animated scan line

        [Header("Colors")]
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color detectedColor = new Color(0.2f, 1f, 0.4f, 0.9f);
        [SerializeField] private Color noDetectionColor = new Color(1f, 0.4f, 0.2f, 0.6f);

        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.05f;
        [SerializeField] private float scanSpeed = 1.5f;

        private RectTransform rectTransform;
        private Vector2 baseSize;
        private bool hasDetection = false;
        private float scanProgress = 0f;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            baseSize = rectTransform.sizeDelta;
        }

        private void OnEnable()
        {
            // Subscribe to detection events
            if (AppManager.Instance?.ObjectDetector != null)
            {
                AppManager.Instance.ObjectDetector.OnObjectDetected += OnDetectionUpdate;
            }
        }

        private void OnDisable()
        {
            if (AppManager.Instance?.ObjectDetector != null)
            {
                AppManager.Instance.ObjectDetector.OnObjectDetected -= OnDetectionUpdate;
            }
        }

        private void Update()
        {
            // Pulse animation
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            rectTransform.sizeDelta = baseSize * pulse;

            // Scan line animation
            if (scanLine != null)
            {
                scanProgress += Time.deltaTime * scanSpeed;
                if (scanProgress > 1f) scanProgress = 0f;

                var scanRT = scanLine.rectTransform;
                float y = Mathf.Lerp(-baseSize.y * 0.5f, baseSize.y * 0.5f, scanProgress);
                scanRT.anchoredPosition = new Vector2(0, y);

                Color scanColor = scanLine.color;
                scanColor.a = Mathf.Sin(scanProgress * Mathf.PI) * 0.5f;
                scanLine.color = scanColor;
            }

            // Update color based on detection state
            Color targetColor = hasDetection ? detectedColor : idleColor;

            if (cornerBrackets != null)
            {
                foreach (var bracket in cornerBrackets)
                {
                    if (bracket != null)
                        bracket.color = Color.Lerp(bracket.color, targetColor, Time.deltaTime * 5f);
                }
            }

            if (centerDot != null)
                centerDot.color = Color.Lerp(centerDot.color, targetColor, Time.deltaTime * 5f);
        }

        private void OnDetectionUpdate(string label, float confidence)
        {
            hasDetection = !string.IsNullOrEmpty(label) && confidence > 0.3f;
        }
    }
}