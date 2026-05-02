using UnityEngine; // Core Unity engine functionality
using UnityEngine.UI; // Required for UI elements like health bars

public class Health : MonoBehaviour
{
    // ---------------- BASIC HEALTH SETTINGS ----------------

    public float maxHealth = 100f; // Maximum health value for this object

    [SerializeField] private float currentHealth; // Current health (hidden but visible in Inspector)

    public bool isPlayer = false; // Determines if this Health belongs to player or enemy

    // ---------------- REFERENCES ----------------

    private Enemy enemy; // Reference to Enemy script (used if this is an enemy)
    private PlayerMovement player; // Reference to Player script (used if this is the player)

    [SerializeField] private ImageController healthUI; // Reference to UI health display (for player)

    [SerializeField] private SpriteRenderer spriteRenderer; // Used for flash effect when damaged

    private Animator animator; // Animator for hurt/death animations

    // ---------------- DAMAGE / INVULNERABILITY ----------------

    public float invulnerabilityTime = 0.5f; // Time after hit where damage is ignored

    private float invulTimer; // Tracks remaining invulnerability time

    // ---------------- STATE ----------------

    private bool isDead = false; // Tracks if this entity is already dead

    // ---------------- INITIALIZATION ----------------

    void Start()
    {
        currentHealth = maxHealth; // Set starting health to max

        animator = GetComponent<Animator>(); // Get Animator component if it exists

        // If no SpriteRenderer assigned manually, try to get one
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Get BOTH components safely (instead of depending on isPlayer logic)
        enemy = GetComponent<Enemy>(); // Will be null if not an enemy
        player = GetComponent<PlayerMovement>(); // Will be null if not player
    }

    // ---------------- UPDATE LOOP ----------------

    void Update()
    {
        // If dead, ensure visuals are reset and stop further logic
        if (isDead)
        {
            ResetVisual(); // Restore sprite visibility
            return; // Exit Update early
        }

        // If currently invulnerable, reduce timer and flash sprite
        if (invulTimer > 0)
        {
            invulTimer -= Time.deltaTime; // Decrease timer over time

            FlashEffect(); // Apply flashing effect
        }
        else
        {
            ResetVisual(); // Restore normal appearance
        }
    }

    // ---------------- VISUAL EFFECTS ----------------

    void FlashEffect()
    {
        if (spriteRenderer == null) return; // Safety check

        Color color = spriteRenderer.color; // Get current sprite color

        // Creates a pulsing transparency effect
        float alpha = Mathf.PingPong(Time.time * 5f, 1f);

        color.a = alpha; // Apply changing alpha
        spriteRenderer.color = color; // Set updated color
    }

    void ResetVisual()
    {
        if (spriteRenderer == null) return; // Safety check

        Color color = spriteRenderer.color; // Get current color

        color.a = 1f; // Reset alpha to fully visible

        spriteRenderer.color = color; // Apply reset color
    }

    // ---------------- DAMAGE HANDLING ----------------

    public void TakeDamage(float amount)
    {
        Debug.Log("Health script hit on: " + gameObject.name); // Log which object was hit

        // Ignore damage if currently invulnerable or already dead
        if (invulTimer > 0 || isDead) return;

        currentHealth -= amount; // Subtract damage from health

        // Clamp health to stay between 0 and maxHealth
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        invulTimer = invulnerabilityTime; // Reset invulnerability timer

        // Trigger hurt animation for both player and enemy (was only enemies)
        if (animator != null)
        {
            animator.SetTrigger("IsHurt");
        }

        // Update UI ONLY if this is the player
        if (healthUI != null)
        {
            healthUI.TakeDamage((int)amount);
        }

        // Debug logs to clearly separate player vs enemy damage
        if (isPlayer)
        {
            Debug.Log("[Player] Took damage: " + amount + " | HP: " + currentHealth);
        }
        else
        {
            Debug.Log("[Enemy] Took damage: " + amount + " | HP: " + currentHealth);
        }

        // If health reaches zero, trigger death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ---------------- HEALING ----------------

    public void Heal(float amount)
    {
        currentHealth += amount; // Add health

        // Clamp to prevent overhealing beyond max
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Update UI if this is the player
        if (isPlayer && healthUI != null)
        {
            healthUI.Heal((int)amount);
        }
    }

    // ---------------- DEATH HANDLING ----------------

    private void Die()
    {
        if (isDead) return; // Prevent multiple death calls

        isDead = true; // Mark as dead

        Debug.Log(gameObject.name + " has died!"); // Debug log

        // Trigger death animation if Animator exists
        if (animator != null)
        {
            animator.SetTrigger("IsDead");
        }

        // ---------------- PLAYER DEATH ----------------
        if (isPlayer)
        {
            if (player != null)
                player.enabled = false; // Disable player controls

            GameManagerScript gm = FindFirstObjectByType<GameManagerScript>(); // Find GameManager

            if (gm != null)
                gm.GameOver(); // Trigger game over
        }
        // ---------------- ENEMY DEATH ----------------
        else
        {
            if (enemy != null)
                enemy.Die(); // Call enemy-specific death logic
        }
    }

    // ---------------- OPTIONAL ANIMATION RESET ----------------

    void IsNotHurt()
    {
        // Reset hurt animation ONLY for enemies
        if (!isPlayer && animator != null)
        {
            animator.SetTrigger("IsNotHurt");
        }
    }
}