using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class PlayerMovement : MonoBehaviour
{
    private GameManagerScript gameManager;

    [Header("Movement")]
    public float speed = 5.0f;
    public float runMultiplier = 1.75f;
    public float jumpForce = 10.0f;

    [Header("Variable Jump")]
    public float jumpCutMultiplier = 0.5f;

    private float moveDirection;
    private Rigidbody2D rb;

    private bool isGrounded;
    private int availableJumps;

    // ---------------- WALL SYSTEM ----------------
    [Header("Wall")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;
    public LayerMask wallLayer;

    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;

    [Header("Wall Jump")]
    public Vector2 wallJumpForce = new Vector2(8f, 12f);

    // ---------------- COLOR SWAP ----------------
    [Header("Color Swap")]
    public SpriteRenderer spriteRenderer;
    public Sprite normalSprite;
    public Sprite altSprite;

    private bool isAltColor = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        availableJumps = 1;
        gameManager = FindFirstObjectByType<GameManagerScript>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (transform.position.y < -10f)
        {
            gameManager.GameOver();
            enabled = false;
        }

        Gamepad currentGamepad = InputSystem.devices.OfType<Gamepad>().FirstOrDefault();

        moveDirection = GetHorizontalInput(currentGamepad);

        if (Mathf.Abs(moveDirection) < 0.1f) moveDirection = 0f;

        float currentSpeed = speed;

        if (IsRunHeld(currentGamepad))
            currentSpeed *= runMultiplier;

        // ---------------- WALL CHECK ----------------
        CheckWall();

        // -------- PREVENT CONTROL DURING WALL JUMP --------
        if (!isWallJumping)
        {
            float horizontalVelocity = moveDirection * currentSpeed;

            // PREVENT PUSHING INTO WALL
            if (isTouchingWall && !isGrounded)
            {
                if (Mathf.Sign(moveDirection) == transform.localScale.x)
                {
                    horizontalVelocity = 0f;
                }
            }

            rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
        }

        // Flip player (only if not wall jumping)
        if (!isWallJumping)
        {
            if (moveDirection > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (moveDirection < 0)
                transform.localScale = new Vector3(-1, 1, 1);
        }

        // ---------------- WALL ----------------
        HandleWallSlide();
        HandleWallJump(currentGamepad);

        // ---------------- NORMAL JUMP ----------------
        if (IsJumpPressed(currentGamepad) && isGrounded && availableJumps > 0)
        {
            Jump();
        }

        // VARIABLE JUMP RELEASE
        if (IsJumpReleased(currentGamepad))
        {
            CutJump();
        }

        // ---------------- COLOR SWAP ----------------
        if (IsColorSwapPressed(currentGamepad))
        {
            ToggleColor();
        }
    }

    // =========================================================
    // JUMP
    // =========================================================
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        availableJumps--;
    }

    private void CutJump()
    {
        if (rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );
        }
    }

    // =========================================================
    // WALL LOGIC
    // =========================================================
    void CheckWall()
    {
        isTouchingWall = Physics2D.Raycast(
            transform.position,
            Vector2.right * transform.localScale.x,
            wallCheckDistance,
            wallLayer
        );

        Debug.DrawRay(transform.position, Vector2.right * transform.localScale.x * wallCheckDistance, Color.red);
    }

    void HandleWallSlide()
    {
        // Require input to slide (prevents sticky idle cling)
        isWallSliding = isTouchingWall && !isGrounded && rb.linearVelocity.y < 0 && moveDirection != 0;

        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);
        }
    }

    void HandleWallJump(Gamepad currentGamepad)
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (IsJumpPressed(currentGamepad) && wallJumpingCounter > 0f)
        {
            isWallJumping = true;

            rb.linearVelocity = new Vector2(
                wallJumpingDirection * wallJumpForce.x,
                wallJumpForce.y
            );

            wallJumpingCounter = 0f;

            // EXTRA PUSH OFF WALL (anti-stick)
            rb.AddForce(new Vector2(wallJumpingDirection * 2f, 0f), ForceMode2D.Impulse);

            // Flip player
            if (transform.localScale.x != wallJumpingDirection)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1f;
                transform.localScale = scale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    void StopWallJumping()
    {
        isWallJumping = false;
    }

    // =========================================================
    // COLOR SWAP
    // =========================================================
    void ToggleColor()
    {
        isAltColor = !isAltColor;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isAltColor ? altSprite : normalSprite;
        }
    }

    // =========================================================
    // INPUT
    // =========================================================
    private float GetHorizontalInput(Gamepad currentGamepad)
    {
        float gamepadInput = 0f;
        float keyboardInput = 0f;

        if (currentGamepad != null)
            gamepadInput = currentGamepad.leftStick.x.ReadValue();

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                keyboardInput -= 1f;

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                keyboardInput += 1f;
        }

        return Mathf.Abs(gamepadInput) > 0.1f ? gamepadInput : keyboardInput;
    }

    private bool IsJumpPressed(Gamepad currentGamepad)
    {
        return (currentGamepad != null && currentGamepad.aButton.wasPressedThisFrame) ||
               (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);
    }

    private bool IsJumpReleased(Gamepad currentGamepad)
    {
        return (currentGamepad != null && currentGamepad.aButton.wasReleasedThisFrame) ||
               (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame);
    }

    private bool IsColorSwapPressed(Gamepad currentGamepad)
    {
        return (currentGamepad != null && currentGamepad.xButton.wasPressedThisFrame) ||
               (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
    }

    private bool IsRunHeld(Gamepad currentGamepad)
    {
        return (currentGamepad != null &&
               (currentGamepad.leftStickButton.isPressed || currentGamepad.rightTrigger.ReadValue() > 0.1f)) ||
               (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);
    }

    // =========================================================
    // GROUND COLLISION
    // =========================================================
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            availableJumps = 1;
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}