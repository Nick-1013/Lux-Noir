using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class LevelTransition : MonoBehaviour
{
    // Set this in the Inspector to the name or build index of your next level
    [SerializeField] private string nextLevelName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that entered the trigger is the player
        if (other.CompareTag("Player"))
        {
            // Load the next scene
            SceneManager.LoadScene(nextLevelName);
        }
    }
}
