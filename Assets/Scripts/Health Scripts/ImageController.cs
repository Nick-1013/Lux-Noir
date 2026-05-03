using UnityEngine;

public class ImageController : MonoBehaviour
{
    [Header("UI States")]
    public GameObject inactiveImage; // ability locked / not usable
    public GameObject activeImage;   // ability unlocked
    public GameObject inUseImage;    // OPTIONAL: when actively wall jumping

    private bool isUnlocked = false;
    private bool isInUse = false;

    // ---------------- INITIALIZE ----------------
    void Start()
    {
        UpdateUI();
    }

    // ---------------- PUBLIC API ----------------

    // Call when ability is unlocked
    public void SetUnlocked(bool value)
    {
        isUnlocked = value;
        UpdateUI();
    }

    // Call when ability is actively being used (wall jump happening)
    public void SetInUse(bool value)
    {
        isInUse = value;
        UpdateUI();
    }

    // ---------------- UI LOGIC ----------------
    void UpdateUI()
    {
        // Disable everything first
        if (inactiveImage != null) inactiveImage.SetActive(false);
        if (activeImage != null) activeImage.SetActive(false);
        if (inUseImage != null) inUseImage.SetActive(false);

        if (!isUnlocked)
        {
            // Show locked state
            if (inactiveImage != null)
                inactiveImage.SetActive(true);
        }
        else
        {
            if (isInUse && inUseImage != null)
            {
                // Show "currently using ability"
                inUseImage.SetActive(true);
            }
            else
            {
                // Show unlocked/ready state
                if (activeImage != null)
                    activeImage.SetActive(true);
            }
        }
    }
}