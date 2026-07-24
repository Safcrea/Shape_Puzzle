using System;
using UnityEngine;
using UnityEngine.UI;

namespace ToyPuzzle
{
    public sealed class LevelEntryView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text levelNumber;
        [SerializeField] private Text bestMoves;
        [SerializeField] private Image thumbnail;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Image completedMark;
        [SerializeField] private GameObject difficultyOne;
        [SerializeField] private GameObject difficultyTwo;
        [SerializeField] private GameObject difficultyThree;

        private int _levelIndex;
        private Action<int> _selected;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Configure(int levelIndex, Sprite targetThumbnail, bool unlocked, bool completed, int bestMoveCount, int difficulty, Action<int> selected)
        {
            _levelIndex = levelIndex;
            _selected = selected;
            if (levelNumber != null) levelNumber.text = (levelIndex + 1).ToString();
            if (bestMoves != null) bestMoves.text = bestMoveCount > 0 ? "BEST " + bestMoveCount : "—";
            if (thumbnail != null)
            {
                thumbnail.sprite = targetThumbnail;
                thumbnail.enabled = targetThumbnail != null;
            }
            if (lockIcon != null) lockIcon.gameObject.SetActive(!unlocked);
            if (completedMark != null) completedMark.gameObject.SetActive(completed);
            if (button != null) button.interactable = unlocked;
            if (difficultyOne != null) difficultyOne.SetActive(difficulty >= 1);
            if (difficultyTwo != null) difficultyTwo.SetActive(difficulty >= 2);
            if (difficultyThree != null) difficultyThree.SetActive(difficulty >= 3);
        }

        private void HandleClick()
        {
            if (_selected != null) _selected(_levelIndex);
        }
    }
}
