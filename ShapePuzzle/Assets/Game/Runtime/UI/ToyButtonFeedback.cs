using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToyPuzzle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class ToyButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private float pressedScale = 0.93f;
        [SerializeField] private float duration = 0.08f;
        private Coroutine _animation;

        private void Awake()
        {
            if (visual == null) visual = transform as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Selectable selectable = GetComponent<Selectable>();
            if (selectable != null && selectable.IsInteractable()) AnimateTo(pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(1f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateTo(1f);
        }

        private void OnDisable()
        {
            if (visual != null) visual.localScale = Vector3.one;
        }

        private void AnimateTo(float target)
        {
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(ScaleRoutine(target));
        }

        private IEnumerator ScaleRoutine(float target)
        {
            if (visual == null) yield break;
            Vector3 start = visual.localScale;
            Vector3 end = new Vector3(target, target, 1f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                visual.localScale = Vector3.LerpUnclamped(start, end, 1f - (1f - t) * (1f - t));
                yield return null;
            }
            visual.localScale = end;
            _animation = null;
        }
    }
}
