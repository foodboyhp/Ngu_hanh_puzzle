// ============================================================
//  LevelProgressionManager.cs
//  Singleton. Manages: scene transitions, checkpoint system,
//  player respawn, element unlock story gates, and saves
//  which elements/levels are completed.
//
//  Place in: Assets/Scripts/Managers/
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FiveElements
{
    public class LevelProgressionManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────
        public static LevelProgressionManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────
        [Header("Level Sequence")]
        [Tooltip("Scene names in unlock order.")]
        [SerializeField]
        private List<string> levelScenes = new List<string>
        {
            "Level_01_Water",
            "Level_02_Wood",
            "Level_03_Fire",
            "Level_04_Earth",
            "Level_05_Metal",
            "Level_06_AllElements"
        };

        [Header("Respawn")]
        [SerializeField] private float respawnDelay = 2f;
        [SerializeField] private string gameOverScene = "GameOver";

        [Header("Transition")]
        [SerializeField] private Animator transitionAnimator;  // fade-in/out animator
        [SerializeField] private float transitionDuration = 0.5f;

        // ── State ─────────────────────────────────────────────────
        private int _currentLevelIndex = 0;
        private Vector3 _lastCheckpointPos;
        private bool _hasCheckpoint = false;

        // Saved progress (written to PlayerPrefs for simplicity;
        // swap for a proper save system in production)
        private const string SAVE_KEY_LEVEL = "SavedLevelIndex";
        private const string SAVE_KEY_ELEMENTS = "SavedElements";

        // ── Events ────────────────────────────────────────────────
        public System.Action<int> OnLevelLoaded;       // level index
        public System.Action<ElementType> OnElementUnlockCutscene; // before absorb

        // ──────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Listen for player death
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.OnDeath += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // ──────────────────────────────────────────────────────────
        // Scene Lifecycle
        // ──────────────────────────────────────────────────────────
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Re-subscribe to player death (new scene means new player instance)
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.OnDeath += HandlePlayerDeath;

            _hasCheckpoint = false;
            OnLevelLoaded?.Invoke(_currentLevelIndex);

            Debug.Log($"[LevelProgressionManager] Scene loaded: {scene.name}");
        }

        // ──────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────

        /// <summary>Load the next level in the sequence with a fade transition.</summary>
        public void LoadNextLevel()
        {
            _currentLevelIndex++;
            if (_currentLevelIndex >= levelScenes.Count)
            {
                Debug.Log("[LevelProgressionManager] All levels complete!");
                LoadScene("Credits");
                return;
            }
            StartCoroutine(LoadLevelRoutine(levelScenes[_currentLevelIndex]));
        }

        /// <summary>Load a specific level by index.</summary>
        public void LoadLevel(int index)
        {
            if (index < 0 || index >= levelScenes.Count)
            {
                Debug.LogError($"[LevelProgressionManager] Invalid level index: {index}");
                return;
            }
            _currentLevelIndex = index;
            StartCoroutine(LoadLevelRoutine(levelScenes[index]));
        }

        /// <summary>Load any scene by name with transition.</summary>
        public void LoadScene(string sceneName) =>
            StartCoroutine(LoadLevelRoutine(sceneName));

        /// <summary>Register a checkpoint at the player's current position.</summary>
        public void RegisterCheckpoint(Vector3 position)
        {
            _lastCheckpointPos = position;
            _hasCheckpoint = true;
            Debug.Log($"[LevelProgressionManager] Checkpoint saved at {position}");
            SaveProgress();
        }

        /// <summary>
        /// Called by an ElementShrine when the player reaches it.
        /// Plays a cutscene, then grants the element.
        /// </summary>
        public void TriggerElementUnlock(ElementType element)
        {
            StartCoroutine(ElementUnlockSequence(element));
        }

        // ──────────────────────────────────────────────────────────
        // Save / Load
        // ──────────────────────────────────────────────────────────
        public void SaveProgress()
        {
            PlayerPrefs.SetInt(SAVE_KEY_LEVEL, _currentLevelIndex);

            // Encode absorbed elements as a comma-separated int list
            if (ElementManager.Instance != null)
            {
                var parts = new List<string>();
                foreach (var e in ElementManager.Instance.AbsorbedElements)
                    parts.Add(((int)e).ToString());
                PlayerPrefs.SetString(SAVE_KEY_ELEMENTS, string.Join(",", parts));
            }

            PlayerPrefs.Save();
            Debug.Log("[LevelProgressionManager] Progress saved.");
        }

        public void LoadProgress()
        {
            _currentLevelIndex = PlayerPrefs.GetInt(SAVE_KEY_LEVEL, 0);

            string savedElements = PlayerPrefs.GetString(SAVE_KEY_ELEMENTS, "");
            if (!string.IsNullOrEmpty(savedElements) && ElementManager.Instance != null)
            {
                // Clear existing and re-absorb saved elements
                foreach (var token in savedElements.Split(','))
                {
                    if (int.TryParse(token, out int val))
                        ElementManager.Instance.AbsorbElement((ElementType)val);
                }
            }

            Debug.Log($"[LevelProgressionManager] Progress loaded. Level: {_currentLevelIndex}");
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY_LEVEL);
            PlayerPrefs.DeleteKey(SAVE_KEY_ELEMENTS);
            PlayerPrefs.Save();
        }

        // ──────────────────────────────────────────────────────────
        // Private Routines
        // ──────────────────────────────────────────────────────────
        private void HandlePlayerDeath()
        {
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            // Fade out
            transitionAnimator?.SetTrigger("FadeOut");
            yield return new WaitForSeconds(respawnDelay);

            if (_hasCheckpoint)
            {
                // Reload current scene and reposition player
                string currentScene = SceneManager.GetActiveScene().name;
                yield return SceneManager.LoadSceneAsync(currentScene);
                yield return null; // wait one frame for scene to settle

                var player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                    player.transform.position = _lastCheckpointPos;
            }
            else
            {
                // No checkpoint — reload from start or show game over
                SceneManager.LoadScene(gameOverScene);
                yield break;
            }

            // Fade in
            transitionAnimator?.SetTrigger("FadeIn");
        }

        private IEnumerator LoadLevelRoutine(string sceneName)
        {
            // Fade out
            transitionAnimator?.SetTrigger("FadeOut");
            yield return new WaitForSeconds(transitionDuration);

            var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone) yield return null;

            // Fade in
            yield return new WaitForSeconds(0.1f);
            transitionAnimator?.SetTrigger("FadeIn");
        }

        private IEnumerator ElementUnlockSequence(ElementType element)
        {
            // 1. Pause gameplay
            Time.timeScale = 0f;

            // 2. Notify UI / cutscene system
            OnElementUnlockCutscene?.Invoke(element);

            // 3. Wait for cutscene (driven externally; we wait a minimum time)
            yield return new WaitForSecondsRealtime(3f);

            // 4. Resume and grant element
            Time.timeScale = 1f;
            ElementManager.Instance?.AbsorbElement(element);

            // 5. Save
            SaveProgress();
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Checkpoint MonoBehaviour — place in scene at save points
    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Place in scene. When player enters the trigger, registers a checkpoint.
    /// Attach a visual (torch, shrine glyph) to show activation.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Animator checkpointAnimator;
        [SerializeField] private AudioClip activateSound;

        private bool _activated = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_activated || !other.CompareTag("Player")) return;

            _activated = true;
            LevelProgressionManager.Instance?.RegisterCheckpoint(transform.position);
            checkpointAnimator?.SetTrigger("Activate");

            if (activateSound != null)
                AudioSource.PlayClipAtPoint(activateSound, transform.position);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // LevelExit — triggers next level load
    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Place at the end of a level. Triggers the next scene when
    /// the player enters AND the level's PuzzleRoom is solved.
    /// </summary>
    public class LevelExit : MonoBehaviour
    {
        [SerializeField] private PuzzleRoom requiredRoom;  // must be solved to exit
        [SerializeField] private GameObject lockedIndicator;
        [SerializeField] private GameObject unlockedIndicator;

        private void Update()
        {
            bool open = requiredRoom == null || requiredRoom.IsSolved;
            if (lockedIndicator) lockedIndicator.SetActive(!open);
            if (unlockedIndicator) unlockedIndicator.SetActive(open);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            bool canExit = requiredRoom == null || requiredRoom.IsSolved;
            if (canExit)
                LevelProgressionManager.Instance?.LoadNextLevel();
        }
    }
}