using UnityEngine;

public class PlayerDamageDealer : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            Health enemyHealth = collision.gameObject.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(1); // Adjust damage value as needed
                Debug.Log("Enemy hit! Remaining health: " + enemyHealth.maxHealth);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            Health enemyHealth = collision.gameObject.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(1); // Adjust damage value as needed
                Debug.Log("Enemy hit! Remaining health: " + enemyHealth.maxHealth);
            }
        }

        if (collision.gameObject.tag == "Mace")
        {
            Health playerHealth = collision.gameObject.GetComponent<Health>();
        }
    }
}
