using UnityEngine;

public class ImageController : MonoBehaviour
{
    // Order them in the inspector like this:
    // 0 = Empty HP
    // 1 = 1 HP
    // 2 = 2 HP
    // 3 = 3 HP
    // 4 = 4 HP

    public GameObject[] imageObjects;

    public int maxHealth = 4;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateImages();
    }

    // Call when player takes damage
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateImages();
    }

    // Call when player heals
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateImages();
    }

    void UpdateImages()
    {
        // If the HP bar itself is disabled, do nothing
        if (!gameObject.activeInHierarchy)
            return;

        // Disable all images first
        for (int i = 0; i < imageObjects.Length; i++)
        {
            imageObjects[i].SetActive(false);
        }

        // Enable only the image matching the current health
        if (currentHealth >= 0 && currentHealth < imageObjects.Length)
        {
            imageObjects[Mathf.Clamp(currentHealth, 0, imageObjects.Length - 1)].SetActive(true);
        }
    }
}