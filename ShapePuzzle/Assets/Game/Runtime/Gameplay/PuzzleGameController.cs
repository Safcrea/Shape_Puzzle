using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ToyPuzzle
{
    [DisallowMultipleComponent]
    public sealed class PuzzleGameController : MonoBehaviour
    {
        [SerializeField] private PuzzleBoardView boardView;
        [SerializeField] private ToyTween tween;
        [SerializeField] private AudioService audioService;
        [SerializeField] private HapticService hapticService;
        [SerializeField, Range(0.1f, 0.8f)] private float snapThresholdInCells = 0.48f;

        private PuzzleSession _session;
        private PuzzlePieceView _selected;
        private Vector2 _dragOffset;
        private Vector2 _dragStart;
        private bool _dragging;
        private GridCoordinate[] _dragFootprint = System.Array.Empty<GridCoordinate>();
        private PuzzleLevelPrefab _levelPrefab;
        private Vector2? _assemblyOffset;

        public event Action<PuzzleSession> SessionStarted;
        public event Action<PuzzleSession> SessionChanged;
        public event Action<PuzzleSession> LevelCompleted;
        public event Action<PuzzleActionType> ValidAction;
        public event Action InvalidAction;
        public event Action HintUsed;
        public event Action FreeformProgressChanged;
        public PuzzleSession Session => _session;
        public string SelectedPieceId => _selected == null ? null : _selected.PieceId;
        public int MoveBudget => _session == null
            ? 0
            : Mathf.Max(1, _session.Level.recommendedMoves);
        public int MovesRemaining => _session == null ? 0 : Mathf.Max(0, MoveBudget - _session.MoveCount);
        public bool HasSnapPower => _session != null && _session.Level.levelNumber > 10;
        public Vector2 AssemblyOffset => _assemblyOffset ?? Vector2.zero;

        private void Update()
        {
            if (_session != null) _session.AdvanceTime(Time.unscaledDeltaTime);
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            Event current = Event.current;
            if (current != null && current.type == EventType.KeyDown && current.keyCode == KeyCode.R)
            {
                RotateSelected();
                current.Use();
            }
        }
#endif

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelDrag();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CancelDrag();
        }

        public void LoadLevel(RuntimeLevelData runtimeLevel)
        {
            if (runtimeLevel == null || runtimeLevel.Level == null) throw new ArgumentNullException(nameof(runtimeLevel));
            LoadLevel(runtimeLevel.Level, null);
        }

        public void LoadLevel(PuzzleLevelPrefab levelPrefab)
        {
            if (levelPrefab == null || levelPrefab.Level == null) throw new ArgumentNullException(nameof(levelPrefab));
            LoadLevel(levelPrefab.Level, levelPrefab);
        }

        public void LoadLevel(LevelDefinition level)
        {
            LoadLevel(level, null);
        }

        private void LoadLevel(LevelDefinition level, PuzzleLevelPrefab levelPrefab)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            UnsubscribeSession();
            _session = new PuzzleSession(level);
            _session.PieceChanged += HandlePieceChanged;
            _session.StateChanged += HandleStateChanged;
            _session.Completed += HandleCompleted;
            _levelPrefab = levelPrefab;
            _assemblyOffset = null;
            if (boardView == null) boardView = GetComponentInChildren<PuzzleBoardView>(true);
            boardView.Build(_session, this, levelPrefab);
            SelectPiece(null);
            SessionStarted?.Invoke(_session);
        }

        public void SelectPiece(PuzzlePieceView view)
        {
            if (_selected == view) return;
            if (_selected != null) _selected.SetSelected(false);
            _selected = view;
            if (_selected != null)
            {
                _selected.SetSelected(true);
                Play(ToyAudioCue.PiecePickup);
                if (hapticService != null) hapticService.Play(HapticCue.Selection);
            }
        }

        public void BeginDrag(PuzzlePieceView view, PointerEventData eventData)
        {
            if (_session == null || view == null || view != _selected || view.IsLocked || boardView.PieceLayer == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardView.PieceLayer, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            _dragOffset = local - view.RectTransform.anchoredPosition;
            _dragStart = view.RectTransform.anchoredPosition;
            if (view.UsesFreeformArtwork)
            {
                _dragFootprint = System.Array.Empty<GridCoordinate>();
                _dragging = true;
                view.RectTransform.SetAsLastSibling();
                return;
            }
            if (_session.TryGetPiece(view.PieceId, out PieceState state))
            {
                _dragFootprint = GridMath.GetRotatedFootprint(state.Definition, state.Pose.rotation).Cells;
            }
            _dragging = true;
            view.RectTransform.SetAsLastSibling();
        }

        public void Drag(PuzzlePieceView view, PointerEventData eventData)
        {
            if (!_dragging || view == null || view != _selected) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardView.PieceLayer, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            if (view.UsesFreeformArtwork)
            {
                view.SetFreeformPosition(local - _dragOffset);
                return;
            }
            view.RectTransform.anchoredPosition = local - _dragOffset;
            GridCoordinate candidatePosition = boardView.GetCandidatePosition(view);
            bool near = boardView.IsWithinSnapThreshold(view, candidatePosition, snapThresholdInCells);
            bool valid = near && IsDragCandidateValid(view.PieceId, candidatePosition);
            view.SetPlacementTint(valid);
        }

        public void EndDrag(PuzzlePieceView view, PointerEventData eventData)
        {
            if (!_dragging || view == null || view != _selected) return;
            _dragging = false;
            view.ClearPlacementTint();
            if (view.UsesFreeformArtwork)
            {
                TryCommitFreeformPlacement(view);
                FreeformProgressChanged?.Invoke();
                return;
            }
            GridCoordinate candidate = boardView.GetCandidatePosition(view);
            bool near = boardView.IsWithinSnapThreshold(view, candidate, snapThresholdInCells);
            PuzzleActionResult result = near ? _session.TryMove(view.PieceId, candidate) : default;
            if (near && result.Succeeded)
            {
                ValidAction?.Invoke(PuzzleActionType.Move);
                Play(ToyAudioCue.PieceDrop);
                if (_session.TryGetPiece(view.PieceId, out PieceState state) && state.IsCorrect)
                {
                    Play(ToyAudioCue.CorrectPlacement);
                    if (hapticService != null) hapticService.Play(HapticCue.Correct);
                    if (tween != null) tween.Pulse(view.RectTransform, 1.1f, 0.22f);
                }
            }
            else
            {
                _session.RegisterFailedMove();
                InvalidAction?.Invoke();
                view.RectTransform.anchoredPosition = _dragStart;
                Play(ToyAudioCue.InvalidPlacement);
                if (hapticService != null) hapticService.Play(HapticCue.Invalid);
                if (tween != null) tween.Shake(view.RectTransform);
            }
        }

        public bool TryPlaceFreeformPiece(string pieceId, Vector2 boardPosition)
        {
            if (_session == null || boardView == null || string.IsNullOrEmpty(pieceId)) return false;
            PuzzlePieceView view = boardView.FindPiece(pieceId);
            if (view == null || !view.UsesFreeformArtwork || view.IsLocked) return false;
            _dragStart = view.RectTransform.anchoredPosition;
            view.SetFreeformPosition(boardPosition);
            bool placed = TryCommitFreeformPlacement(view);
            FreeformProgressChanged?.Invoke();
            return placed;
        }

        public void RotateSelected()
        {
            if (_session == null || _selected == null || _selected.IsLocked) return;
            if (_selected.UsesFreeformArtwork) return;
            PuzzleActionResult result = _session.TryRotate(_selected.PieceId);
            if (result.Succeeded)
            {
                ValidAction?.Invoke(PuzzleActionType.Rotate);
                Play(ToyAudioCue.Rotate);
                if (tween != null) tween.Pulse(_selected.RectTransform, 1.08f, 0.16f);
            }
            else if (result.Failure != PuzzleActionFailure.NoChange)
            {
                InvalidAction?.Invoke();
                Play(ToyAudioCue.InvalidPlacement);
                if (hapticService != null) hapticService.Play(HapticCue.Invalid);
                if (tween != null) tween.Shake(_selected.RectTransform);
            }
        }

        public void Undo()
        {
            if (_session == null || !_session.TryUndo(out MoveRecord record)) return;
            Play(ToyAudioCue.Undo);
            PuzzlePieceView view = boardView.FindPiece(record.PieceId);
            if (view != null && tween != null) tween.Pulse(view.RectTransform, 1.06f, 0.16f);
            if (view != null && view.UsesFreeformArtwork)
            {
                RefreshAssemblyOffsetFromCorrectPieces();
                FreeformProgressChanged?.Invoke();
            }
        }

        public void Hint()
        {
            if (_session == null) return;
            PieceState state = _session.RequestHint();
            if (state == null) return;
            PuzzlePieceView view = boardView.FindPiece(state.PieceId);
            if (view != null && tween != null) tween.Pulse(view.RectTransform, 1.14f, 0.5f);
            if (HasSnapPower && view != null && view.UsesFreeformArtwork)
            {
                if (!_assemblyOffset.HasValue)
                {
                    Vector2 baseTarget = boardView.GetFreeformTarget(view.PieceId, Vector2.zero);
                    _assemblyOffset = boardView.ClampAssemblyOffset(view.RectTransform.anchoredPosition - baseTarget);
                }
                Vector2 target = boardView.GetFreeformTarget(view.PieceId, _assemblyOffset.Value);
                PuzzleActionResult result = _session.TryMove(view.PieceId, state.Definition.targetPosition);
                if (result.Succeeded)
                {
                    view.SnapToFreeformTarget(target);
                    ValidAction?.Invoke(PuzzleActionType.Move);
                    FreeformProgressChanged?.Invoke();
                }
            }
            else
            {
                boardView.ShowHint(state.Definition, _assemblyOffset ?? Vector2.zero);
            }
            HintUsed?.Invoke();
            Play(ToyAudioCue.Hint);
        }

        public void ResetLevel()
        {
            if (_session == null) return;
            _assemblyOffset = null;
            _session.Reset();
            boardView.ClearHint();
            boardView.ApplyAll(_session);
            SelectPiece(null);
            FreeformProgressChanged?.Invoke();
        }

        public PieceProgressData[] CaptureFreeformProgress()
        {
            if (_session == null || boardView == null || boardView.PieceLayer == null)
                return Array.Empty<PieceProgressData>();
            Vector2 size = boardView.PieceLayer.rect.size;
            if (size.x <= 0f || size.y <= 0f) return Array.Empty<PieceProgressData>();
            var result = new List<PieceProgressData>();
            IReadOnlyList<PieceState> pieces = _session.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                PuzzlePieceView view = boardView.FindPiece(state.PieceId);
                if (view == null || !view.UsesFreeformArtwork) continue;
                Vector2 position = view.RectTransform.anchoredPosition;
                result.Add(new PieceProgressData
                {
                    pieceId = state.PieceId,
                    normalizedX = Mathf.Clamp01(position.x / size.x),
                    normalizedY = Mathf.Clamp01(position.y / size.y),
                    snapped = state.IsCorrect
                });
            }
            return result.ToArray();
        }

        public bool RestoreFreeformProgress(LevelProgressData progress)
        {
            if (_session == null || progress == null || progress.pieceProgress == null ||
                progress.pieceProgress.Length == 0 || boardView == null || boardView.PieceLayer == null)
                return false;

            var savedById = new Dictionary<string, PieceProgressData>(StringComparer.Ordinal);
            for (int i = 0; i < progress.pieceProgress.Length; i++)
            {
                PieceProgressData saved = progress.pieceProgress[i];
                if (saved == null || string.IsNullOrEmpty(saved.pieceId) || savedById.ContainsKey(saved.pieceId)) return false;
                savedById.Add(saved.pieceId, saved);
            }

            var correctIds = new HashSet<string>(StringComparer.Ordinal);
            int freeformCount = 0;
            IReadOnlyList<PieceState> pieces = _session.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                PuzzlePieceView view = boardView.FindPiece(pieces[i].PieceId);
                if (view == null || !view.UsesFreeformArtwork) continue;
                freeformCount++;
                if (!savedById.TryGetValue(pieces[i].PieceId, out PieceProgressData saved)) return false;
                if (saved.snapped) correctIds.Add(saved.pieceId);
            }
            if (freeformCount == 0 || savedById.Count != freeformCount) return false;
            if (!_session.RestoreProgress(
                    correctIds,
                    progress.inProgressMoveCount,
                    progress.inProgressElapsedSeconds,
                    progress.inProgressHintsUsed))
                return false;

            boardView.ApplyAll(_session);
            Vector2 boardSize = boardView.PieceLayer.rect.size;
            _assemblyOffset = null;
            foreach (KeyValuePair<string, PieceProgressData> pair in savedById)
            {
                PuzzlePieceView view = boardView.FindPiece(pair.Key);
                view.SetFreeformPosition(new Vector2(pair.Value.normalizedX * boardSize.x, pair.Value.normalizedY * boardSize.y));
                if (pair.Value.snapped && !_assemblyOffset.HasValue)
                {
                    Vector2 baseTarget = boardView.GetFreeformTarget(pair.Key, Vector2.zero);
                    _assemblyOffset = boardView.ClampAssemblyOffset(view.RectTransform.anchoredPosition - baseTarget);
                }
            }
            if (_assemblyOffset.HasValue)
            {
                foreach (string correctId in correctIds)
                {
                    PuzzlePieceView view = boardView.FindPiece(correctId);
                    if (view != null) view.SnapToFreeformTarget(boardView.GetFreeformTarget(correctId, _assemblyOffset.Value));
                }
            }
            SelectPiece(null);
            return true;
        }

        public void SetPaused(bool paused)
        {
            if (_session != null) _session.SetPaused(paused);
        }

        public void CancelDrag()
        {
            if (!_dragging || _selected == null) return;
            _dragging = false;
            _selected.ClearPlacementTint();
            _selected.RectTransform.anchoredPosition = _dragStart;
        }

        private void HandlePieceChanged(PieceState state)
        {
            boardView.ApplyState(state);
            if (state != null && state.IsCorrect && _assemblyOffset.HasValue)
            {
                PuzzlePieceView view = boardView.FindPiece(state.PieceId);
                if (view != null && view.UsesFreeformArtwork)
                    view.SnapToFreeformTarget(boardView.GetFreeformTarget(state.PieceId, _assemblyOffset.Value));
            }
        }

        private void HandleStateChanged()
        {
            SessionChanged?.Invoke(_session);
        }

        private void HandleCompleted()
        {
            SelectPiece(null);
            Play(ToyAudioCue.LevelComplete);
            if (hapticService != null) hapticService.Play(HapticCue.Completion);
            LevelCompleted?.Invoke(_session);
        }

        private void UnsubscribeSession()
        {
            if (_session == null) return;
            _session.PieceChanged -= HandlePieceChanged;
            _session.StateChanged -= HandleStateChanged;
            _session.Completed -= HandleCompleted;
        }

        private void Play(ToyAudioCue cue)
        {
            if (audioService != null) audioService.Play(cue);
        }

        private bool TryCommitFreeformPlacement(PuzzlePieceView view)
        {
            Vector2 target;
            bool establishingAnchor = !_assemblyOffset.HasValue;
            if (establishingAnchor)
            {
                Vector2 baseTarget = boardView.GetFreeformTarget(view.PieceId, Vector2.zero);
                _assemblyOffset = boardView.ClampAssemblyOffset(view.RectTransform.anchoredPosition - baseTarget);
                target = boardView.GetFreeformTarget(view.PieceId, _assemblyOffset.Value);
            }
            else
            {
                target = boardView.GetFreeformTarget(view.PieceId, _assemblyOffset.Value);
            }

            if (!establishingAnchor && !view.IsNearFreeformTarget(target))
            {
                _session.RegisterFailedMove();
                Play(ToyAudioCue.PieceDrop);
                return false;
            }

            PuzzleActionResult result = _session.TryMove(view.PieceId, view.Definition.targetPosition);
            if (result.Succeeded)
            {
                view.SnapToFreeformTarget(target);
                ValidAction?.Invoke(PuzzleActionType.Move);
                Play(ToyAudioCue.CorrectPlacement);
                if (hapticService != null) hapticService.Play(HapticCue.Correct);
                if (tween != null) tween.Pulse(view.RectTransform, 1.1f, 0.22f);
                return true;
            }

            _session.RegisterFailedMove();
            view.RectTransform.anchoredPosition = _dragStart;
            InvalidAction?.Invoke();
            Play(ToyAudioCue.InvalidPlacement);
            return false;
        }

        private void RefreshAssemblyOffsetFromCorrectPieces()
        {
            _assemblyOffset = null;
            if (_session == null || boardView == null) return;
            IReadOnlyList<PieceState> pieces = _session.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (!state.IsCorrect) continue;
                PuzzlePieceView view = boardView.FindPiece(state.PieceId);
                if (view == null || !view.UsesFreeformArtwork) continue;
                Vector2 baseTarget = boardView.GetFreeformTarget(state.PieceId, Vector2.zero);
                _assemblyOffset = boardView.ClampAssemblyOffset(view.RectTransform.anchoredPosition - baseTarget);
                return;
            }
        }

        private bool IsDragCandidateValid(string pieceId, GridCoordinate origin)
        {
            OccupancyMap occupancy = _session.Occupancy;
            for (int i = 0; i < _dragFootprint.Length; i++)
            {
                GridCoordinate cell = origin + _dragFootprint[i];
                if (!occupancy.IsInside(cell)) return false;
                string occupant = occupancy.GetOccupant(cell);
                if (!string.IsNullOrEmpty(occupant) && !string.Equals(occupant, pieceId, StringComparison.Ordinal)) return false;
            }
            return _dragFootprint.Length > 0;
        }
    }
}
