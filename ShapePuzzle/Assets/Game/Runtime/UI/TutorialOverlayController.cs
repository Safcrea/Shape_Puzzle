using System;
using UnityEngine;
using UnityEngine.UI;

namespace ToyPuzzle
{
    [DisallowMultipleComponent]
    public sealed class TutorialOverlayController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text message;
        [SerializeField] private RectTransform finger;
        [SerializeField] private Button skipButton;
        [SerializeField] private bool reducedMotion;

        private Vector2 _fingerOrigin;

        public event Action Skipped;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (finger != null) _fingerOrigin = finger.anchoredPosition;
            if (skipButton != null) skipButton.onClick.AddListener(HandleSkip);
        }

        private void OnDestroy()
        {
            if (skipButton != null) skipButton.onClick.RemoveListener(HandleSkip);
        }

        private void Update()
        {
            if (finger == null || reducedMotion || canvasGroup == null || canvasGroup.alpha <= 0f) return;
            float wave = Mathf.Sin(Time.unscaledTime * 4f) * 18f;
            finger.anchoredPosition = _fingerOrigin + new Vector2(wave, -Mathf.Abs(wave) * 0.35f);
        }

        public void Show(string text, bool showFinger)
        {
            gameObject.SetActive(true);
            if (message != null) message.text = text;
            if (finger != null)
            {
                finger.gameObject.SetActive(showFinger);
                finger.anchoredPosition = _fingerOrigin;
            }
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
            gameObject.SetActive(false);
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (value && finger != null) finger.anchoredPosition = _fingerOrigin;
        }

        private void HandleSkip()
        {
            Skipped?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
