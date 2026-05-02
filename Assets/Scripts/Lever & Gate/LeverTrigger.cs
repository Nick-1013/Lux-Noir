using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class LeverTrigger : MonoBehaviour
{
    [Header("Interaction")]
    public UnityEvent onPressed;                  // Action to perform (assign in Inspector)
    public bool oneUse = true;                    // Disable after first successful use
    public GameObject promptUI;                   // Optional prompt (e.g. "Press E") to show while in range

    [Header("Prompt Positioning")]
    public Vector2 promptScreenOffset = new(0f, 48f); // pixel offset from lever position (screen space)
    public float promptWorldOffsetY = 1.0f;                   // Y offset when promptUI is in world-space

    [Header("Animator")]
    public Animator leverAnimator;               // Optional animator for the lever
    public string pressTrigger = "Press";         // Animator trigger parameter name

    [Header("Audio")]
    public AudioClip pressClip;                   // Optional per-lever clip
    public bool playAtObjectPosition = true;      // Play clip at object (3D) or camera (2D)
    public AudioManager audioManager;             // Optional scene AudioManager fallback

    // internal state
    private bool playerInRange = false;
    private bool used = false;

    void Reset()
    {
        // sensible defaults when adding the component
        pressTrigger = "Press";
        playAtObjectPosition = true;
    }

    void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Update()
    {
        // Keep the prompt positioned above the lever while player is in range
        if (playerInRange && promptUI != null && promptUI.activeSelf)
        {
            UpdatePromptPosition();
        }

        if (!playerInRange || used) return;

        // Check for E key using the new Input System; fallback to KeyCode if Keyboard.current is null
        if ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
            (Keyboard.current == null && Input.GetKeyDown(KeyCode.E)))
        {
            Activate();
        }
    }

    private void Activate()
    {
        // Play animator trigger
        if (leverAnimator != null && !string.IsNullOrEmpty(pressTrigger))
        {
            leverAnimator.SetTrigger(pressTrigger);
        }

        // Play audio
        PlayPressAudio();

        // Invoke assigned event
        onPressed?.Invoke();

        // If single use, mark and hide prompt
        if (oneUse)
        {
            used = true;
            if (promptUI != null)
                promptUI.SetActive(false);
        }
    }

    private void PlayPressAudio()
    {
        Vector3 playPos = (Camera.main != null && !playAtObjectPosition) ? Camera.main.transform.position : transform.position;

        if (pressClip != null)
        {
            AudioSource.PlayClipAtPoint(pressClip, playPos);
            return;
        }

        if (audioManager != null)
        {
            if (audioManager.audioClip != null)
            {
                AudioSource.PlayClipAtPoint(audioManager.audioClip, playPos);
            }
            else
            {
                audioManager.PlayAudio();
            }
            return;
        }

        // No audio assigned — optional: silently ignore.
        Debug.Log("[LeverTrigger] No pressClip or AudioManager assigned for lever: " + gameObject.name);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (promptUI != null)
            {
                promptUI.SetActive(true);
                // immediately position the prompt
                UpdatePromptPosition();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (promptUI != null)
                promptUI.SetActive(false);
        }
    }

    // Optional: public helper so other scripts (or animation events) can trigger the lever without needing the player input or range check. Respects oneUse setting.
    public void ForceActivate()
    {
        if (used) return;
        Activate();
    }

    // Update the prompt position to sit above this lever.
    // Supports Screen Space - Overlay, Screen Space - Camera and World Space canvases.
    private void UpdatePromptPosition()
    {
        if (promptUI == null) return;

        if (!promptUI.TryGetComponent<RectTransform>(out var promptRect))
        {
            // Not a UI element; treat as world-space object and position above lever
            promptUI.transform.position = transform.position + Vector3.up * promptWorldOffsetY;
            return;
        }

        // Determine canvas render mode and position accordingly
        Canvas canvas = promptRect.GetComponentInParent<Canvas>();
        Camera cam = Camera.main;

        Vector3 worldPos = transform.position;
        Vector3 screenPoint = cam != null ? cam.WorldToScreenPoint(worldPos) : Vector3.zero;

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            // place prompt in world-space above lever
            Vector3 worldOffset = Vector3.up * promptWorldOffsetY;
            promptUI.transform.position = transform.position + worldOffset;
        }
        else
        {
            // screen-space (Overlay or Camera) - convert world point to canvas space
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
            {
                // use canvas camera if assigned
                screenPoint = canvas.worldCamera.WorldToScreenPoint(worldPos);
            }

            // For ScreenSpace overlay and ScreenSpaceCamera, placing RectTransform.position to screen point works
            Vector3 targetScreenPos = screenPoint + (Vector3)promptScreenOffset;

            // If canvas is in Overlay or Camera modes, set anchored position using RectTransformUtility
            RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreenPos, canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null, out Vector2 localPoint))
            {
                promptRect.localPosition = localPoint;
            }
            else
            {
                // fallback: set world position based on screen point
                promptRect.position = targetScreenPos;
            }
        }
    }
}
