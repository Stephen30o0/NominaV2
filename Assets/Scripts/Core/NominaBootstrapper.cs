using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Nomina
{
    /// <summary>
    /// Auto-wires all Nomina component references at runtime.
    /// This eliminates the need to manually drag references in the Inspector.
    /// Runs in Awake before any Start methods.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before everything else
    public class NominaBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("[Nomina] Bootstrapper: Wiring component references...");

            // Find all managers
            var appManager = FindAnyObjectByType<AppManager>();
            var objectDetector = FindAnyObjectByType<ObjectDetector>();
            var translationManager = FindAnyObjectByType<TranslationManager>();
            var arLabelManager = FindAnyObjectByType<ARLabelManager>();
            var languageManager = FindAnyObjectByType<LanguageManager>();
            var vocabularyManager = FindAnyObjectByType<VocabularyManager>();
            var ttsManager = FindAnyObjectByType<TTSManager>();
            var uiManager = FindAnyObjectByType<UIManager>();
            var touchInputHandler = FindAnyObjectByType<TouchInputHandler>();

            // Find AR components
            var arCameraManager = FindAnyObjectByType<ARCameraManager>();
            var arRaycastManager = FindAnyObjectByType<ARRaycastManager>();
            var arAnchorManager = FindAnyObjectByType<ARAnchorManager>();

            // Disable AR plane visualization (the white dots) — we only need 
            // plane detection for raycasting, not the visual overlay
            var arPlaneManager = FindAnyObjectByType<ARPlaneManager>();
            if (arPlaneManager != null)
            {
                arPlaneManager.planePrefab = null; // Stop spawning plane visuals
                // Disable renderers on any existing planes
                foreach (var plane in FindObjectsByType<ARPlane>(FindObjectsSortMode.None))
                    HidePlaneVisuals(plane);
                // Subscribe to future plane additions
                arPlaneManager.trackablesChanged.AddListener((args) =>
                {
                    foreach (var plane in args.added)
                        HidePlaneVisuals(plane);
                });
                Debug.Log("[Nomina] Bootstrapper: AR plane visualization disabled");
            }

            // Wire AppManager references
            if (appManager != null)
            {
                SetField(appManager, "objectDetector", objectDetector);
                SetField(appManager, "translationManager", translationManager);
                SetField(appManager, "arLabelManager", arLabelManager);
                SetField(appManager, "languageManager", languageManager);
                SetField(appManager, "vocabularyManager", vocabularyManager);
                SetField(appManager, "ttsManager", ttsManager);
                SetField(appManager, "uiManager", uiManager);
                Debug.Log("[Nomina] Bootstrapper: AppManager wired ✓");
            }

            // Wire ObjectDetector -> AR Camera (Cloud Vision API handles detection)
            if (objectDetector != null)
            {
                if (arCameraManager != null)
                    SetField(objectDetector, "arCameraManager", arCameraManager);
                Debug.Log("[Nomina] Bootstrapper: ObjectDetector wired (Cloud Vision API)");
            }

            // Wire ARLabelManager -> AR Raycast & Anchor
            if (arLabelManager != null)
            {
                if (arRaycastManager != null)
                    SetField(arLabelManager, "arRaycastManager", arRaycastManager);
                if (arAnchorManager != null)
                    SetField(arLabelManager, "arAnchorManager", arAnchorManager);
                Debug.Log("[Nomina] Bootstrapper: ARLabelManager -> AR managers wired ✓");
            }

            // Wire TouchInputHandler -> Camera
            if (touchInputHandler != null)
            {
                var arCamera = arCameraManager?.GetComponent<Camera>();
                if (arCamera != null)
                    SetField(touchInputHandler, "arCamera", arCamera);
                Debug.Log("[Nomina] Bootstrapper: TouchInputHandler -> Camera wired ✓");
            }

            // UIManager panel references are wired by UIBuilder after UI is built.
            // Do NOT wire them here — UIBuilder.Awake runs after this and is authoritative.
            Debug.Log("[Nomina] Bootstrapper: UIManager panels deferred to UIBuilder");

            // Disable the AR template's default ObjectSpawner and SpawnTrigger
            // which place blue cubes on tap — we use our own label system instead.
            DisableTemplateSpawner();

            Debug.Log("[Nomina] Bootstrapper: All references wired successfully!");
        }

        private GameObject FindChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            return child != null ? child.gameObject : null;
        }

        private void HidePlaneVisuals(ARPlane plane)
        {
            if (plane == null) return;
            var mr = plane.GetComponent<MeshRenderer>();
            if (mr) mr.enabled = false;
            var lr = plane.GetComponent<LineRenderer>();
            if (lr) lr.enabled = false;
            foreach (var r in plane.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        private void DisableTemplateSpawner()
        {
            // The AR Mobile template includes an ObjectSpawner + ARInteractorSpawnTrigger
            // that spawns blue cubes on tap. Find and disable them.
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                string typeName = mb.GetType().Name;
                if (typeName == "ObjectSpawner" || typeName == "ARInteractorSpawnTrigger")
                {
                    mb.enabled = false;
                    Debug.Log($"[Nomina] Bootstrapper: Disabled {typeName}");
                }
            }
        }

        private void SetField(object target, string fieldName, object value)
        {
            if (target == null || value == null) return;
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[Nomina] Bootstrapper: Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }
    }
}