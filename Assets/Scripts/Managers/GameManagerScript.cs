using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManagerScript : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuUI;
    public GameObject gameOverUI;
    public GameObject gameWinUI;

    private bool isPaused;

    [Header("Level Settings")]
    public bool isFinalLevel = false;

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;

        pauseMenuUI.SetActive(false);
        gameOverUI.SetActive(false);
        gameWinUI.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (IsPausePressed())
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    private bool IsPausePressed()
    {
        bool keyboardPause =
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;

        bool gamepadPause =
            Gamepad.current != null &&
            Gamepad.current.startButton.wasPressedThisFrame;

        return keyboardPause || gamepadPause;
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f; // Ensure game is not paused

        int currentIndex = SceneManager.GetActiveScene().buildIndex; // Get current scene index

        SceneManager.LoadScene(currentIndex + 1); // Load next scene in build order
    }

    public void PauseGame()
    {
        if (gameOverUI.activeSelf || gameWinUI.activeSelf) return;

        pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void GameOver()
    {
        StartCoroutine(GameOverDelay());
        IEnumerator GameOverDelay()
        {
            // Wait a few seconds (adjust if needed)
            yield return new WaitForSeconds(1f);

            // Show Game Over screen
            gameOverUI.SetActive(true);

            // Pause the game
            Time.timeScale = 1.0f;
        }
    }

    public void GameWin()
    {
        gameWinUI.SetActive(true);
    }

    public void Resume()
    {
        ResumeGame();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        // Best practice to handle quitting correctly in both the Editor and a built game.
#if UNITY_EDITOR
        // Exits Play mode in the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quits the application when running as a built executable
        Debug.Log("Quit function called"); // Check the Console for this message
        Application.Quit(); // This line only works in a build on its own
#endif
        // MAKE SURE THAT YOU HAVE AN EVENT SYSTEM IN THE SCENE OF THE CANVAS SO THAT WAY THE BUTTON INTAKE CAN BE REGISTERED BY THE SYSTEM.
    }
}