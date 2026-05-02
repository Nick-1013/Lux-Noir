using UnityEngine; // Gives access to Unity engine core features (physics, transforms, etc.)
using UnityEngine.InputSystem; // Enables use of the new Unity Input System
using System.Linq; // Allows use of LINQ (used here to find gamepad devices)
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour // Main player controller class
{
    // ---------------- GAME MANAGER ----------------
    private GameManagerScript gameManager; // Reference to GameManager for pause/game over logic

    // ---------------- MOVEMENT SETTINGS ----------------
    public float speed = 5.0f; // Base movement speed of the player
    public float runMultiplier = 1.75f; // Multiplier applied when running
    public float jumpForce = 10.0f; // Force applied when jumping
    public int maxJumps = 2; // Maximum number of jumps allowed (double jump)
    public float groundPoundForce = 25f; // Downward force for ground pound

    // ---------------- MOVEMENT STATE ----------------
    private float moveDirection; // Stores horizontal input (-1 to 1)
    private Rigidbody2D rb; // Reference to Rigidbody2D for physics movement
    private Animator animator; // Reference to Animator for animations
    private bool isGrounded; // Tracks if player is touching the ground
    private int availableJumps; // Tracks how many jumps remain
    private bool isGroundPounding; // Tracks if player is currently ground pounding

    // ---------------- MOVEMENT AUDIO ----------------
    [Header("Movement Audio")]
    public AudioClip moveClip; // looping movement sound
    [Range(0f, 1f)]
    public float moveVolume = 1f;
    public bool spatialMove = false;

    private AudioSource moveAudioSource;

    // ---------------- ATTACK SETTINGS ----------------
    [Header("Attack")] // Creates a header in the Unity Inspector
    public float attackRange = 2.5f; // Radius of attack hit detection
    public int attackDamage = 1; // Damage dealt per attack
    public Transform attackPoint;

    public LayerMask enemyLayer; // Layer mask to specify which layers are considered enemies for attack detection

    // ---------------- ATTACK AUDIO ----------------
    [Header("Attack Audio")]
    public AudioClip attackClip; // clip to play on attack keyframe (assign in Inspector)
    public bool spatialAttack = false; // true -> play at player position, false -> play at camera (2D)

    // ---------------- HURT AUDIO ----------------
    [Header("Hurt Audio")]
    public AudioClip hurtClip; // clip to play when player is hurt
    public bool spatialHurt = false; // true -> play at player position, false -> play at camera

    // ---------------- PICKUP SYSTEM ----------------
    [Header("Pickups")]
    public CoinManager coinManager;        // Tracks total coins collected
    public AudioManager audioManager;      // Handles audio playback
    public GameObject pickupCollectVFX;    // Visual effect when picking up items

    // ---------------- UNITY START ----------------
    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); // Get Rigidbody2D component
        animator = GetComponent<Animator>(); // Get Animator component
        availableJumps = maxJumps; // Initialize jump count

        gameManager = FindFirstObjectByType<GameManagerScript>(); // Find GameManager in scene

        // Setup movement audio
        if (moveClip != null)
        {
            moveAudioSource = GetComponent<AudioSource>();
            if (moveAudioSource == null)
            {
                moveAudioSource = gameObject.AddComponent<AudioSource>();
                moveAudioSource.playOnAwake = false;
            }

            moveAudioSource.clip = moveClip;
            moveAudioSource.loop = true;
            moveAudioSource.volume = moveVolume;
            moveAudioSource.spatialBlend = spatialMove ? 1f : 0f;
        }
    }

    // ---------------- MAIN UPDATE LOOP ----------------
    void Update()
    {
        if (Time.timeScale == 0f) return; // Stop input when game is paused

        if (Time.timeScale == 0f)
        {
            UpdateMoveAudio(false);
            return;
        }

        // Get current connected gamepad (if any)
        Gamepad currentGamepad = InputSystem.devices.OfType<Gamepad>().FirstOrDefault();

        // Get horizontal movement input (keyboard or gamepad)
        moveDirection = GetHorizontalInput(currentGamepad);

        // Apply deadzone to prevent small unwanted movement
        if (Mathf.Abs(moveDirection) < 0.1f) moveDirection = 0f;

        float currentSpeed = speed; // Start with base speed

        // Apply run multiplier if run input is held
        if (IsRunHeld(currentGamepad))
        {
            currentSpeed *= runMultiplier;
        }

        // Only allow movement if NOT ground pounding
        if (!isGroundPounding)
        {
            // Apply horizontal velocity while keeping vertical velocity unchanged
            rb.linearVelocity = new Vector2(moveDirection * currentSpeed, rb.linearVelocity.y);

            // Flip character based on movement direction
            if (moveDirection > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (moveDirection < 0)
                transform.localScale = new Vector3(-1, 1, 1);
        }

        // Update animation states
        UpdateAnimations();

        // Update movement audio based on whether player is moving and not ground pounding
        {
            if (animator == null) return;

            float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);
            bool isMoving = horizontalSpeed > 0.1f && !isGroundPounding;

            animator.SetBool("IsMoving", isMoving);

            UpdateMoveAudio(isMoving);
        }

        // -------- HURT AUDIO WATCHER --------
        // Play hurt sound on rising edge of Animator "IsHurt" bool, if the parameter exists.
        if (animator != null && AnimatorHasParameter("IsHurt"))
        {
            bool animHurt = animator.GetBool("IsHurt");
            // track previous state using a static local field emulated with PlayerPrefs? No — use a private field.
            // (prevAnimHurt declared below)
            if (animHurt && !prevAnimHurt)
            {
                PlayHurtSoundEvent();
            }
            prevAnimHurt = animHurt;
        }

        // -------- GROUND POUND CHECK --------
        // Only allow ground pound while in air and not already doing it
        if (!isGrounded && !isGroundPounding && IsGroundPoundPressed(currentGamepad))
        {
            GroundPound(); // Execute ground pound
            return; // Skip rest of update this frame
        }

        // -------- JUMP CHECK --------
        // Only jump if player has jumps remaining
        if (IsJumpPressed(currentGamepad) && availableJumps > 0)
        {
            Jump(); // Perform jump
        }

        // -------- ATTACK INPUT --------
        // Check for mouse click (left click)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            AttackAnimation(); // Trigger attack
        }


        // -------- DEBUG: RELOAD SCENE --------
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
        else if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // ---------------- ANIMATION HANDLER ----------------
    void UpdateAnimations()
    {
        if (animator == null) return; // Safety check

        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x); // Get horizontal movement speed

        bool isMoving = horizontalSpeed > 0.1f; // True if moving horizontally

        // Apply animation parameters
        animator.SetBool("IsMoving", isMoving);
    }

    // ---------------- MOVEMENT AUDIO HANDLER ----------------
    void UpdateMoveAudio(bool isMoving)
    {
        if (moveAudioSource == null) return; // No audio source to play sound
        if (isMoving && !moveAudioSource.isPlaying)
        {
            moveAudioSource.Play(); // Start movement sound
        }

        // Smooth volume transition
        float targetVolume = isMoving ? moveVolume : 0f;

        moveAudioSource.volume = Mathf.Lerp(
            moveAudioSource.volume,
            targetVolume,
            Time.deltaTime * 10f
        );
        if (!isMoving && moveAudioSource.isPlaying)
        {
            moveAudioSource.Stop(); // Stop movement sound
        }
    }

    // ---------------- INPUT HANDLING ----------------
    private float GetHorizontalInput(Gamepad currentGamepad)
    {
        float gamepadInput = 0f; // Store gamepad input
        float keyboardInput = 0f; // Store keyboard input

        // -------- GAMEPAD INPUT --------
        if (currentGamepad != null)
        {
            gamepadInput = currentGamepad.leftStick.x.ReadValue(); // Read left stick horizontal value
        }

        // -------- KEYBOARD INPUT --------
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                keyboardInput -= 1f; // Move left

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                keyboardInput += 1f; // Move right
        }

        // Prefer gamepad input if it's being used
        if (Mathf.Abs(gamepadInput) > 0.1f)
            return gamepadInput;

        return keyboardInput; // Otherwise use keyboard
    }

    // ---------------- JUMP LOGIC ----------------
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vertical velocity
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse); // Apply upward force

        isGrounded = false; // Immediately mark as airborne
        availableJumps--; // Reduce available jumps
    }

    // ---------------- GROUND POUND ----------------
    private void GroundPound()
    {
        isGroundPounding = true; // Mark as ground pounding

        rb.linearVelocity = Vector2.zero; // Stop all current motion
        rb.AddForce(Vector2.down * groundPoundForce, ForceMode2D.Impulse); // Slam downward
    }

    // ---------------- INPUT CHECKS ----------------
    private bool IsJumpPressed(Gamepad currentGamepad)
    {
        bool gamepadJump = currentGamepad != null && currentGamepad.aButton.wasPressedThisFrame; // Gamepad A button
        bool keyboardJump = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame; // Space key

        return gamepadJump || keyboardJump; // Return true if either pressed
    }

    private bool IsGroundPoundPressed(Gamepad currentGamepad)
    {
        bool gamepadPound =
            currentGamepad != null &&
            currentGamepad.leftStick.y.ReadValue() < -0.5f && // Stick pushed down
            currentGamepad.aButton.wasPressedThisFrame; // Jump pressed

        bool keyboardPound =
            Keyboard.current != null &&
            (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) && // Down input
            Keyboard.current.spaceKey.wasPressedThisFrame; // Jump pressed

        return gamepadPound || keyboardPound; // Return true if either triggered
    }

    private bool IsRunHeld(Gamepad currentGamepad)
    {
        bool gamepadRun =
            currentGamepad != null &&
            (currentGamepad.leftStickButton.isPressed || currentGamepad.rightTrigger.ReadValue() > 0.1f); // Run input

        bool keyboardRun =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed; // Shift key

        return gamepadRun || keyboardRun; // Return true if either held
    }

    // ---------------- COLLISION HANDLING ----------------
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) // Check if collided with ground
        {
            if (isGroundPounding)
            {
                rb.AddForce(Vector2.up * 2f, ForceMode2D.Impulse); // Small bounce after ground pound
            }

            availableJumps = maxJumps; // Reset jumps
            isGrounded = true; // Mark as grounded
            isGroundPounding = false; // Reset ground pound state
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false; // Mark as airborne
        }
    }

    // ---------------- PICKUP HANDLING ----------------
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("CoinCollection"))
        {
            // -------- PLAY SOUND --------
            if (audioManager != null && audioManager.audioClip != null)
            {
                // Play at camera position so it's always audible like UI sound
                Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
                AudioSource.PlayClipAtPoint(audioManager.audioClip, playPos);
            }
            else if (audioManager != null)
            {
                audioManager.PlayAudio(); // fallback
            }

            // -------- SPAWN VFX --------
            if (pickupCollectVFX != null)
            {
                Instantiate(pickupCollectVFX, other.transform.position, Quaternion.identity);
            }

            // -------- DESTROY COIN --------
            Destroy(other.gameObject);

            // -------- ADD TO COIN COUNT --------
            if (coinManager != null)
            {
                coinManager.coinCount++;
            }
        }
    }

    // ---------------- ATTACK SYSTEM ----------------
    void Attack() // FIXED: removed unnecessary parameter
    {
        Debug.Log("[Player] ATTACK!"); // Debug log for attack trigger

        // Detect all colliders within attack range
        Vector2 attackCenter = attackPoint.position;

        // Draw this always
        Debug.DrawLine(transform.position, attackCenter, Color.red, 1f);
        Debug.DrawRay(attackCenter, Vector2.up * attackRange, Color.green, 1f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRange, enemyLayer);
        Debug.Log("Hits found: " + hits.Length);
        Debug.Log("Enemy LayerMask value: " + enemyLayer.value);

        // Loop through all detected colliders
        foreach (Collider2D col in hits)
        {
            Debug.Log("Hit object: " + col.name);

            hits[0].GetComponent<Health>().TakeDamage(attackDamage);


            /*
            // Only apply damage if enemy was found
            if (health != null && !health.isPlayer)
            {
                Debug.Log("Applying damage to: " + health.gameObject.name);
                health.TakeDamage(attackDamage);
            }
            */
        }
    }
    void AttackAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Attack"); // Trigger attack animation
    }

    // Public method for Animation Event — call this from the attack animation keyframe
    public void PlayAttackSoundEvent()
    {
        Vector3 playPos = spatialAttack ? transform.position : (Camera.main != null ? Camera.main.transform.position : transform.position);

        if (attackClip != null)
        {
            AudioSource.PlayClipAtPoint(attackClip, playPos);
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
        }
    }

    public void PlayHurtSoundEvent()
    {
        Vector3 playPos = spatialHurt ? transform.position : (Camera.main != null ? Camera.main.transform.position : transform.position);

        if (hurtClip != null)
        {
            AudioSource.PlayClipAtPoint(hurtClip, playPos);
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
        }
    }

    void OnDisable()
    {
        UpdateMoveAudio(false);
    }

    // ---------------- PRIVATE HELPERS ----------------
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
    // previous animator hurt state
    private bool prevAnimHurt = false;
}


// ***************  THIS IS THE END OF THE CODE  ************************


// ***************  KEY TERMS  ************************
//  variable
//  inspector
//  declaring
//  initializing
//  public
//  private
//  debug.log
//  string
//  float
//  integer (aka 'int')
//  GameObject
//  Input
//  KeyCode
//  string
//  Rigidbody2D
//  Vector2
//  Vector3
//  ||
//  &&
//  ++
//  *
//  ==
//  =
// !=






// ***************  IGNORE EVERYTHING BELOW THIS LINE!  ************************