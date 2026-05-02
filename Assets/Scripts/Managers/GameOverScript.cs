using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverScript : MonoBehaviour
{
    public Text pointsText;

    public void Setup(int score)
    {
        gameObject.SetActive(true);
        pointsText.text = score.ToString() + " POINTS";
    }

    public void RestartButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitButton()
    {
        // Best practice to handle quitting correctly in both the Editor and a built game.
#if UNITY_EDITOR
            // Exits Play mode in the Unity Editor
            UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quits the application when running as a built executable
        Debug.Log("Quit function called"); // Check the Console for this message
        Application.Quit();
#endif
        // MAKE SURE THAT YOU HAVE AN EVENT SYSTEM IN THE SCENE OF THE CANVAS SO THAT WAY THE BUTTON INTAKE CAN BE REGISTERED BY THE SYSTEM.
    }
}
