using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    public float speed = 8f;
    public float speedMultiplier = 1f;
    public Vector2 initialDirection;

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

    private void FixedUpdate()
    {
        // Try to apply queued direction again in physics step
        if (nextDirection != Vector2.zero)
        {
            SetDirection(nextDirection);
        }

        // STOP if wall ahead
        if (Occupied(direction))
        {
            return;
        }

        Vector2 position = rb.position;
        Vector2 translation = speed * speedMultiplier * Time.fixedDeltaTime * direction;

        rb.MovePosition(position + translation);
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
        Vector2 origin = rb.position + direction * 0.4f;
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