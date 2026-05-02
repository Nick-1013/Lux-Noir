using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip audioClip;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogWarning("AudioManager: No AudioSource assigned or found on the GameObject.", this);
        }

        if (audioClip == null)
        {
            Debug.LogWarning("AudioManager: No AudioClip assigned.", this);
        }
    }

    public void PlayAudio()
    {
        if (audioSource == null || audioClip == null)
        {
            Debug.LogWarning("AudioManager.PlayAudio() skipped because AudioSource or AudioClip is null.", this);
            return;
        }

        audioSource.PlayOneShot(audioClip);
    }
}
