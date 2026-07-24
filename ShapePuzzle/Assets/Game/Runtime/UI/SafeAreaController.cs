using UnityEngine;

namespace ToyPuzzle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaController : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            ApplySafeArea(true);
        }

        private void OnEnable()
        {
            ApplySafeArea(true);
        }

        private void Update()
        {
            Vector2Int size = new Vector2Int(Screen.width, Screen.height);
            Rect safeArea = Screen.safeArea;
            if (safeArea != _lastSafeArea || size != _lastScreenSize)
            {
                ApplySafeArea(false);
            }
        }

        private void ApplySafeArea(bool force)
        {
            if (_rectTransform == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safeArea = Screen.safeArea;
            Vector2Int size = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == _lastSafeArea && size == _lastScreenSize) return;

            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            _lastSafeArea = safeArea;
            _lastScreenSize = size;
        }
    }
}
