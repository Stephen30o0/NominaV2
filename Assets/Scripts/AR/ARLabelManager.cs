using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

namespace Nomina
{
    /// <summary>
    /// Manages placing and displaying 3D translation labels in AR space.
    /// Labels are anchored to real-world surfaces using AR anchors.
    /// Auto-places labels when objects are detected; updates position on re-detection.
    /// </summary>
    public class ARLabelManager : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] private ARRaycastManager arRaycastManager;
        [SerializeField] private ARAnchorManager arAnchorManager;

        [Header("Label Prefab")]
        [SerializeField] private GameObject labelPrefab;

        [Header("Settings")]
        [SerializeField] private float labelScale = 0.02f;
        [SerializeField] private float labelYOffset = 0.1f;
        [SerializeField] private int maxLabels = 20;
        [SerializeField] private float labelRepositionDistance = 0.3f; // Re-anchor if moved further than this

        private List<ARLabel> activeLabels = new List<ARLabel>();
        private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

        // Track which detected word maps to which active label (for re-positioning)
        private Dictionary<string, ARLabel> labelsByWord = new Dictionary<string, ARLabel>();

        /// <summary>
        /// Auto-place or update a label for a detected object at screen center.
        /// Called automatically by AppManager on each detection event.
        /// </summary>
        public void AutoPlaceLabel(string originalWord, string translatedWord, string languageCode)
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

            if (!arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                return; // No surface at screen center — skip
            }

            Pose hitPose = hits[0].pose;
            Vector3 targetPosition = hitPose.position + Vector3.up * labelYOffset;

            // Check if we already have a label for this word
            string wordKey = originalWord.ToLower();
            if (labelsByWord.TryGetValue(wordKey, out ARLabel existingLabel) && existingLabel != null)
            {
                // Update position if the object has moved significantly
                float dist = Vector3.Distance(existingLabel.transform.position, targetPosition);
                if (dist > labelRepositionDistance)
                {
                    existingLabel.transform.position = targetPosition;
                    TryCreateAnchor(existingLabel.gameObject, hitPose);
                    Debug.Log($"[Nomina] Repositioned label: {originalWord} (moved {dist:F2}m)");
                }
                // Update translation text if it changed
                existingLabel.UpdateTranslation(translatedWord, languageCode);
                return;
            }

            // Remove oldest label if at max
            if (activeLabels.Count >= maxLabels)
            {
                RemoveLabel(activeLabels[0]);
            }

            // Create new label
            PlaceLabelAtPosition(targetPosition, hitPose, originalWord, translatedWord, languageCode);
        }

        /// <summary>
        /// Place a label at a specific screen position (tap-to-anchor).
        /// </summary>
        public void PlaceLabel(Vector2 screenPosition, string originalWord, string translatedWord, string languageCode)
        {
            if (arRaycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                Pose hitPose = hits[0].pose;
                Vector3 labelPosition = hitPose.position + Vector3.up * labelYOffset;

                // If already have a label for this word, re-anchor it
                string wordKey = originalWord.ToLower();
                if (labelsByWord.TryGetValue(wordKey, out ARLabel existingLabel) && existingLabel != null)
                {
                    existingLabel.transform.position = labelPosition;
                    TryCreateAnchor(existingLabel.gameObject, hitPose);
                    existingLabel.UpdateTranslation(translatedWord, languageCode);
                    Debug.Log($"[Nomina] Re-anchored label: {originalWord} -> {translatedWord}");
                    return;
                }

                // Remove oldest label if at max
                if (activeLabels.Count >= maxLabels)
                {
                    RemoveLabel(activeLabels[0]);
                }

                PlaceLabelAtPosition(labelPosition, hitPose, originalWord, translatedWord, languageCode);
            }
            else
            {
                Debug.LogWarning("[Nomina] AR Raycast did not hit any surface");
            }
        }

        private void PlaceLabelAtPosition(Vector3 position, Pose hitPose, string originalWord, string translatedWord, string languageCode)
        {
            GameObject labelObj;
            if (labelPrefab != null)
            {
                labelObj = Instantiate(labelPrefab, position, Quaternion.identity);
                labelObj.transform.localScale = Vector3.one * labelScale;
            }
            else
            {
                labelObj = CreateDefaultLabel(position);
            }

            // Ensure collider exists for tap interaction
            if (labelObj.GetComponent<Collider>() == null)
            {
                var box = labelObj.AddComponent<BoxCollider>();
                box.size = new Vector3(10f, 5f, 1f);
            }

            var arLabel = labelObj.GetComponent<ARLabel>();
            if (arLabel == null)
                arLabel = labelObj.AddComponent<ARLabel>();

            arLabel.Initialize(originalWord, translatedWord, languageCode);
            activeLabels.Add(arLabel);

            string wordKey = originalWord.ToLower();
            labelsByWord[wordKey] = arLabel;

            // Create an AR anchor to keep label locked to surface
            TryCreateAnchor(labelObj, hitPose);

            Debug.Log($"[Nomina] Placed label: {originalWord} -> {translatedWord} ({languageCode})");
        }

        /// <summary>
        /// Create an AR anchor at the hit pose and parent the label to it.
        /// </summary>
        private async void TryCreateAnchor(GameObject labelObj, Pose hitPose)
        {
            if (arAnchorManager == null) return;

            // Remove old anchor parent if any
            var oldAnchor = labelObj.GetComponentInParent<ARAnchor>();
            if (oldAnchor != null)
            {
                labelObj.transform.SetParent(null, true);
                Destroy(oldAnchor.gameObject);
            }

            try
            {
                var result = await arAnchorManager.TryAddAnchorAsync(hitPose);
                if (result.status.IsSuccess() && labelObj != null)
                {
                    var anchor = result.value;
                    Vector3 worldPos = labelObj.transform.position;
                    Quaternion worldRot = labelObj.transform.rotation;
                    labelObj.transform.SetParent(anchor.transform, false);
                    labelObj.transform.position = worldPos;
                    labelObj.transform.rotation = worldRot;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Nomina] Failed to create AR anchor: {e.Message}");
            }
        }

        /// <summary>
        /// Remove a specific label.
        /// </summary>
        public void RemoveLabel(ARLabel label)
        {
            if (label == null) return;
            activeLabels.Remove(label);

            // Clean up word tracking
            string wordKey = label.OriginalWord?.ToLower();
            if (!string.IsNullOrEmpty(wordKey) && labelsByWord.ContainsKey(wordKey))
            {
                labelsByWord.Remove(wordKey);
            }

            // Destroy anchor parent if any
            var anchor = label.GetComponentInParent<ARAnchor>();
            if (anchor != null)
            {
                Destroy(anchor.gameObject); // Destroys anchor + label
            }
            else
            {
                Destroy(label.gameObject);
            }
        }

        /// <summary>
        /// Remove all labels from the scene.
        /// </summary>
        public void ClearAllLabels()
        {
            foreach (var label in activeLabels)
            {
                if (label != null)
                {
                    var anchor = label.GetComponentInParent<ARAnchor>();
                    if (anchor != null)
                        Destroy(anchor.gameObject);
                    else
                        Destroy(label.gameObject);
                }
            }
            activeLabels.Clear();
            labelsByWord.Clear();
        }

        public List<ARLabel> GetActiveLabels()
        {
            return new List<ARLabel>(activeLabels);
        }

        /// <summary>
        /// Check if a label already exists for the given word.
        /// </summary>
        public bool HasLabelForWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            string key = word.ToLower();
            return labelsByWord.ContainsKey(key) && labelsByWord[key] != null;
        }

        private GameObject CreateDefaultLabel(Vector3 position)
        {
            GameObject labelRoot = new GameObject("ARLabel");
            labelRoot.transform.position = position;

            // World-space Canvas (uses Unity built-in UI shader — works on all platforms)
            var canvas = labelRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasRT = labelRoot.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(400, 160);
            // 1 canvas unit = 0.5mm in world → label is 20cm x 8cm
            canvasRT.localScale = Vector3.one * 0.0005f;

            // Background panel
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(labelRoot.transform, false);
            var bgRT = bgObj.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

            // BoxCollider for Physics.Raycast tap interaction
            // Size in canvas local units (400x160 canvas at 0.0005 scale = 0.2m x 0.08m world)
            var boxCollider = labelRoot.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(400f, 160f, 20f);
            boxCollider.center = Vector3.zero;

            // Translation text (top, large, white)
            var transObj = new GameObject("TranslationText");
            transObj.transform.SetParent(labelRoot.transform, false);
            var translationText = transObj.AddComponent<TextMeshProUGUI>();
            translationText.fontSize = 56;
            translationText.alignment = TextAlignmentOptions.Center;
            translationText.color = Color.white;
            translationText.fontStyle = FontStyles.Bold;
            var transRT = transObj.GetComponent<RectTransform>();
            transRT.anchorMin = new Vector2(0, 0.35f);
            transRT.anchorMax = new Vector2(1, 1);
            transRT.offsetMin = new Vector2(10, 0);
            transRT.offsetMax = new Vector2(-10, -5);

            // Original word (bottom, smaller, gray)
            var origObj = new GameObject("OriginalText");
            origObj.transform.SetParent(labelRoot.transform, false);
            var originalText = origObj.AddComponent<TextMeshProUGUI>();
            originalText.fontSize = 30;
            originalText.alignment = TextAlignmentOptions.Center;
            originalText.color = new Color(0.7f, 0.75f, 0.8f, 0.9f);
            var origRT = origObj.GetComponent<RectTransform>();
            origRT.anchorMin = new Vector2(0, 0);
            origRT.anchorMax = new Vector2(1, 0.35f);
            origRT.offsetMin = new Vector2(10, 5);
            origRT.offsetMax = new Vector2(-10, 0);

            return labelRoot;
        }
    }
}