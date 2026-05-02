using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Cutscene Object")]
    [SerializeField] private GameObject cutsceneObject;

    private Animator cutsceneAnimator;

    [Header("Animation")]
    [SerializeField] private string playTriggerName = "PlayCutscene";
    [SerializeField] private float cutsceneDuration = 3f;

    [Header("Skip UI")]
    [SerializeField] private GameObject skipPromptUI;
    [SerializeField] private float skipPromptDelay = 3f;
    [SerializeField] private float skipPromptDuration = 3f;

    [Header("Button Delay")]
    [SerializeField] private float buttonDelay = 1f;

    private bool isTransitioning = false;
    private bool canSkip = false;

    void Awake()
    {
        if (cutsceneObject != null)
        {
            cutsceneAnimator = cutsceneObject.GetComponent<Animator>();
        }

        if (skipPromptUI != null)
            skipPromptUI.SetActive(false);
    }

    void Update()
    {
        if (canSkip && IsSkipPressed())
        {
            SkipCutscene();
        }
    }

    public void PlayGame()
    {
        if (isTransitioning) return;
        StartCoroutine(DelayedPlayGame());
    }

    public void QuitGame()
    {
        if (isTransitioning) return;
        StartCoroutine(DelayedQuitGame());
    }

    IEnumerator DelayedPlayGame()
    {
        isTransitioning = true;

        yield return new WaitForSeconds(buttonDelay);

        ExecutePlayGame();
    }

    IEnumerator DelayedQuitGame()
    {
        isTransitioning = true;

        yield return new WaitForSeconds(buttonDelay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ExecutePlayGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cutsceneObject != null)
        {
            cutsceneObject.SetActive(true);
            StartCoroutine(PlayCutsceneNextFrame());
        }
        else
        {
            LoadScene();
        }
    }

    IEnumerator PlayCutsceneNextFrame()
    {
        yield return null;

        if (cutsceneAnimator != null && cutsceneAnimator.runtimeAnimatorController != null)
        {
            cutsceneAnimator.SetTrigger(playTriggerName);

            canSkip = true;

            StartCoroutine(ShowSkipPrompt());
            StartCoroutine(LoadAfterCutscene());
        }
        else
        {
            Debug.LogError("Animator missing or no controller assigned!");
            LoadScene();
        }
    }

    IEnumerator ShowSkipPrompt()
    {
        if (skipPromptUI == null) yield break;

        yield return new WaitForSeconds(skipPromptDelay);

        if (!canSkip) yield break;

        skipPromptUI.SetActive(true);

        yield return new WaitForSeconds(skipPromptDuration);

        skipPromptUI.SetActive(false);
    }

    bool IsSkipPressed()
    {
        return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
               (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }

    void SkipCutscene()
    {
        if (!canSkip) return;

        canSkip = false;

        if (skipPromptUI != null)
            skipPromptUI.SetActive(false);

        LoadScene();
    }

    public void OnCutsceneEnd()
    {
        if (!canSkip) return;

        canSkip = false;
        LoadScene();
    }

    IEnumerator LoadAfterCutscene()
    {
        yield return new WaitForSeconds(cutsceneDuration);

        if (canSkip)
        {
            canSkip = false;
            LoadScene();
        }
    }

    void LoadScene()
    {
        SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
    }
}