using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    [Tooltip("Base movement speed")]
    public float speed = 8f;
    [Tooltip("Runtime speed multiplier")]
    public float speedMultiplier = 1f;
    [Tooltip("Initial movement direction (set in inspector)")]
    public Vector2 initialDirection;
    [Tooltip("Layers considered obstacles (walls, etc.)")]
    public LayerMask obstacleLayer;

    public Rigidbody2D rb { get; private set; }
    public Vector2 direction { get; private set; }
    public Vector2 nextDirection { get; private set; }
    public Vector3 startingPosition { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startingPosition = transform.position;
    }

    private void Start()
    {
        ResetState();
    }

    public void ResetState()
    {
        speedMultiplier = 1f;
        direction = initialDirection.normalized;
        nextDirection = Vector2.zero;
        transform.position = startingPosition;
        rb.isKinematic = false;
        enabled = true;
    }

    private void Update()
    {
        // If a direction is queued, try to apply it (more responsive turning).
        if (nextDirection != Vector2.zero)
        {
            // Try to set it — SetDirection will store it again if blocked.
            SetDirection(nextDirection);
        }
    }

    private void FixedUpdate()
    {
        if (direction == Vector2.zero)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // Ensure normalized direction to keep speed stable.
        Vector2 dir = direction.normalized;
        Vector2 position = rb.position;
        Vector2 translation = dir * speed * speedMultiplier * Time.fixedDeltaTime;

        rb.MovePosition(position + translation);
    }

    /// <summary>
    /// Attempts to set the current direction. If forced or the target direction is free of obstacles,
    /// the current direction is set. Otherwise the requested direction is stored as nextDirection.
    /// </summary>
    public void SetDirection(Vector2 dir, bool forced = false)
    {
        if (dir == Vector2.zero) return;

        Vector2 normalized = dir.normalized;

        if (forced || !Occupied(normalized))
        {
            direction = normalized;
            nextDirection = Vector2.zero;
        }
        else
        {
            // Keep as queued request until it becomes available
            nextDirection = normalized;
        }
    }

    /// <summary>
    /// Checks whether moving in the given direction would immediately hit an obstacle.
    /// Uses Rigidbody2D.Cast with a ContactFilter2D so the check ignores this object's own colliders.
    /// </summary>
    public bool Occupied(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;

        // Normalized direction assumed
        Vector2 testDir = dir.normalized;

        // Distance to check ahead — small so we only detect immediate blocking obstacles.
        const float checkDistance = 0.6f;

        // Setup contact filter with the provided obstacle layer and ignore triggers
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayer);
        filter.useTriggers = false;

        // Allocate a small results array — we only need to know if something was hit
        RaycastHit2D[] results = new RaycastHit2D[4];

        // Cast this rigidbody's shape in the requested direction. Rigidbody2D.Cast sweeps the rigidbody's
        // collider(s) and won't report the rigidbody's own colliders as hits.
        int hitCount = rb.Cast(testDir, filter, results, checkDistance);

        return hitCount > 0;
    }
}