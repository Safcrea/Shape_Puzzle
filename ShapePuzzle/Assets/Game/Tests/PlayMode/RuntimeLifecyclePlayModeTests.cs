using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ToyPuzzle.Tests.PlayMode
{
    public sealed class RuntimeLifecyclePlayModeTests
    {
        [Test]
        public void AudioService_CanBeDisabledBeforeAwake()
        {
            var root = new GameObject("AudioServicePlayModeTest");
            root.SetActive(false);
            var service = root.AddComponent<AudioService>();
            try
            {
                Assert.DoesNotThrow(() => service.SetSoundEnabled(false),
                    "Serialized services may be configured by another component before their Awake order runs.");
                Assert.That(service.SoundEnabled, Is.False);

                root.SetActive(true);

                Assert.That(service.SoundEnabled, Is.False);
                Assert.That(root.GetComponents<AudioSource>().Length, Is.EqualTo(5));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RectangularBoardLayout_PreservesSquareCellsInsideAvailableEnvelope()
        {
            var root = new GameObject("ResponsiveLayoutPlayModeTest", typeof(RectTransform));
            root.SetActive(false);
            RectTransform layoutRoot = (RectTransform)root.transform;
            layoutRoot.sizeDelta = new Vector2(1080f, 1920f);
            var layout = root.AddComponent<ResponsiveGameLayout>();
            RectTransform top = CreateRect(root.transform, "Top");
            RectTransform board = CreateRect(root.transform, "Board");
            RectTransform controls = CreateRect(root.transform, "Controls");
            SetPrivateField(layout, "layoutRoot", layoutRoot);
            SetPrivateField(layout, "topZone", top);
            SetPrivateField(layout, "board", board);
            SetPrivateField(layout, "bottomControls", controls);

            try
            {
                root.SetActive(true);
                layout.SetBoardGridDimensions(5, 8);

                Assert.That(board.sizeDelta.x, Is.GreaterThan(0f));
                Assert.That(board.sizeDelta.y, Is.GreaterThan(0f));
                Assert.That(board.sizeDelta.x / 5f, Is.EqualTo(board.sizeDelta.y / 8f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShowingBaseScreen_ClosesPreviouslyVisiblePopup()
        {
            var root = new GameObject("ScreenManagerPlayModeTest");
            root.SetActive(false);
            var manager = root.AddComponent<ScreenManager>();
            CanvasGroup home = CreateScreen(root.transform, "Home");
            CanvasGroup levels = CreateScreen(root.transform, "Levels");
            CanvasGroup pause = CreateScreen(root.transform, "Pause");
            SetPrivateField(manager, "screens", new[]
            {
                new ScreenBinding { id = GameScreenId.Home, canvasGroup = home, popup = false },
                new ScreenBinding { id = GameScreenId.LevelSelect, canvasGroup = levels, popup = false },
                new ScreenBinding { id = GameScreenId.Pause, canvasGroup = pause, popup = true }
            });
            SetPrivateField(manager, "transitionDuration", 0f);

            try
            {
                root.SetActive(true);
                manager.Show(GameScreenId.Pause);
                Assert.That(pause.gameObject.activeSelf, Is.True);

                manager.Show(GameScreenId.LevelSelect);

                Assert.That(manager.BaseScreen, Is.EqualTo(GameScreenId.LevelSelect));
                Assert.That(levels.gameObject.activeSelf, Is.True);
                Assert.That(pause.gameObject.activeSelf, Is.False,
                    "A popup from the previous flow must not continue blocking a newly selected base screen.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static CanvasGroup CreateScreen(Transform parent, string name)
        {
            var screen = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            screen.transform.SetParent(parent, false);
            return screen.GetComponent<CanvasGroup>();
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }
    }
}
