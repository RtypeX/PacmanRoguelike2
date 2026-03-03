using UnityEngine;

/// <summary>
/// PacmanController - Handles player movement, input, collisions, and state.
/// Attach to the Pacman GameObject. Requires a Rigidbody2D and Collider2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PacmanController : MonoBehaviour
{
    // ─── Inspector Fields ───────────────────────────────────────────────────

    [Header("Movement")]
    [Tooltip("Base movement speed. Increased by upgrades.")]
    public float moveSpeed = 5f;

    [Header("Lives")]
    [Tooltip("Starting lives. Upgradeable.")]
    public int maxLives = 3;

    [Header("Grid / Tile Settings")]
    [Tooltip("Size of one maze tile in world units.")]
    public float tileSize = 1f;

    [Tooltip("Layer mask for walls — used to check if a direction is passable.")]
    public LayerMask wallLayer;

    // ─── Runtime State ───────────────────────────────────────────────────────

    // Current lives remaining
    public int CurrentLives { get; private set; }

    // Whether Pacman is currently powered up (large pellet active)
    public bool IsPoweredUp { get; private set; }

    // Total points scored this run
    public int Score { get; private set; }

    // Fruit currency (unlocked via upgrade)
    public int FruitCurrency { get; private set; }

    // ─── Private Fields ───────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private Animator animator;

    // The direction the player is currently moving
    private Vector2 currentDirection = Vector2.zero;

    // The next direction the player wants to move (buffered input)
    private Vector2 queuedDirection = Vector2.zero;

    // Power pellet timer
    private float powerUpTimer = 0f;
    public float powerUpDuration = 8f; // upgradeable

    // Invincibility frames after being hit (prevents rapid multi-death)
    private float invincibilityTimer = 0f;
    private const float INVINCIBILITY_DURATION = 1.5f;

    // Animator parameter hashes (for performance)
    private static readonly int AnimDirX = Animator.StringToHash("DirX");
    private static readonly int AnimDirY = Animator.StringToHash("DirY");
    private static readonly int AnimPowered = Animator.StringToHash("IsPowered");

    // ─── Events (subscribe in GameManager, HUD, etc.) ─────────────────────

    public static event System.Action<int> OnScoreChanged;     // passes new score
    public static event System.Action<int> OnLivesChanged;     // passes remaining lives
    public static event System.Action OnPlayerDied;            // 0 lives → game over
    public static event System.Action OnPowerUpStart;
    public static event System.Action OnPowerUpEnd;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Rigidbody2D setup for top-down maze movement
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CurrentLives = maxLives;
    }

    private void Update()
    {
        HandleInput();
        UpdatePowerUpTimer();
        UpdateInvincibility();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Move();
    }

    // ─── Input ────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads arrow key (or WASD) input and buffers it as queuedDirection.
    /// The queued direction is applied as soon as the path is clear.
    /// </summary>
    private void HandleInput()
    {
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            queuedDirection = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            queuedDirection = Vector2.down;
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            queuedDirection = Vector2.left;
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            queuedDirection = Vector2.right;
    }

    // ─── Movement ─────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to apply the queued direction, falls back to current direction.
    /// Pacman cannot move through walls.
    /// </summary>
    private void Move()
    {
        // Try to turn into the queued direction first
        if (queuedDirection != Vector2.zero && CanMove(queuedDirection))
        {
            currentDirection = queuedDirection;
        }

        // Continue in currentDirection if possible
        if (currentDirection != Vector2.zero && CanMove(currentDirection))
        {
            rb.MovePosition(rb.position + currentDirection * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Hit a wall — stop smoothly
            rb.velocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Casts a small box in the desired direction to check for walls.
    /// Uses tileSize so it works at any scale.
    /// </summary>
    private bool CanMove(Vector2 direction)
    {
        // Offset the check origin slightly ahead of Pacman's center
        Vector2 origin = rb.position + direction * (tileSize * 0.5f);
        Vector2 size = Vector2.one * (tileSize * 0.4f); // slightly smaller than tile

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, direction, tileSize * 0.1f, wallLayer);
        return hit.collider == null;
    }

    // ─── Power-Up ─────────────────────────────────────────────────────────

    /// <summary>Call this when Pacman eats a large (power) pellet.</summary>
    public void ActivatePowerUp()
    {
        IsPoweredUp = true;
        powerUpTimer = powerUpDuration;
        OnPowerUpStart?.Invoke();
    }

    private void UpdatePowerUpTimer()
    {
        if (!IsPoweredUp) return;

        powerUpTimer -= Time.deltaTime;
        if (powerUpTimer <= 0f)
        {
            IsPoweredUp = false;
            OnPowerUpEnd?.Invoke();
        }
    }

    // ─── Scoring ──────────────────────────────────────────────────────────

    /// <summary>Add points (regular pellet, ghost eat, fruit, etc.).</summary>
    public void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    /// <summary>Add fruit currency (unlocked via upgrade).</summary>
    public void AddFruitCurrency(int amount)
    {
        FruitCurrency += amount;
        // Notify HUD via event or GameManager as needed
    }

    // ─── Damage / Lives ───────────────────────────────────────────────────

    /// <summary>
    /// Called when Pacman touches a non-frightened ghost.
    /// Subtracts a life; triggers game-over if none remain.
    /// </summary>
    public void TakeHit()
    {
        if (invincibilityTimer > 0f) return;   // still invincible, ignore

        CurrentLives--;
        OnLivesChanged?.Invoke(CurrentLives);

        if (CurrentLives <= 0)
        {
            // Zero lives → game over → upgrade screen
            OnPlayerDied?.Invoke();
        }
        else
        {
            // Brief invincibility, then respawn in same scene
            invincibilityTimer = INVINCIBILITY_DURATION;
            RespawnInPlace();
        }
    }

    private void RespawnInPlace()
    {
        // Stop movement and reset direction
        currentDirection = Vector2.zero;
        queuedDirection = Vector2.zero;
        rb.velocity = Vector2.zero;

        // Disable collider briefly so Pacman doesn't instantly re-collide
        GetComponent<Collider2D>().enabled = false;
        Invoke(nameof(ReenableCollider), INVINCIBILITY_DURATION * 0.75f);

        // Optionally play death animation here before re-enable
    }

    private void ReenableCollider()
    {
        GetComponent<Collider2D>().enabled = true;
    }

    private void UpdateInvincibility()
    {
        if (invincibilityTimer > 0f)
            invincibilityTimer -= Time.deltaTime;
    }

    // ─── Animation ────────────────────────────────────────────────────────

    private void UpdateAnimation()
    {
        animator.SetFloat(AnimDirX, currentDirection.x);
        animator.SetFloat(AnimDirY, currentDirection.y);
        animator.SetBool(AnimPowered, IsPoweredUp);
    }

    // ─── Collision Detection ──────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Pellets, power pellets, fruits, and ghost eating are handled here.
        // Tag-based dispatch — set these tags on your prefabs in Unity.

        switch (other.tag)
        {
            case "Pellet":
                other.gameObject.SetActive(false);
                AddScore(10); // base value; multiplied by upgrade in GameManager
                // GameManager.Instance.OnPelletEaten() tracks remaining count
                break;

            case "PowerPellet":
                other.gameObject.SetActive(false);
                ActivatePowerUp();
                AddScore(50);
                break;

            case "Fruit":
                other.gameObject.SetActive(false);
                AddScore(100);        // tweak per fruit type
                AddFruitCurrency(1);
                break;

            case "Ghost":
                GhostController ghost = other.GetComponent<GhostController>();
                if (ghost == null) break;

                if (IsPoweredUp && ghost.IsFrightened)
                {
                    // Eat the ghost
                    ghost.OnEaten();
                    AddScore(200);    // escalating value can be tracked in GameManager
                }
                else if (!ghost.IsFrightened && !ghost.IsEaten)
                {
                    TakeHit();
                }
                break;
        }
    }

    // ─── Upgrade Hooks ────────────────────────────────────────────────────
    // Call these from your UpgradeManager after the player selects an upgrade.

    public void UpgradeMoveSpeed(float bonus) => moveSpeed += bonus;
    public void UpgradePowerDuration(float bonus) => powerUpDuration += bonus;
    public void UpgradeMaxLives(int bonus)
    {
        maxLives += bonus;
        CurrentLives = Mathf.Min(CurrentLives + bonus, maxLives);
        OnLivesChanged?.Invoke(CurrentLives);
    }
}