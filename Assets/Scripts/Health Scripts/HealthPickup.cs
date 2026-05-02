using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public float healAmount = 25f;
    public GameObject pickupCollectVFX; // assign prefab in Inspector

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Health health = collision.GetComponent<Health>();

        if (health != null && health.isPlayer)
        {
            health.Heal(healAmount);

            if (pickupCollectVFX != null)
            {
                // Instantiates VFX at pickup position. AutoDestroyVFX on the prefab will remove it.
                Instantiate(pickupCollectVFX, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}