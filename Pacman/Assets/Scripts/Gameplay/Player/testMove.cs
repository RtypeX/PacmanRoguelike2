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
    private Vector2 lastDirection = Vector2.right;

    // ─── Events ──────────────────────────────────────────────────────────────

    public static event System.Action<int> OnScoreChanged;
    public static event System.Action<int> OnLivesChanged;
    public static event System.Action OnPlayerDied;
    public static event System.Action OnPowerUpStart;
    public static event System.Action OnPowerUpEnd;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (PlayerUpgrades.Instance != null)
        {
            moveSpeed += PlayerUpgrades.Instance.SpeedBonus;
            powerUpDuration += PlayerUpgrades.Instance.PowerDurationBonus;
            maxLives += PlayerUpgrades.Instance.BonusLives;
        }

        CurrentLives = maxLives;
    }

    private void Start()
    {
        OnScoreChanged?.Invoke(Score);
        OnLivesChanged?.Invoke(CurrentLives);
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

    private void Move()
    {
        Vector2 pos = rb.position;

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
            rb.MovePosition(rb.position + currentDirection * moveSpeed * Time.fixedDeltaTime);
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
        return Vector2.Distance(pos, center) < turnTolerance;
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
                other.gameObject.SetActive(false);
                AddScore(100);
                AddFruitCurrency(1);
                ManageHUD.Instance?.UpdateFruitCurrency(
                    CurrencyManager.Instance != null ? CurrencyManager.Instance.FruitCurrency : FruitCurrency);
                break;

            case "Ghost":
                GhostController ghost = other.GetComponent<GhostController>();
                if (ghost == null) break;

                if (IsPoweredUp && ghost.CanBeEaten)
                {
                    ghost.OnEaten();
                    AddScore(200);
                }
                else if (!ghost.IsEaten)
                {
                    TakeHit();
                }
                break;
        }
    }

    // ─── Upgrade Hooks ────────────────────────────────────────────────────

    public void UpgradeMoveSpeed(float bonus) => moveSpeed += bonus;
    public void UpgradePowerDuration(float bonus) => powerUpDuration += bonus;
    public void UpgradeMaxLives(int bonus)
    {
        maxLives += bonus;
        CurrentLives = Mathf.Min(CurrentLives + bonus, maxLives);
        OnLivesChanged?.Invoke(CurrentLives);
    }
}
