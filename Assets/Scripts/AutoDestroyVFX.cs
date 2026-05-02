using System.Collections;
using UnityEngine;

public class AutoDestroyVFX : MonoBehaviour
{
    // Fallback time if no particle/animation info is found
    public float fallbackTime = 5f;

    void Start()
    {
        // If there are particle systems, wait until they're done
        var particleSystems = GetComponentsInChildren<ParticleSystem>();
        if (particleSystems != null && particleSystems.Length > 0)
        {
            StartCoroutine(WaitForParticlesAndDestroy(particleSystems));
            return;
        }

        // If an Animator with clips exists, wait for the longest clip length
        var animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            float maxLen = 0f;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null)
                    maxLen = Mathf.Max(maxLen, clip.length);
            }

            if (maxLen > 0f)
            {
                StartCoroutine(DestroyAfterSeconds(maxLen));
                return;
            }
        }

        // Default fallback
        Destroy(gameObject, fallbackTime);
    }

    IEnumerator WaitForParticlesAndDestroy(ParticleSystem[] systems)
    {
        // Wait until all particle systems are no longer alive (including children)
        bool anyAlive;
        do
        {
            anyAlive = false;
            foreach (var ps in systems)
            {
                if (ps != null && ps.IsAlive(true))
                {
                    anyAlive = true;
                    break;
                }
            }

            yield return null;
        } while (anyAlive);

        Destroy(gameObject);
    }

    IEnumerator DestroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }
}
