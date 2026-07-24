using UnityEngine;

namespace ToyPuzzle
{
    public sealed class MobilePresentationSettings : MonoBehaviour
    {
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }
}
