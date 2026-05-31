// ============================================================
//  GameManager.cs
//  Top-level game state machine.
//  States: MainMenu → Playing ↔ Paused → GameOver → MainMenu
//
//  Also owns the pause menu toggle and ties together the other
//  manager singletons on startup.
//
//  Place in: Assets/Scripts/Managers/
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace FiveElements
{
    public enum GameState { MainMenu, Loading, Playing, Paused, GameOver, Credits }

    public class GameController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────
        public static GameController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────
        [Header("Scene Names")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string firstLevelScene = "Level_01_Water";

        [Header("UI Panels (assign from PersistentUI canvas)")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject hudPanel;

        // ── State ─────────────────────────────────────────────────
        private GameState _state = GameState.MainMenu;

        public GameState CurrentState => _state;
        public bool IsPlaying => _state == GameState.Playing;
        public bool IsPaused => _state == GameState.Paused;

        // ── Events ────────────────────────────────────────────────
        public System.Action<GameState> OnStateChanged;

        // ──────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                HandleEscapeKey();
        }

        // ──────────────────────────────────────────────────────────
        // State Transitions
        // ──────────────────────────────────────────────────────────
        public void StartNewGame()
        {
            ElementManager.Instance?.AbsorbElement(ElementType.Water); // reset isn't needed here,
            // LevelProgressionManager handles scene loading
            LevelProgressionManager.Instance?.DeleteSave();
            LevelProgressionManager.Instance?.LoadScene(firstLevelScene);
            TransitionTo(GameState.Playing);
        }

        public void ContinueGame()
        {
            LevelProgressionManager.Instance?.LoadProgress();
            TransitionTo(GameState.Playing);
        }

        public void PauseGame()
        {
            if (_state != GameState.Playing) return;
            Time.timeScale = 0f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            TransitionTo(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (_state != GameState.Paused) return;
            Time.timeScale = 1f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            TransitionTo(GameState.Playing);
        }

        public void TriggerGameOver()
        {
            Time.timeScale = 0f;
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);
            TransitionTo(GameState.GameOver);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false);
            SceneManager.LoadScene(mainMenuScene);
            TransitionTo(GameState.MainMenu);
        }

        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ──────────────────────────────────────────────────────────
        // Private
        // ──────────────────────────────────────────────────────────
        private void HandleEscapeKey()
        {
            if (_state == GameState.Playing) PauseGame();
            else if (_state == GameState.Paused) ResumeGame();
        }

        private void TransitionTo(GameState newState)
        {
            _state = newState;
            Debug.Log($"[GameManager] State → {newState}");
            OnStateChanged?.Invoke(newState);

            // Show/hide HUD based on state
            if (hudPanel != null)
                hudPanel.SetActive(newState == GameState.Playing ||
                                   newState == GameState.Paused);
        }
    }
}