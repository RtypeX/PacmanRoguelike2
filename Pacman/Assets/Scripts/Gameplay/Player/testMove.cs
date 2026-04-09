using UnityEngine;

/// <summary>
/// testMove handles player movement, input, collisions, scoring, and power-up state.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class testMove : MonoBehaviour
{
    // ─── Inspector Fields ───────────────────────────────────────────────────

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float maxMoveSpeed = 7.5f;

    [Header("Lives")]
    public int maxLives = 3;

    [Header("Grid / Tile Settings")]
    public float tileSize = 1f;
    public LayerMask wallLayer;
    public Vector2 gridOffset = new Vector2(0.5f, 0.5f);
    public float turnTolerance = 0.08f;

    // ─── Runtime State ───────────────────────────────────────────────────────

    public int CurrentLives { get; private set; }
    public bool IsPoweredUp { get; private set; }
    public int Score { get; private set; }
    public int FruitCurrency { get; private set; }

    // ─── Private Fields ───────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private Animator animator;

    public Vector2 currentDirection = Vector2.zero;
    private Vector2 queuedDirection = Vector2.zero;

    private float powerUpTimer = 0f;
    public float powerUpDuration = 8f;

    private float invincibilityTimer = 0f;
    private const float INVINCIBILITY_DURATION = 1.5f;

    private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
    private static readonly int AnimMoveY = Animator.StringToHash("MoveY");
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimDeath = Animator.StringToHash("Death");
    private Vector2 lastDirection = Vector2.right;

    // ─── Events ──────────────────────────────────────────────────────────────

    public static event System.Action<int> OnScoreChanged;
    public static event System.Action<int> OnLivesChanged;
    public static event System.Action OnPlayerDied;
    public static event System.Action OnPowerUpStart;
    public static event System.Action OnPowerUpEnd;

    [SerializeField]
    private AnimatedSprite deathSequence;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Movement movement;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        movement = GetComponent<Movement>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        GameManager.EnsureInstance();

        // 1. Apply upgrades first so the math is correct
        ApplyPurchasedUpgrades();

        // 2. Initialize UI
        OnScoreChanged?.Invoke(Score);
        OnLivesChanged?.Invoke(CurrentLives);
    }

    void ApplyPurchasedUpgrades()
    {
        if (PlayerUpgrades.Instance == null)
        {
            Debug.LogError("!!! HUD/PLAYER: Cannot find PlayerUpgrades! Is it in the scene?");
            CurrentLives = maxLives;
            return;
        }

        // This will tell us if the value is actually saved
        Debug.Log("Checking Upgrades... Bonus Lives found: " + PlayerUpgrades.Instance.BonusLives);

        maxLives = 3 + PlayerUpgrades.Instance.BonusLives;
        CurrentLives = maxLives;

        // 3. IMPORTANT: Tell the HUD to show the new number
        ManageHUD.Instance?.UpdateLivesUI(CurrentLives);

        // Also apply speed while we are here
        moveSpeed = Mathf.Min(5f + PlayerUpgrades.Instance.SpeedBonus, maxMoveSpeed);
        powerUpDuration = 8f + PlayerUpgrades.Instance.PowerDurationBonus;
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

    // ─── Input & Rotation ───────────────────────────────────────────────────

    /// <summary>
    /// Reads arrow key (or WASD) input and buffers it as queuedDirection.
    /// The queued direction is applied as soon as the path is clear.
    /// </summary>
    private void HandleInput()
    {
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            TryQueueDirection(Vector2.up);
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            TryQueueDirection(Vector2.down);
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            TryQueueDirection(Vector2.left);
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            TryQueueDirection(Vector2.right);
    }

    /// <summary>
    /// Attempts to set queuedDirection only if the requested direction is not blocked.
    /// This prevents players from "inputting into walls".
    /// </summary>
    private void TryQueueDirection(Vector2 direction)
    {
        // If the direction is already queued, nothing to do.
        if (queuedDirection == direction)
            return;

        // If the requested direction is currently free of walls, queue it.
        if (CanMove(direction))
        {
            queuedDirection = direction;
        }
        // Otherwise ignore the input (do not set queuedDirection).
    }

    // ─── Movement ─────────────────────────────────────────────────────────

    private void Move()
    {
        Vector2 pos = rb.position;
        float moveStep = GetMoveStep();

        if (IsAtCellCenter(pos))
        {
            pos = WorldToCellCenter(pos);
            rb.position = pos;

            if (queuedDirection != Vector2.zero && CanMove(queuedDirection))
            {
                currentDirection = queuedDirection;
            }

            if (currentDirection != Vector2.zero && !CanMove(currentDirection))
            {
                currentDirection = Vector2.zero;
            }
        }

        if (currentDirection != Vector2.zero)
        {
            rb.MovePosition(rb.position + currentDirection * moveStep);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    private bool CanMove(Vector2 direction)
    {
        Vector2 center = WorldToCellCenter(rb.position);
        Vector2 checkPos = center + direction * tileSize;
        Vector2 checkSize = Vector2.one * (tileSize * 0.3f);

        Collider2D hit = Physics2D.OverlapBox(checkPos, checkSize, 0f, wallLayer);
        return hit == null;
    }

    private Vector2 WorldToCellCenter(Vector2 pos)
    {
        float x = Mathf.Round((pos.x - gridOffset.x) / tileSize) * tileSize + gridOffset.x;
        float y = Mathf.Round((pos.y - gridOffset.y) / tileSize) * tileSize + gridOffset.y;
        return new Vector2(x, y);
    }

    private bool IsAtCellCenter(Vector2 pos)
    {
        Vector2 center = WorldToCellCenter(pos);
        return Vector2.Distance(pos, center) <= GetCenterSnapTolerance();
    }

    private float GetMoveStep()
    {
        return Mathf.Min(moveSpeed, maxMoveSpeed) * Time.fixedDeltaTime;
    }

    private float GetCenterSnapTolerance()
    {
        // At higher speeds we can step over the exact center between physics ticks,
        // so expand the snap window based on the current movement step.
        return Mathf.Max(turnTolerance, GetMoveStep() * 0.6f);
    }

    // ─── Power-Up ─────────────────────────────────────────────────────────

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

    public void AddScore(int amount)
    {
        float multiplier = PlayerUpgrades.Instance != null ? PlayerUpgrades.Instance.ScoreMultiplier : 1f;
        int finalAmount = Mathf.RoundToInt(amount * multiplier);

        Score += finalAmount;
        CurrencyManager.Instance?.AddPoints(finalAmount);
        OnScoreChanged?.Invoke(Score);

        // Show the pop-up (if implemented)
        ManageHUD.Instance?.ShowScorePopup(finalAmount, transform.position);
    }

    public void AddFruitCurrency(int amount)
    {
        FruitCurrency += amount;
        CurrencyManager.Instance?.AddFruitCurrency(amount);
    }

    // ─── Damage / Lives ───────────────────────────────────────────────────

    public void TakeHit()
    {
        if (invincibilityTimer > 0f) return;

        CurrentLives--;
        OnLivesChanged?.Invoke(CurrentLives);
        Debug.Log("Took Hit! Lives remaining: " + CurrentLives);

        if (CurrentLives <= 0)
        {
            OnPlayerDied?.Invoke();
        }
        else
        {
            invincibilityTimer = INVINCIBILITY_DURATION;
            RespawnInPlace();
        }
    }

    private void RespawnInPlace()
    {
        currentDirection = Vector2.zero;
        queuedDirection = Vector2.zero;
        rb.velocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Invoke(nameof(ReenableCollider), INVINCIBILITY_DURATION * 0.75f);
    }

    private void ReenableCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
    }

    private void UpdateInvincibility()
    {
        if (invincibilityTimer > 0f)
            invincibilityTimer -= Time.deltaTime;
    }

    public void ResetState()
    {
        enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (circleCollider != null) circleCollider.enabled = true;
        if (deathSequence != null) deathSequence.enabled = false;
        if (movement != null) movement.ResetState();
        gameObject.SetActive(true);
    }

    public void DeathSequence()
    {
        enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (circleCollider != null) circleCollider.enabled = false;
        if (movement != null) movement.enabled = false;
        if (deathSequence != null)
        {
            deathSequence.enabled = true;
            deathSequence.Restart();
        }
    }


    // ─── Animation ────────────────────────────────────────────────────────

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = currentDirection != Vector2.zero;

        if (isMoving)
            lastDirection = currentDirection;

        animator.SetFloat(AnimMoveX, lastDirection.x);
        animator.SetFloat(AnimMoveY, lastDirection.y);
        animator.SetBool(AnimIsMoving, isMoving);
    }

    // ─── Collision Detection ──────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        switch (other.tag)
        {
            case "Pellet":
                other.gameObject.SetActive(false);
                AddScore(10);
                GameManager.Instance?.OnPelletEaten();
                break;

            case "PowerPellet":
                other.gameObject.SetActive(false);
                ActivatePowerUp();
                AddScore(50);
                GameManager.Instance?.OnPelletEaten();
                break;

            case "Fruit":
                FruitPickup fruitPickup = other.GetComponent<FruitPickup>();
                if (fruitPickup != null)
                {
                    fruitPickup.Collect(this);
                }
                else
                {
                    other.gameObject.SetActive(false);
                    AddScore(100);
                    AddFruitCurrency(1);
                    ManageHUD.Instance?.UpdateFruitCurrency(
                        CurrencyManager.Instance != null ? CurrencyManager.Instance.FruitCurrency : FruitCurrency);
                }
                break;

            case "Ghost":
                GhostController ghost = other.GetComponent<GhostController>();
                if (ghost == null) break;

                if (IsPoweredUp)
                {
                    ghost.OnEaten();
                    AddScore(200);
                }
                else if (!ghost.IsEaten)
                {
                    animator.SetTrigger(AnimDeath);
                    TakeHit();
                }
                break;
        }
    }

    // ─── Upgrade Hooks (Called by PlayerUpgrades during purchase) ──────────

    public void UpgradeMoveSpeed(float bonus)
    {
        moveSpeed = Mathf.Min(moveSpeed + bonus, maxMoveSpeed);
    }
    public void UpgradePowerDuration(float bonus) => powerUpDuration += bonus;
    public void UpgradeMaxLives(int bonus)
    {
        maxLives += bonus;
        CurrentLives = Mathf.Min(CurrentLives + bonus, maxLives);
        OnLivesChanged?.Invoke(CurrentLives);
        ManageHUD.Instance?.UpdateLivesUI(CurrentLives);
    }
}
