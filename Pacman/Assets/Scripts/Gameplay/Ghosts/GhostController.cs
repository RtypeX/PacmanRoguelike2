using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GhostController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public LayerMask wallLayer;
    public float tileSize = 1f;
    public Vector2 gridOffset = new Vector2(0.5f, 0.5f);
    public float frightenedSpeedMultiplier = 0.7f;

    [Header("Respawn")]
    public Transform homePoint;
    public float eatenRespawnDelay = 2f;

    public bool IsFrightened { get; private set; }
    public bool IsEaten { get; private set; }
    public bool IsFrozen { get; private set; }
    public bool CanBeEaten => IsFrightened && !IsEaten;

    private Rigidbody2D rb;
    private Collider2D ghostCollider;
    private SpriteRenderer spriteRenderer;
    private Vector2 currentDirection = Vector2.left;
    private Vector2 spawnPosition;

    private static readonly Vector2[] Directions =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ghostCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spawnPosition = rb.position;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        testMove.OnPowerUpStart += HandlePowerUpStart;
        testMove.OnPowerUpEnd += HandlePowerUpEnd;
    }

    private void OnDisable()
    {
        testMove.OnPowerUpStart -= HandlePowerUpStart;
        testMove.OnPowerUpEnd -= HandlePowerUpEnd;
    }

    private void FixedUpdate()
    {
        if (IsFrozen || IsEaten)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Move();
    }

    private void Move()
    {
        Vector2 center = WorldToCellCenter(rb.position);
        bool atCenter = Vector2.Distance(rb.position, center) < 0.08f;

        if (atCenter)
        {
            rb.position = center;

            if (!CanMove(currentDirection))
                currentDirection = PickNextDirection();
            else
            {
                Vector2 turn = PickTurnDirection();
                if (turn != Vector2.zero)
                    currentDirection = turn;
            }
        }

        float speed = moveSpeed * (IsFrightened ? frightenedSpeedMultiplier : 1f);
        rb.MovePosition(rb.position + currentDirection * speed * Time.fixedDeltaTime);
    }

    private Vector2 PickNextDirection()
    {
        Vector2 fallback = -currentDirection;

        foreach (Vector2 direction in Directions)
        {
            if (direction == -currentDirection)
                continue;

            if (CanMove(direction))
                return direction;
        }

        return CanMove(fallback) ? fallback : currentDirection;
    }

    private Vector2 PickTurnDirection()
    {
        foreach (Vector2 direction in Directions)
        {
            if (direction == currentDirection || direction == -currentDirection)
                continue;

            if (CanMove(direction) && Random.value > 0.65f)
                return direction;
        }

        return Vector2.zero;
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

    private void HandlePowerUpStart()
    {
        if (!IsEaten)
            SetFrightened(true);
    }

    private void HandlePowerUpEnd()
    {
        if (!IsEaten)
            SetFrightened(false);
    }

    public void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;

        if (frozen)
            rb.velocity = Vector2.zero;
    }

    public void SetFrightened(bool frightened)
    {
        IsFrightened = frightened;

        if (spriteRenderer != null)
            spriteRenderer.color = frightened ? Color.cyan : Color.white;
    }

    public void OnEaten()
    {
        if (IsEaten)
            return;

        IsEaten = true;
        SetFrightened(false);
        if (ghostCollider != null) ghostCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        Invoke(nameof(Respawn), eatenRespawnDelay);
    }

    private void Respawn()
    {
        Vector2 targetPos = homePoint != null ? homePoint.position : spawnPosition;
        rb.position = targetPos;
        currentDirection = Vector2.left;
        IsEaten = false;
        if (ghostCollider != null) ghostCollider.enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;

        testMove pacman = FindObjectOfType<testMove>();
        if (pacman != null && pacman.IsPoweredUp)
            SetFrightened(true);
    }
}
