using System.Collections;
using UnityEngine;

namespace ToyPuzzle
{
    public sealed class ToyTween : MonoBehaviour
    {
        [SerializeField] private bool reducedMotion;

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
        }

        public Coroutine Pulse(RectTransform target, float scale = 1.12f, float duration = 0.26f)
        {
            if (target == null) return null;
            return StartCoroutine(PulseRoutine(target, reducedMotion ? 1.04f : scale, reducedMotion ? duration * 0.35f : duration));
        }

        public Coroutine Move(RectTransform target, Vector2 destination, float duration = 0.16f)
        {
            if (target == null) return null;
            return StartCoroutine(MoveRoutine(target, destination, reducedMotion ? duration * 0.35f : duration));
        }

        public Coroutine Rotate(RectTransform target, float degrees, float duration = 0.14f)
        {
            if (target == null) return null;
            return StartCoroutine(RotateRoutine(target, degrees, reducedMotion ? duration * 0.35f : duration));
        }

        public Coroutine Shake(RectTransform target, float distance = 12f, float duration = 0.18f)
        {
            if (target == null) return null;
            return StartCoroutine(ShakeRoutine(target, reducedMotion ? distance * 0.35f : distance, reducedMotion ? duration * 0.4f : duration));
        }

        public Coroutine WobbleRotation(RectTransform target, float degrees = 7f, float duration = 0.24f)
        {
            if (target == null) return null;
            return StartCoroutine(
                WobbleRotationRoutine(
                    target,
                    reducedMotion ? degrees * 0.35f : degrees,
                    reducedMotion ? duration * 0.45f : duration));
        }

        private static IEnumerator PulseRoutine(RectTransform target, float scale, float duration)
        {
            Vector3 original = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI);
                target.localScale = original * Mathf.Lerp(1f, scale, wave);
                yield return null;
            }
            if (target != null) target.localScale = original;
        }

        private static IEnumerator MoveRoutine(RectTransform target, Vector2 destination, float duration)
        {
            Vector2 start = target.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - (1f - t) * (1f - t);
                target.anchoredPosition = Vector2.LerpUnclamped(start, destination, t);
                yield return null;
            }
            if (target != null) target.anchoredPosition = destination;
        }

        private static IEnumerator RotateRoutine(RectTransform target, float degrees, float duration)
        {
            Quaternion start = target.localRotation;
            Quaternion end = Quaternion.Euler(0f, 0f, degrees);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localRotation = Quaternion.Lerp(start, end, 1f - (1f - t) * (1f - t));
                yield return null;
            }
            if (target != null) target.localRotation = end;
        }

        private static IEnumerator ShakeRoutine(RectTransform target, float distance, float duration)
        {
            Vector2 start = target.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float offset = Mathf.Sin(t * Mathf.PI * 5f) * distance * (1f - t);
                target.anchoredPosition = start + Vector2.right * offset;
                yield return null;
            }
            if (target != null) target.anchoredPosition = start;
        }

        private static IEnumerator WobbleRotationRoutine(RectTransform target, float degrees, float duration)
        {
            Quaternion start = target.localRotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float angle = Mathf.Sin(t * Mathf.PI * 5f) * degrees * (1f - t);
                target.localRotation = start * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            if (target != null) target.localRotation = start;
        }
    }
}
