using System;
using UnityEngine;
using UnityEngine.UI;

namespace ToyPuzzle
{
    public enum ToyEffectKind
    {
        Star,
        Sparkle,
        Confetti,
        HighlightRing
    }

    [Serializable]
    public sealed class ToyEffectSprite
    {
        public ToyEffectKind kind;
        public Sprite sprite;
    }

    public sealed class ToyEffectPool : MonoBehaviour
    {
        [SerializeField] private RectTransform effectRoot;
        [SerializeField] private ToyEffectSprite[] sprites = Array.Empty<ToyEffectSprite>();
        [SerializeField, Range(4, 32)] private int capacity = 18;
        [SerializeField] private bool reducedMotion;

        private RectTransform[] _rects;
        private Image[] _images;
        private float[] _ages;
        private float[] _lifetimes;
        private Vector2[] _velocities;
        private float[] _spin;

        private void Awake()
        {
            if (effectRoot == null) effectRoot = transform as RectTransform;
            capacity = Mathf.Clamp(capacity, 4, 32);
            _rects = new RectTransform[capacity];
            _images = new Image[capacity];
            _ages = new float[capacity];
            _lifetimes = new float[capacity];
            _velocities = new Vector2[capacity];
            _spin = new float[capacity];
            for (int i = 0; i < capacity; i++)
            {
                GameObject item = new GameObject("Effect_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                item.transform.SetParent(effectRoot, false);
                RectTransform rect = (RectTransform)item.transform;
                rect.sizeDelta = new Vector2(56f, 56f);
                Image image = item.GetComponent<Image>();
                image.raycastTarget = false;
                item.SetActive(false);
                _rects[i] = rect;
                _images[i] = image;
            }
        }

        private void Update()
        {
            if (_rects == null) return;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _rects.Length; i++)
            {
                if (!_rects[i].gameObject.activeSelf) continue;
                _ages[i] += dt;
                if (_ages[i] >= _lifetimes[i])
                {
                    _rects[i].gameObject.SetActive(false);
                    continue;
                }
                float t = _ages[i] / _lifetimes[i];
                _rects[i].anchoredPosition += _velocities[i] * dt;
                _rects[i].localRotation *= Quaternion.Euler(0f, 0f, _spin[i] * dt);
                float scale = 0.75f + Mathf.Sin(t * Mathf.PI) * 0.35f;
                _rects[i].localScale = Vector3.one * scale;
                Color color = _images[i].color;
                color.a = 1f - t * t;
                _images[i].color = color;
            }
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
        }

        public void Play(ToyEffectKind kind, Vector2 anchoredPosition, Color color)
        {
            if (_rects == null) return;
            int slot = FindAvailableSlot();
            if (slot < 0) return;
            Sprite sprite = FindSprite(kind);
            if (sprite == null) return;
            RectTransform rect = _rects[slot];
            Image image = _images[slot];
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one * 0.75f;
            rect.localRotation = Quaternion.identity;
            image.sprite = sprite;
            image.color = color;
            _ages[slot] = 0f;
            _lifetimes[slot] = reducedMotion ? 0.25f : (kind == ToyEffectKind.Confetti ? 0.85f : 0.55f);
            float direction = (slot & 1) == 0 ? -1f : 1f;
            _velocities[slot] = reducedMotion ? Vector2.zero : new Vector2(direction * (24f + slot * 2f), 70f + (slot % 4) * 18f);
            _spin[slot] = reducedMotion ? 0f : direction * (70f + slot * 7f);
            rect.gameObject.SetActive(true);
        }

        public void PlayCelebration(Vector2 center)
        {
            int count = reducedMotion ? 4 : Mathf.Min(capacity, 14);
            for (int i = 0; i < count; i++)
            {
                ToyEffectKind kind = i % 3 == 0 ? ToyEffectKind.Star : (i % 3 == 1 ? ToyEffectKind.Sparkle : ToyEffectKind.Confetti);
                float x = ((i * 83) % 280) - 140f;
                float y = ((i * 47) % 120) - 40f;
                Color color = i % 2 == 0 ? new Color32(255, 195, 25, 255) : new Color32(244, 240, 223, 255);
                Play(kind, center + new Vector2(x, y), color);
            }
        }

        public void Clear()
        {
            if (_rects == null) return;
            for (int i = 0; i < _rects.Length; i++) _rects[i].gameObject.SetActive(false);
        }

        private int FindAvailableSlot()
        {
            for (int i = 0; i < _rects.Length; i++)
            {
                if (!_rects[i].gameObject.activeSelf) return i;
            }
            return -1;
        }

        private Sprite FindSprite(ToyEffectKind kind)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                ToyEffectSprite entry = sprites[i];
                if (entry != null && entry.kind == kind) return entry.sprite;
            }
            return null;
        }
    }
}
