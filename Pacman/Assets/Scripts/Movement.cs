using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    public float speed = 8f;
    public float speedMultiplier = 1f;
    public Vector2 initialDirection;

    [Header("Grid")]
    public float tileSize = 1f;
    public Vector2 gridOffset = new Vector2(0.5f, 0.5f);
    public float turnTolerance = 0.08f;

    [Header("Collision")]
    public LayerMask obstacleLayer; // MUST be set to Wall in Inspector

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
        direction = initialDirection;
        nextDirection = Vector2.zero;
        transform.position = startingPosition;

        rb.isKinematic = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;

        enabled = true;
    }

    private void Update()
    {
        // Try queued direction for smoother turning
        if (nextDirection != Vector2.zero)
        {
            SetDirection(nextDirection);
        }
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

    private float GetCenterSnapTolerance()
    {
        return Mathf.Max(turnTolerance, speed * speedMultiplier * Time.fixedDeltaTime * 0.6f);
    }

    private void FixedUpdate()
    {
        Vector2 pos = rb.position;
        if (IsAtCellCenter(pos))
        {
            // Snap EXACTLY like Pac-Man
            pos = WorldToCellCenter(pos);
            rb.position = pos;

            // Try queued direction
            if (nextDirection != Vector2.zero && !Occupied(nextDirection))
            {
                direction = nextDirection;
                nextDirection = Vector2.zero;
            }

            // Stop if blocked
            if (Occupied(direction))
            {
                return;
            }
        }

        // Move normally
        Vector2 translation = speed * speedMultiplier * Time.fixedDeltaTime * direction;
        rb.MovePosition(rb.position + translation);

    }

    public void Stop()
    {
        direction = Vector2.zero;
        nextDirection = Vector2.zero;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true; // freezes movement completely
        }

        enabled = false; // stops FixedUpdate from running
    }

    public void SetDirection(Vector2 direction, bool forced = false)
    {
        if (forced || !Occupied(direction))
        {
            this.direction = direction;
            nextDirection = Vector2.zero;
        }
        else
        {
            nextDirection = direction;
        }
    }

    public bool Occupied(Vector2 direction)
    {
        float distance = 0.6f;

        // Slightly larger and more reliable box
        Vector2 origin = rb.position + direction * 0.27f;
        Vector2 size = Vector2.one * distance;

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, direction, 0.1f, obstacleLayer);

        // DEBUG (optional)
        // if (hit.collider != null)
        // {
        //     Debug.Log(name + " hit wall: " + hit.collider.name);
        // }

        return hit.collider != null;
    }
}