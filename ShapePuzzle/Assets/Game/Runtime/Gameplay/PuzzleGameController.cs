using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ToyPuzzle
{
    [Serializable]
    public sealed class PuzzleInteractionSettings
    {
        [Range(1f, 1.35f)] public float pickupScale = 1.2f;
        [Range(0f, 1.5f)] public float fingerLiftHeightFraction = 0.65f;
        [Range(0f, 12f)] public float maximumTiltDegrees = 5f;
        [Range(4f, 40f)] public float dragFollowSharpness = 18f;
        [Range(4f, 50f)] public float magneticSettleSharpness = 28f;
        [Range(0.05f, 0.5f)] public float magneticRangeFraction = 0.2f;
        [Range(0.08f, 0.6f)] public float trailGlowDuration = 0.28f;
        [Range(0f, 1f)] public float trailGlowStrength = 0.62f;
        [Range(1f, 16f)] public float invalidWobbleDegrees = 7f;
        [Range(0.08f, 0.6f)] public float invalidWobbleDuration = 0.24f;
    }

    [DisallowMultipleComponent]
    public sealed class PuzzleGameController : MonoBehaviour
    {
        [SerializeField] private PuzzleBoardView boardView;
        [SerializeField] private ToyTween tween;
        [SerializeField] private AudioService audioService;
        [SerializeField] private HapticService hapticService;
        [SerializeField, Range(0.1f, 0.8f)] private float snapThresholdInCells = 0.48f;
        [SerializeField] private PuzzleInteractionSettings interaction = new PuzzleInteractionSettings();

        private PuzzleSession _session;
        private PuzzlePieceView _selected;
        private Vector2 _dragOffset;
        private Vector2 _dragStart;
        private Vector2 _lastDragDesired;
        private bool _dragging;
        private string _magneticTargetSlotId;
        private Vector2 _magneticTargetPosition;
        private GridCoordinate[] _dragFootprint = System.Array.Empty<GridCoordinate>();
        private PuzzleLevelPrefab _levelPrefab;
        private Vector2? _assemblyOffset;
        private readonly Dictionary<string, string> _targetSlotByPiece = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> _occupiedTargetSlots = new HashSet<string>(StringComparer.Ordinal);
        private bool _completionAnimating;
        private string _completionOriginPieceId;

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
            : Mathf.Max(
                1,
                _session.Level.recommendedMoves -
                (string.IsNullOrEmpty(_session.ReferenceAnchorPieceId) ? 0 : 1));
        public int MovesRemaining => _session == null ? 0 : Mathf.Max(0, MoveBudget - _session.MoveCount);
        public bool HasSnapPower => _session != null && _session.Level.levelNumber > 10;
        public Vector2 AssemblyOffset => _assemblyOffset ?? Vector2.zero;
        public string CompletionOriginPieceId => _completionOriginPieceId;
        public bool IsDragging => _dragging;

        private void Awake()
        {
            if (interaction == null) interaction = new PuzzleInteractionSettings();
        }

        private void Update()
        {
            if (_session != null) _session.AdvanceTime(Time.unscaledDeltaTime);
        }

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
            string anchorPieceId = levelPrefab == null
                ? string.Empty
                : levelPrefab.ReferenceAnchorPieceId;
            _session = new PuzzleSession(level, anchorPieceId);
            _session.PieceChanged += HandlePieceChanged;
            _session.StateChanged += HandleStateChanged;
            _session.Completed += HandleCompleted;
            _levelPrefab = levelPrefab;
            _assemblyOffset = null;
            _targetSlotByPiece.Clear();
            _occupiedTargetSlots.Clear();
            _dragging = false;
            ClearMagneticPreview();
            _completionAnimating = false;
            _completionOriginPieceId = null;
            if (boardView == null) boardView = GetComponentInChildren<PuzzleBoardView>(true);
            boardView.Build(_session, this, levelPrefab);
            InitializeReferenceAnchor();
            SelectPiece(null);
            SessionStarted?.Invoke(_session);
        }

        public void SelectPiece(PuzzlePieceView view)
        {
            if (_completionAnimating && view != null) return;
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
            if (_completionAnimating || _session == null || view == null || view != _selected || view.IsLocked || boardView.PieceLayer == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardView.PieceLayer, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            _dragOffset = local - view.RectTransform.anchoredPosition;
            _dragStart = view.RectTransform.anchoredPosition;
            _lastDragDesired = _dragStart;
            _magneticTargetSlotId = null;
            _magneticTargetPosition = Vector2.zero;
            boardView.ClearHoverTrail();
            if (view.UsesFreeformArtwork)
            {
                _dragFootprint = System.Array.Empty<GridCoordinate>();
                _dragging = true;
                view.BeginDragPresentation(interaction.pickupScale);
                return;
            }
            if (_session.TryGetPiece(view.PieceId, out PieceState state))
            {
                _dragFootprint = GridMath.GetRotatedFootprint(state.Definition, state.Pose.rotation).Cells;
            }
            _dragging = true;
            view.BeginDragPresentation(interaction.pickupScale);
        }

        public void Drag(PuzzlePieceView view, PointerEventData eventData)
        {
            if (!_dragging || view == null || view != _selected) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardView.PieceLayer, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            float lift = view.RectTransform.sizeDelta.y * interaction.fingerLiftHeightFraction;
            Vector2 desired = local - _dragOffset + Vector2.up * lift;
            Vector2 frameMotion = desired - _lastDragDesired;
            _lastDragDesired = desired;
            view.UpdateDragPresentation(
                frameMotion,
                interaction.pickupScale,
                interaction.maximumTiltDegrees);
            if (view.UsesFreeformArtwork)
            {
                desired = view.ClampFreeformPosition(desired);
                float magneticRange = view.LargestVisualDimension * interaction.magneticRangeFraction;
                if (TryFindTargetSlot(
                        view,
                        desired,
                        magneticRange,
                        out string targetSlotId,
                        out Vector2 target))
                {
                    _magneticTargetSlotId = targetSlotId;
                    _magneticTargetPosition = target;
                    view.SetFreeformPosition(SmoothFollow(
                        view.RectTransform.anchoredPosition,
                        target,
                        interaction.magneticSettleSharpness));
                }
                else
                {
                    _magneticTargetSlotId = null;
                    view.SetFreeformPosition(SmoothFollow(
                        view.RectTransform.anchoredPosition,
                        desired,
                        interaction.dragFollowSharpness));
                }
                boardView.UpdateHoverTrail(
                    view,
                    interaction.trailGlowDuration,
                    interaction.trailGlowStrength);
                return;
            }

            Vector2 followed = SmoothFollow(
                view.RectTransform.anchoredPosition,
                desired,
                interaction.dragFollowSharpness);
            view.RectTransform.anchoredPosition = followed;
            GridCoordinate candidatePosition = boardView.GetCandidatePosition(view);
            Vector2 snapped = new Vector2(
                candidatePosition.x * boardView.CellSize,
                candidatePosition.y * boardView.CellSize);
            float magneticRangeForGrid = Mathf.Max(
                boardView.CellSize * snapThresholdInCells,
                view.LargestVisualDimension * interaction.magneticRangeFraction);
            bool near = Vector2.Distance(desired, snapped) <= magneticRangeForGrid;
            bool valid = near && IsDragCandidateValid(view.PieceId, candidatePosition);
            if (valid)
            {
                view.RectTransform.anchoredPosition = SmoothFollow(
                    view.RectTransform.anchoredPosition,
                    snapped,
                    interaction.magneticSettleSharpness);
            }
            view.SetPlacementTint(valid);
            boardView.UpdateHoverTrail(
                view,
                interaction.trailGlowDuration,
                interaction.trailGlowStrength);
        }

        public void EndDrag(PuzzlePieceView view, PointerEventData eventData)
        {
            if (!_dragging || view == null || view != _selected) return;
            _dragging = false;
            boardView.ReleaseHoverTrail();
            view.ClearPlacementTint();
            if (view.UsesFreeformArtwork)
            {
                if (string.IsNullOrEmpty(_magneticTargetSlotId))
                {
                    FailPlacement(view);
                }
                else
                {
                    TryCommitFreeformPlacement(
                        view,
                        _magneticTargetSlotId,
                        _magneticTargetPosition);
                }
                ClearMagneticPreview();
                FreeformProgressChanged?.Invoke();
                return;
            }
            GridCoordinate candidate = boardView.GetCandidatePosition(view);
            bool near = boardView.IsWithinSnapThreshold(view, candidate, snapThresholdInCells);
            PuzzleActionResult result = near ? _session.TryMove(view.PieceId, candidate) : default;
            if (near && result.Succeeded)
            {
                view.EndDragPresentation(0f, true);
                ValidAction?.Invoke(PuzzleActionType.Move);
                Play(ToyAudioCue.PieceDrop);
                if (_session.TryGetPiece(view.PieceId, out PieceState state) && state.IsCorrect)
                {
                    Play(ToyAudioCue.CorrectPlacement);
                    if (hapticService != null) hapticService.Play(HapticCue.Correct);
                    if (tween != null) tween.Pulse(view.RectTransform, 1.1f, 0.22f);
                    boardView.PlayPlacementRipple(_session, view.PieceId, false);
                }
            }
            else
            {
                FailPlacement(view);
            }
        }

        public bool TryPlaceFreeformPiece(string pieceId, Vector2 boardPosition)
        {
            if (_session == null || boardView == null || string.IsNullOrEmpty(pieceId)) return false;
            PuzzlePieceView view = boardView.FindPiece(pieceId);
            if (view == null || !view.UsesFreeformArtwork || view.IsLocked) return false;
            _dragStart = view.RectTransform.anchoredPosition;
            view.SetFreeformPosition(boardPosition);
            float magneticRange = view.LargestVisualDimension * interaction.magneticRangeFraction;
            if (!TryFindTargetSlot(
                    view,
                    view.RectTransform.anchoredPosition,
                    magneticRange,
                    out string targetSlotId,
                    out Vector2 target))
            {
                FailPlacement(view);
                FreeformProgressChanged?.Invoke();
                return false;
            }
            bool placed = TryCommitFreeformPlacement(view, targetSlotId, target);
            FreeformProgressChanged?.Invoke();
            return placed;
        }

        public void Undo()
        {
            if (_session == null || !_session.TryUndo(out MoveRecord record)) return;
            ReleaseTargetSlot(record.PieceId);
            Play(ToyAudioCue.Undo);
            PuzzlePieceView view = boardView.FindPiece(record.PieceId);
            if (view != null && tween != null) tween.Pulse(view.RectTransform, 1.06f, 0.16f);
            if (view != null && view.UsesFreeformArtwork)
            {
                InitializeReferenceAnchor();
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
                if (!_assemblyOffset.HasValue) InitializeReferenceAnchor();
                Vector2 target = boardView.GetFreeformTarget(view.PieceId, _assemblyOffset ?? Vector2.zero);
                PuzzleActionResult result = _session.TryMove(view.PieceId, state.Definition.targetPosition);
                if (result.Succeeded)
                {
                    _targetSlotByPiece[view.PieceId] = view.PieceId;
                    _occupiedTargetSlots.Add(view.PieceId);
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
            _targetSlotByPiece.Clear();
            _occupiedTargetSlots.Clear();
            ClearMagneticPreview();
            _session.Reset();
            boardView.ClearHint();
            boardView.ApplyAll(_session);
            InitializeReferenceAnchor();
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
                    snapped = state.IsCorrect,
                    targetSlotId = _targetSlotByPiece.TryGetValue(state.PieceId, out string slotId) ? slotId : state.PieceId
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
            _targetSlotByPiece.Clear();
            _occupiedTargetSlots.Clear();
            InitializeReferenceAnchor();
            foreach (KeyValuePair<string, PieceProgressData> pair in savedById)
            {
                PuzzlePieceView view = boardView.FindPiece(pair.Key);
                if (view == null) return false;
                if (view.IsReferenceAnchor)
                {
                    view.SnapToFreeformTarget(
                        boardView.GetFreeformTarget(pair.Key, _assemblyOffset ?? Vector2.zero));
                    continue;
                }
                view.SetFreeformPosition(new Vector2(pair.Value.normalizedX * boardSize.x, pair.Value.normalizedY * boardSize.y));
                if (pair.Value.snapped)
                {
                    string slotId = string.IsNullOrEmpty(pair.Value.targetSlotId) ? pair.Key : pair.Value.targetSlotId;
                    _targetSlotByPiece[pair.Key] = slotId;
                    _occupiedTargetSlots.Add(slotId);
                }
            }
            if (_assemblyOffset.HasValue)
            {
                foreach (string correctId in correctIds)
                {
                    PuzzlePieceView view = boardView.FindPiece(correctId);
                    if (view == null || view.IsReferenceAnchor) continue;
                    string slotId = _targetSlotByPiece.TryGetValue(correctId, out string assigned) ? assigned : correctId;
                    view.SnapToFreeformTarget(boardView.GetFreeformTarget(slotId, _assemblyOffset.Value));
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
            boardView.ReleaseHoverTrail();
            _selected.ClearPlacementTint();
            _selected.RectTransform.anchoredPosition = _dragStart;
            _selected.EndDragPresentation(0.08f);
            ClearMagneticPreview();
        }

        private void HandlePieceChanged(PieceState state)
        {
            boardView.ApplyState(state);
            if (state != null && state.IsCorrect && _assemblyOffset.HasValue)
            {
                PuzzlePieceView view = boardView.FindPiece(state.PieceId);
                string slotId = _targetSlotByPiece.TryGetValue(state.PieceId, out string assigned) ? assigned : state.PieceId;
                if (view != null && view.UsesFreeformArtwork)
                    view.SnapToFreeformTarget(boardView.GetFreeformTarget(slotId, _assemblyOffset.Value));
            }
        }

        private void HandleStateChanged()
        {
            SessionChanged?.Invoke(_session);
        }

        private void HandleCompleted()
        {
            _completionAnimating = true;
            _completionOriginPieceId = _selected == null ? null : _selected.PieceId;
            SelectPiece(null);
            Play(ToyAudioCue.LevelComplete);
            if (hapticService != null) hapticService.Play(HapticCue.Completion);
            LevelCompleted?.Invoke(_session);
        }

        public void PlayCompletionRipple()
        {
            if (boardView != null && _session != null)
                boardView.PlayPlacementRipple(_session, _completionOriginPieceId, true);
        }

        public void PlayWholeObjectBounce()
        {
            if (boardView != null) boardView.PlayWholeObjectBounce();
        }

        public void PlayObjectAction()
        {
            if (boardView == null || _session == null) return;
            string action = string.IsNullOrEmpty(_session.Level.completionAction)
                ? _session.Level.targetObjectName
                : _session.Level.completionAction;
            boardView.PlayObjectAction(action);
        }

        public void PlayCompletionPop()
        {
            if (boardView != null) boardView.PlayCompletionPop();
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

        private bool TryCommitFreeformPlacement(
            PuzzlePieceView view,
            string targetSlotId,
            Vector2 target)
        {
            if (!_session.TryGetPiece(view.PieceId, out PieceState pieceState) ||
                GridMath.NormalizeRotation(pieceState.Pose.rotation) != GridMath.NormalizeRotation(pieceState.Definition.targetRotation))
            {
                FailPlacement(view);
                return false;
            }

            if (string.IsNullOrEmpty(targetSlotId) ||
                _occupiedTargetSlots.Contains(targetSlotId) ||
                !IsExactTargetSlot(view, targetSlotId))
            {
                FailPlacement(view);
                return false;
            }

            PuzzleActionResult result = _session.TryMove(view.PieceId, view.Definition.targetPosition);
            if (result.Succeeded)
            {
                _targetSlotByPiece[view.PieceId] = targetSlotId;
                _occupiedTargetSlots.Add(targetSlotId);
                view.SnapToFreeformTarget(target);
                view.EndDragPresentation(0f, true);
                ValidAction?.Invoke(PuzzleActionType.Move);
                Play(ToyAudioCue.CorrectPlacement);
                if (hapticService != null) hapticService.Play(HapticCue.Correct);
                if (tween != null) tween.Pulse(view.RectTransform, 1.1f, 0.22f);
                boardView.PlayPlacementRipple(_session, view.PieceId, false);
                return true;
            }

            FailPlacement(view);
            return false;
        }

        private bool TryFindTargetSlot(
            PuzzlePieceView source,
            Vector2 sourcePosition,
            float magneticRange,
            out string targetSlotId,
            out Vector2 target)
        {
            targetSlotId = null;
            target = Vector2.zero;
            if (source == null || string.IsNullOrEmpty(source.PieceId)) return false;

            string exactTargetSlotId = source.PieceId;
            if (_occupiedTargetSlots.Contains(exactTargetSlotId)) return false;

            Vector2 exactTarget =
                boardView.GetFreeformTarget(exactTargetSlotId, _assemblyOffset ?? Vector2.zero);
            if (Vector2.Distance(sourcePosition, exactTarget) > Mathf.Max(0f, magneticRange)) return false;

            targetSlotId = exactTargetSlotId;
            target = exactTarget;
            return true;
        }

        private static bool IsExactTargetSlot(PuzzlePieceView source, string candidatePieceId)
        {
            return source != null &&
                   string.Equals(source.PieceId, candidatePieceId, StringComparison.Ordinal);
        }

        private void ReleaseTargetSlot(string pieceId)
        {
            if (string.IsNullOrEmpty(pieceId)) return;
            if (_targetSlotByPiece.TryGetValue(pieceId, out string targetSlotId))
                _occupiedTargetSlots.Remove(targetSlotId);
            _targetSlotByPiece.Remove(pieceId);
        }

        private void InitializeReferenceAnchor()
        {
            _assemblyOffset = null;
            if (_session == null || boardView == null) return;
            string anchorId = _session.ReferenceAnchorPieceId;
            if (string.IsNullOrEmpty(anchorId)) return;
            PuzzlePieceView anchor = boardView.FindPiece(anchorId);
            if (anchor == null || !anchor.UsesFreeformArtwork) return;
            _assemblyOffset = boardView.GetCenteredAssemblyOffset(anchorId);
            _targetSlotByPiece[anchorId] = anchorId;
            _occupiedTargetSlots.Add(anchorId);
            anchor.SnapToFreeformTarget(boardView.GetFreeformTarget(anchorId, _assemblyOffset.Value));
        }

        private void FailPlacement(PuzzlePieceView view)
        {
            if (view == null) return;
            _session?.RegisterFailedMove();
            view.RectTransform.anchoredPosition = _dragStart;
            view.EndDragPresentation(0f, true);
            InvalidAction?.Invoke();
            Play(ToyAudioCue.InvalidPlacement);
            if (hapticService != null) hapticService.Play(HapticCue.Invalid);
            if (tween != null)
                tween.WobbleRotation(
                    view.RectTransform,
                    interaction.invalidWobbleDegrees,
                    interaction.invalidWobbleDuration);
        }

        private void ClearMagneticPreview()
        {
            _magneticTargetSlotId = null;
            _magneticTargetPosition = Vector2.zero;
        }

        private static Vector2 SmoothFollow(Vector2 current, Vector2 target, float sharpness)
        {
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, sharpness) *
                                         Mathf.Max(0.001f, Time.unscaledDeltaTime));
            return Vector2.LerpUnclamped(current, target, blend);
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
