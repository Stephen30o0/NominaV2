using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;

namespace Nomina
{
    /// <summary>
    /// Handles touch input for the AR view.
    /// - Tap on empty space: anchor current detection as a label
    /// - Tap on existing label: show detail popup (listen/save)
    /// - Long press on label: delete it
    /// </summary>
    public class TouchInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera arCamera;

        [Header("Settings")]
        [SerializeField] private float longPressThreshold = 0.8f;
        [SerializeField] private float tapMaxMovement = 30f; // pixels

        private float touchStartTime;
        private Vector2 touchStartPosition;
        private bool isTouching;
        private bool longPressTriggered;

        private void Update()
        {
            if (Input.touchCount == 0)
            {
                if (isTouching)
                {
                    OnTouchEnd();
                }
                isTouching = false;
                return;
            }

            Touch touch = Input.GetTouch(0);

            // Ignore touches on UI elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchStart(touch);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnTouchHeld(touch);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnTouchEnd();
                    break;
            }
        }

        private void OnTouchStart(Touch touch)
        {
            isTouching = true;
            longPressTriggered = false;
            touchStartTime = Time.time;
            touchStartPosition = touch.position;
        }

        private void OnTouchHeld(Touch touch)
        {
            if (!isTouching || longPressTriggered) return;

            // Check for long press
            if (Time.time - touchStartTime >= longPressThreshold)
            {
                longPressTriggered = true;
                HandleLongPress(touch.position);
            }
        }

        private void OnTouchEnd()
        {
            if (!isTouching) return;

            float duration = Time.time - touchStartTime;
            float distance = Vector2.Distance(touchStartPosition, Input.touchCount > 0 ? (Vector2)Input.GetTouch(0).position : touchStartPosition);

            // Only register as tap if short press and minimal movement
            if (!longPressTriggered && duration < longPressThreshold && distance < tapMaxMovement)
            {
                HandleTap(touchStartPosition);
            }

            isTouching = false;
        }

        private void HandleTap(Vector2 screenPosition)
        {
            // First check if we tapped an existing label
            if (arCamera == null) arCamera = Camera.main;

            Ray ray = arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var label = hit.collider.GetComponentInParent<ARLabel>();
                if (label != null)
                {
                    label.OnTap();
                    return;
                }
            }

            // No label hit — anchor the current detection at this position
            if (AppManager.Instance != null)
            {
                AppManager.Instance.AnchorCurrentDetection(screenPosition);
            }
        }

        private void HandleLongPress(Vector2 screenPosition)
        {
            if (arCamera == null) arCamera = Camera.main;

            Ray ray = arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var label = hit.collider.GetComponentInParent<ARLabel>();
                if (label != null)
                {
                    label.OnLongPress();
                    // Haptic feedback on Android
#if UNITY_ANDROID && !UNITY_EDITOR
                    Handheld.Vibrate();
#endif
                }
            }
        }
    }
}