// ============================================================
//  PuzzleRoom.cs
//  Tracks a collection of PuzzleObjects inside one room.
//  When every required object is solved, the room is marked
//  complete and downstream gates/doors can react.
//
//  Place in: Assets/Scripts/Puzzle/
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FiveElements
{
    public class PuzzleRoom : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────
        [Header("Identity")]
        [SerializeField] private string roomID;

        [Header("Puzzle Objects")]
        [Tooltip("All PuzzleObjects that must be solved to complete this room. " +
                 "Leave empty to auto-collect all children.")]
        [SerializeField] private List<PuzzleObject> puzzleObjects = new();

        [Header("Completion Reward")]
        [Tooltip("Element unlocked when this room is completed. None = no unlock.")]
        [SerializeField] private ElementType rewardElement = ElementType.None;

        [Tooltip("Seconds to wait after solving before granting reward.")]
        [SerializeField] private float solvedDelay = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip roomSolvedSound;
        [SerializeField] private AudioClip roomResetSound;

        [Header("Unity Events")]
        public UnityEvent OnRoomSolved;
        public UnityEvent OnRoomReset;

        // ── State ─────────────────────────────────────────────────
        private bool _isSolved = false;
        private HashSet<string> _solvedIDs = new HashSet<string>();

        public bool IsSolved => _isSolved;
        public string RoomID => roomID;

        // Code-subscription event
        public System.Action<PuzzleRoom> OnSolved;

        // ──────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────
        private void Awake()
        {
            // Auto-collect if list not populated in inspector
            if (puzzleObjects.Count == 0)
                puzzleObjects.AddRange(GetComponentsInChildren<PuzzleObject>());
        }

        private void Start()
        {
            foreach (var obj in puzzleObjects)
            {
                obj.OnObjectSolved += HandleObjectSolved;
                obj.OnObjectActivated += HandleObjectActivated;
            }

            Debug.Log($"[PuzzleRoom:{roomID}] Tracking {puzzleObjects.Count} puzzle object(s).");
        }

        private void OnDestroy()
        {
            foreach (var obj in puzzleObjects)
                if (obj != null)
                {
                    obj.OnObjectSolved -= HandleObjectSolved;
                    obj.OnObjectActivated -= HandleObjectActivated;
                }
        }

        // ──────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns how many puzzle objects are currently solved.
        /// </summary>
        public int SolvedCount => _solvedIDs.Count;

        /// <summary>
        /// Returns total number of puzzle objects in this room.
        /// </summary>
        public int TotalCount => puzzleObjects.Count;

        /// <summary>
        /// Force the room into a solved state (used by save/load restore).
        /// </summary>
        public void ForceComplete()
        {
            foreach (var obj in puzzleObjects)
                obj.Solve();
            MarkRoomSolved();
        }

        /// <summary>
        /// Reset all puzzle objects and room state.
        /// </summary>
        public void ResetRoom()
        {
            if (_isSolved) return; // solved rooms don't reset

            _solvedIDs.Clear();
            foreach (var obj in puzzleObjects)
                obj.Reset();

            if (roomResetSound != null)
                AudioSource.PlayClipAtPoint(roomResetSound, transform.position);

            OnRoomReset?.Invoke();
            Debug.Log($"[PuzzleRoom:{roomID}] Room reset.");
        }

        // ──────────────────────────────────────────────────────────
        // Private
        // ──────────────────────────────────────────────────────────
        private void HandleObjectSolved(PuzzleObject obj)
        {
            _solvedIDs.Add(obj.PuzzleID);
            Debug.Log($"[PuzzleRoom:{roomID}] Object solved: {obj.PuzzleID} " +
                      $"({_solvedIDs.Count}/{puzzleObjects.Count})");

            CheckAllSolved();
        }

        private void HandleObjectActivated(PuzzleObject obj)
        {
            // Can be used to give partial feedback (e.g. progress bar fills)
        }

        private void CheckAllSolved()
        {
            if (_isSolved) return;

            foreach (var obj in puzzleObjects)
                if (!obj.IsSolved) return; // at least one not solved

            MarkRoomSolved();
        }

        private void MarkRoomSolved()
        {
            _isSolved = true;
            Debug.Log($"[PuzzleRoom:{roomID}] ✓ ROOM SOLVED");

            if (roomSolvedSound != null)
                AudioSource.PlayClipAtPoint(roomSolvedSound, transform.position);

            OnRoomSolved?.Invoke();
            OnSolved?.Invoke(this);

            if (rewardElement != ElementType.None)
                StartCoroutine(GrantRewardAfterDelay());
        }

        private IEnumerator GrantRewardAfterDelay()
        {
            yield return new WaitForSeconds(solvedDelay);
            LevelProgressionManager.Instance?.TriggerElementUnlock(rewardElement);
        }
    }
}