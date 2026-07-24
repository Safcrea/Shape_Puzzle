using System;
using System.Collections;
using UnityEngine;

namespace ToyPuzzle
{
    public enum GameScreenId
    {
        Home,
        Gameplay,
        LevelSelect,
        Settings,
        Pause,
        Completion,
        ResetConfirmation,
        ProgressResetConfirmation
    }

    [Serializable]
    public sealed class ScreenBinding
    {
        public GameScreenId id;
        public CanvasGroup canvasGroup;
        public bool popup;
    }

    public sealed class ScreenManager : MonoBehaviour
    {
        [SerializeField] private ScreenBinding[] screens = Array.Empty<ScreenBinding>();
        [SerializeField] private float transitionDuration = 0.16f;
        [SerializeField] private bool reducedMotion;

        private GameScreenId _baseScreen = GameScreenId.Home;
        private Coroutine _transition;

        public GameScreenId BaseScreen => _baseScreen;

        private void Awake()
        {
            ShowImmediate(GameScreenId.Home, false);
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
        }

        public void Show(GameScreenId id)
        {
            ScreenBinding target = Find(id);
            if (target == null || target.canvasGroup == null) return;
            if (!target.popup) _baseScreen = id;
            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(TransitionTo(target));
        }

        public void ClosePopup(GameScreenId id)
        {
            ScreenBinding binding = Find(id);
            if (binding == null || binding.canvasGroup == null || !binding.popup) return;
            SetVisible(binding.canvasGroup, false);
        }

        public void ShowImmediate(GameScreenId id, bool preservePopups)
        {
            ScreenBinding target = Find(id);
            if (target == null) return;
            if (!target.popup) _baseScreen = id;
            for (int i = 0; i < screens.Length; i++)
            {
                ScreenBinding binding = screens[i];
                if (binding == null || binding.canvasGroup == null) continue;
                if (binding.popup && preservePopups && binding.id != id) continue;
                bool visible = binding.id == id || (binding.popup && binding.canvasGroup.alpha > 0.99f && target.popup);
                SetVisible(binding.canvasGroup, visible);
            }
        }

        private IEnumerator TransitionTo(ScreenBinding target)
        {
            if (!target.popup)
            {
                for (int i = 0; i < screens.Length; i++)
                {
                    ScreenBinding binding = screens[i];
                    if (binding != null && binding.canvasGroup != null && binding != target)
                    {
                        SetVisible(binding.canvasGroup, false);
                    }
                }
            }

            CanvasGroup group = target.canvasGroup;
            if (target.popup) group.transform.SetAsLastSibling();
            group.gameObject.SetActive(true);
            group.interactable = false;
            group.blocksRaycasts = true;
            float duration = reducedMotion ? 0.05f : transitionDuration;
            float start = group.alpha;
            float elapsed = 0f;
            RectTransform rect = group.transform as RectTransform;
            Vector3 initialScale = reducedMotion ? Vector3.one : new Vector3(0.96f, 0.96f, 1f);
            if (rect != null) rect.localScale = initialScale;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                group.alpha = Mathf.Lerp(start, 1f, t);
                if (rect != null) rect.localScale = Vector3.LerpUnclamped(initialScale, Vector3.one, t);
                yield return null;
            }
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            if (rect != null) rect.localScale = Vector3.one;
            _transition = null;
        }

        private ScreenBinding Find(GameScreenId id)
        {
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] != null && screens[i].id == id) return screens[i];
            }
            return null;
        }

        private static void SetVisible(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            group.gameObject.SetActive(visible);
        }
    }
}
