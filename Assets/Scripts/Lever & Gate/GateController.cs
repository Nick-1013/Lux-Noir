using UnityEngine;

public class GateController : MonoBehaviour
{
    [Header("Target")]
    public GameObject targetToDisable;   // GameObject that will be disabled when triggered

    [Header("Audio")]
    public AudioClip clip;               // Optional per-gate clip
    public bool playAtObjectPosition = true; // true => play at gate position, false => play at camera position
    public AudioManager audioManager;    // Optional fallback AudioManager

    [Header("Behaviour")]
    public bool oneUse = true;           // If true, subsequent triggers are ignored

    bool used = false;

    // Public API: call this from LeverTrigger.onPressed or other scripts
    public void Trigger()
    {
        if (oneUse && used) return;

        // Play configured audio BEFORE disabling the target
        PlayAudio();

        // Disable the target GameObject (safe null-check)
        if (targetToDisable != null)
            targetToDisable.SetActive(false);
        else
            Debug.LogWarning($"GateController on '{gameObject.name}' has no targetToDisable assigned.");

        used = true;
    }

    void PlayAudio()
    {
        Vector3 playPos = (Camera.main != null && !playAtObjectPosition) ? Camera.main.transform.position : transform.position;

        if (clip != null)
        {
            // PlayClipAtPoint creates a temporary audio source so playback continues even if objects are disabled.
            AudioSource.PlayClipAtPoint(clip, playPos);
            return;
        }

        // fallback to AudioManager
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

        // No audio configured — noop (silent)
    }

    // Optional helper to reset the gate for testing
    public void ResetGate()
    {
        if (targetToDisable != null)
            targetToDisable.SetActive(true);
        used = false;
    }
}
