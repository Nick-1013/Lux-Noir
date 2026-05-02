using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAudio : MonoBehaviour,
    ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public AudioSource audioSource;

    [Header("Sounds")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("Polish")]
    public float hoverCooldown = 0.1f; // Prevent rapid re-triggering

    private bool isHovered = false;
    private bool isSelected = false;
    private float lastHoverTime = 0f;

    // ---------------- POINTER ----------------
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Extra fix: don't double-trigger if already selected
        if (!isHovered && !isSelected)
        {
            PlayHover();
            isHovered = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    // ---------------- CONTROLLER / KEYBOARD ----------------
    public void OnSelect(BaseEventData eventData)
    {
        // Extra fix: don't double-trigger if already hovered
        if (!isSelected && !isHovered)
        {
            PlayHover();
            isSelected = true;
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }

    // ---------------- PLAY HOVER ----------------
    void PlayHover()
    {
        if (audioSource == null || hoverSound == null)
            return;

        // Optional polish: cooldown check
        if (Time.time - lastHoverTime < hoverCooldown)
            return;

        lastHoverTime = Time.time;
        audioSource.PlayOneShot(hoverSound);
    }

    // ---------------- CLICK ----------------
    public void PlayClick()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}