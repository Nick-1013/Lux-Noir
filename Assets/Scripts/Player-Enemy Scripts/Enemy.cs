using UnityEngine; // Provides access to Unity engine core functionality

public class Enemy : MonoBehaviour // Enemy behavior script attached to enemy GameObject
{
    // ---------------- STATE MACHINE ----------------
    public enum EnemyState // Defines all possible enemy states
    {
        Idle,   // Enemy stands still
        Chase,  // Enemy moves toward player
        Attack  // Enemy attacks player
    }

    // ---------------- STATS ----------------
    [Header("Stats")] // Inspector header for stats
    public int damage = 1; // Damage dealt to player per attack
    public float attackCooldown = 2f; // Time between attacks

    // ---------------- MOVEMENT ----------------
    [Header("Movement")] // Inspector header for movement settings
    public float moveSpeed = 2f; // Movement speed of enemy
    public float detectionRange = 5f; // Distance at which enemy starts chasing
    public float attackRange = 1.5f; // Distance at which enemy attacks

    // ---------------- REFERENCES ----------------
    [Header("References")] // Inspector header for references
    public Transform player; // Reference to player transform

    // Audio: optional AudioManager or per-enemy AudioClip
    [Header("Audio")]
    public AudioManager audioManager; // optional, will be auto-found if left null
    public AudioClip attackClip; // optional per-enemy override clip

    [Header("Death Audio")]
    public AudioClip deathClip; // clip to play when enemy dies
    public bool spatialDeath = true; // true -> play at enemy position, false -> play at camera position

    [Header("Hurt Audio")]
    public AudioClip hurtClip; // clip to play when enemy is hurt
    public bool spatialHurt = true; // play at enemy position when true, otherwise at camera

    // Walking audio (plays while IsWalking is active)
    [Header("Walking Audio")]
    public AudioClip walkClip; // assign a looping walk sound in Inspector
    [Range(0f, 1f)]
    public float walkVolume = 1f;
    public bool spatialWalk = true; // true = 3D (enemy position), false = 2D (camera)
    private AudioSource walkAudioSource;

    private Rigidbody2D rb; // Rigidbody for movement physics
    private Animator animator; // Animator for handling animations
    private GameManagerScript gameManager; // Reference to game manager
    private Health playerHealth; // Reference to player's health script

    // ---------------- PROMPT UI ----------------
    [Header("Prompt UI")]
    public GameObject promptUI;                       // Optional prompt (e.g. "Press E" or "!" above enemy)
    public Vector2 promptScreenOffset = new(0f, 48f); // pixel offset from enemy position (screen space)
    public float promptWorldOffsetY = 1.0f;           // Y offset when promptUI is in world-space
    private bool promptVisible = false;

    // ---------------- INTERNAL STATE ----------------
    private EnemyState currentState; // Current AI state
    private float attackTimer; // Timer controlling attack cooldown
    private bool isDead; // Tracks whether enemy is dead

    // prevent playing death sound multiple times
    private bool deathSoundPlayed = false;

    // ---------------- UNITY START ----------------
    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); // Get Rigidbody2D component
        animator = GetComponent<Animator>(); // Get Animator component
        gameManager = FindFirstObjectByType<GameManagerScript>(); // Find GameManager in scene

        // Auto-find AudioManager if not assigned
        if (audioManager == null)
            audioManager = FindFirstObjectByType<AudioManager>();

        // Prepare walk audio source (per-enemy). Use existing AudioSource if present, otherwise add one if walkClip assigned.
        if (walkClip != null)
        {
            walkAudioSource = GetComponent<AudioSource>();
            if (walkAudioSource == null)
            {
                walkAudioSource = gameObject.AddComponent<AudioSource>();
                walkAudioSource.playOnAwake = false;
            }

            walkAudioSource.clip = walkClip;
            walkAudioSource.loop = true;
            walkAudioSource.volume = walkVolume;
            // spatialBlend 1 => 3D, 0 => 2D
            walkAudioSource.spatialBlend = spatialWalk ? 1f : 0f;
        }

        attackTimer = attackCooldown; // Initialize attack timer

        // -------- FIND PLAYER AUTOMATICALLY --------
        if (player == null) // If player not assigned in Inspector
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player"); // Find player by tag
            if (playerObj != null)
                player = playerObj.transform; // Assign player transform
        }

        // -------- CACHE PLAYER HEALTH --------
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>(); // Get player's Health script

            if (playerHealth == GetComponent<Health>())
            {
                Debug.LogError("Enemy is targeting itself as player!");
            }
        }

        // Ensure prompt UI is hidden initially
        if (promptUI != null)
            promptUI.SetActive(false);

        currentState = EnemyState.Idle; // Start in Idle state
        deathSoundPlayed = false;
        Debug.Log("[Enemy] Initialized."); // Debug log
    }

    // ---------------- MAIN UPDATE LOOP ----------------
    void Update()
    {
        if (isDead || player == null) return; // Stop logic if dead or no player

        float distance = Vector2.Distance(transform.position, player.position); // Calculate distance to player

        // -------- STATE MANAGEMENT --------
        if (distance <= attackRange) // If player is in attack range
        {
            if (currentState != EnemyState.Attack) // Only switch if not already attacking
            {
                currentState = EnemyState.Attack; // Switch to Attack state
                attackTimer = attackCooldown; // Reset attack timer
                //Debug.Log("[Enemy] Entering ATTACK state."); // Debug log
            }
        }
        else if (distance <= detectionRange) // If player is within detection range
        {
            if (currentState != EnemyState.Chase) // Only switch if not already chasing
            {
                currentState = EnemyState.Chase; // Switch to Chase state
                //Debug.Log("[Enemy] Entering CHASE state."); // Debug log
            }
        }
        else // If player is out of range
        {
            if (currentState != EnemyState.Idle) // Only switch if not already idle
            {
                currentState = EnemyState.Idle; // Switch to Idle state
                //Debug.Log("[Enemy] Entering IDLE state."); // Debug log
            }
        }

        HandleAttackTimer(distance); // Handle attack timing logic
        UpdateAnimations(); // Update animation states

        // Prompt UI: show while player within detectionRange (but not when dead)
        bool shouldShowPrompt = !isDead && distance <= detectionRange;
        if (shouldShowPrompt && !promptVisible)
        {
            ShowPrompt();
        }
        else if (!shouldShowPrompt && promptVisible)
        {
            HidePrompt();
        }

        // If visible, update its position each frame
        if (promptVisible)
        {
            UpdatePromptPosition();
        }

        // Watch Animator's "IsDead" bool and play death sound when it flips true (covers animation-driven death)
        if (animator != null && AnimatorHasParameter("IsDead"))
        {
            bool animDead = animator.GetBool("IsDead");
            if (animDead && !deathSoundPlayed)
            {
                PlayDeathSound();
                deathSoundPlayed = true;
            }
        }
    }

    // ---------------- FIXED UPDATE (PHYSICS) ----------------
    void FixedUpdate()
    {
        if (isDead) return; // Stop movement if dead

        switch (currentState) // Check current state
        {
            case EnemyState.Idle:
                StopMoving(); // Stop movement
                break;

            case EnemyState.Chase:
                ChasePlayer(); // Move toward player
                break;

            case EnemyState.Attack:
                StopMoving(); // Stop moving while attacking
                break;
        }
    }

    // ---------------- ATTACK TIMER ----------------
    void HandleAttackTimer(float distance)
    {
        if (currentState != EnemyState.Attack) return; // Only run if attacking

        attackTimer -= Time.deltaTime; // Decrease timer

        if (attackTimer <= 0) // If cooldown finished
        {
            if (distance <= attackRange) // Double-check player is still in range
            {
                Attack(); // Perform attack (starts animation)
            }
            else
            {
                Debug.Log("[Enemy] Player moved out of attack range."); // Debug log
            }

            attackTimer = attackCooldown; // Reset cooldown
        }
    }

    // ---------------- CHASE LOGIC ----------------
    void ChasePlayer()
    {
        float deltaX = player.position.x - transform.position.x; // Horizontal distance to player
        float direction = 0f; // Movement direction (-1 or 1)

        float flipThreshold = 0.2f; // Prevents jitter when very close

        if (Mathf.Abs(deltaX) > flipThreshold) // Only move if beyond threshold
        {
            direction = Mathf.Sign(deltaX); // Determine direction (-1 or 1)
        }

        // Move enemy manually toward player
        transform.position += new Vector3(direction * moveSpeed * Time.fixedDeltaTime, 0, 0);

        // Flip sprite to face movement direction
        if (direction != 0)
        {
            transform.localScale = new Vector3(direction, 1, 1);
        }

        // Trigger movement animation
        if (animator != null)
            animator.SetBool("IsWalking", true);
    }

    // ---------------- STOP MOVEMENT ----------------
    void StopMoving()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop horizontal velocity

        if (animator != null)
            animator.SetBool("IsWalking", false); // Stop movement animation
    }

    // ---------------- ATTACK ----------------
    void Attack()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > attackRange) return;

        if (animator != null)
            animator.SetTrigger("IsAttacking");

        // NOTE: damage is applied via Animation Event on Attack keyframe.
    }

    // Public entry for an Animation Event placed on the attack keyframe to play sound.
    public void PlayAttackSoundEvent()
    {
        PlayAttackSound();
    }

    // Public entry for an Animation Event placed on the attack keyframe to apply damage.
    // Add an Animation Event calling `DealAttackDamageEvent` at the frame you want damage to occur.
    public void DealAttackDamageEvent()
    {
        if (isDead || player == null || playerHealth == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > attackRange)
        {
            Debug.Log("[Enemy] Damage event skipped: player out of range.");
            return;
        }

        if (playerHealth.gameObject == gameObject) return; // safety

        Debug.Log("Enemy dealing damage to: " + playerHealth.gameObject.name);
        playerHealth.TakeDamage(damage);
    }

    // Play sound when enemy attacks. Priority:
    // 1) per-enemy attackClip
    // 2) assigned AudioManager.audioClip or AudioManager.PlayAudio()
    private void PlayAttackSound()
    {
        Vector3 playPosition = Camera.main != null ? Camera.main.transform.position : transform.position;

        if (attackClip != null)
        {
            AudioSource.PlayClipAtPoint(attackClip, playPosition);
            return;
        }

        if (audioManager != null)
        {
            // If AudioManager has a clip assigned, play it at camera position so 2D UI-like sound is audible
            if (audioManager.audioClip != null)
            {
                AudioSource.PlayClipAtPoint(audioManager.audioClip, playPosition);
            }
            else
            {
                // fallback to AudioManager's PlayAudio implementation (uses its own AudioSource)
                audioManager.PlayAudio();
            }
        }
    }

    // Play hurt sound. Called from Animator watcher or Animation Event.
    private void PlayHurtSound()
    {
        Vector3 playPosition = spatialHurt ? transform.position : (Camera.main != null ? Camera.main.transform.position : transform.position);

        if (hurtClip != null)
        {
            AudioSource.PlayClipAtPoint(hurtClip, playPosition);
        }
        else if (audioManager != null)
        {
            if (audioManager.audioClip != null)
                AudioSource.PlayClipAtPoint(audioManager.audioClip, playPosition);
            else
                audioManager.PlayAudio();
        }
    }

    // Public method for Animation Event to play hurt sound exactly on the hurt keyframe
    public void PlayHurtSoundEvent()
    {
        PlayHurtSound();
    }

    // Play death sound. Called from Die(), Animator watcher, or Animation Event.
    private void PlayDeathSound()
    {
        if (deathSoundPlayed) return;

        Vector3 playPosition = spatialDeath ? transform.position : (Camera.main != null ? Camera.main.transform.position : transform.position);

        if (deathClip != null)
        {
            AudioSource.PlayClipAtPoint(deathClip, playPosition);
        }
        else if (audioManager != null)
        {
            if (audioManager.audioClip != null)
                AudioSource.PlayClipAtPoint(audioManager.audioClip, playPosition);
            else
                audioManager.PlayAudio();
        }

        deathSoundPlayed = true;
    }

    // Public method for Animation Event to play death sound exactly on the death keyframe
    public void PlayDeathSoundEvent()
    {
        PlayDeathSound();
    }

    // ---------------- ANIMATION HANDLER ----------------
    void UpdateAnimations()
    {
        if (animator == null) return; // Safety check

        animator.SetBool("IsWalking", currentState == EnemyState.Chase); // True when chasing
        animator.SetBool("IsIdle", currentState == EnemyState.Idle); // True when idle
    }

    // Helper: check whether the attached Animator contains a parameter name
    private bool AnimatorHasParameter(string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    // ---------------- PROMPT UI HELPERS ----------------
    private void ShowPrompt()
    {
        if (promptUI == null) return;
        promptUI.SetActive(true);
        promptVisible = true;
        UpdatePromptPosition();
    }

    private void HidePrompt()
    {
        if (promptUI == null) return;
        promptUI.SetActive(false);
        promptVisible = false;
    }

    // Update the prompt position to sit above this enemy.
    // Supports Screen Space - Overlay, Screen Space - Camera and World Space canvases.
    private void UpdatePromptPosition()
    {
        if (promptUI == null) return;

        if (!promptUI.TryGetComponent<RectTransform>(out var promptRect))
        {
            // Not a UI element; treat as world-space object and position above enemy
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
            // place prompt in world-space above enemy
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

    // ---------------- EXTERNAL ATTACK STATE (OPTIONAL) ----------------
    public void SetAttackState(bool value)
    {
        if (animator != null)
        {
            animator.SetBool("IsAttacking", value); // Sets attack animation bool
        }
    }

    // ---------------- DEATH ----------------
    public void Die()
    {
        if (isDead) return; // Prevent multiple death calls

        isDead = true; // Mark as dead

        StopMoving(); // Stop all movement

        if (animator != null)
        {
            // set trigger and also (optionally) set bool so both animator setups are supported
            animator.SetTrigger("IsDead");
            if (AnimatorHasParameter("IsDead"))
                animator.SetBool("IsDead", true);
        }

        // Play death sound immediately (covers code-driven death)
        PlayDeathSound();

        // stop any walking audio immediately
        if (walkAudioSource != null && walkAudioSource.isPlaying)
            walkAudioSource.Stop();

        if (gameManager != null)
            gameManager.EnemyKilled(); // Notify GameManager

        Destroy(gameObject, 1.5f); // Destroy enemy after delay
    }
}