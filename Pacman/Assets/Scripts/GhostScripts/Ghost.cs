using UnityEngine;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Movement))]
public class Ghost : MonoBehaviour
{
    public Movement movement { get; private set; }
    public GhostHome home { get; private set; }
    public GhostScatter scatter { get; private set; }
    public GhostChase chase { get; private set; }
    public GhostFrightened frightened { get; private set; }
    public GhostBehavior initialBehavior;
    public Transform target;
    public int points = 200;

    private void Awake()
    {
        movement = GetComponent<Movement>();
        home = GetComponent<GhostHome>();
        scatter = GetComponent<GhostScatter>();
        chase = GetComponent<GhostChase>();
        frightened = GetComponent<GhostFrightened>();
        behaviors = GetComponents<GhostBehavior>();
    }

    private void Start()
    {
        ResetState();
    }

    public void ResetState()
    {
        gameObject.SetActive(true);
        movement.ResetState();

        frightened.Disable();
        chase.Disable();
        scatter.Enable();

        if (home != initialBehavior)
        {
            home.Disable();
        }

        if (initialBehavior != null)
        {
            initialBehavior.Enable();
        }
    }

    public void SetPosition(Vector3 position)
    {
        // Keep the z-position the same since it determines draw depth
        position.z = transform.position.z;
        transform.position = position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
    }
    public bool IsFrightened { get; private set; } = false;
    public bool IsEaten { get; private set; } = false;

    // cache any GhostBehavior components on this GameObject (may be multiple)
    private GhostBehavior[] behaviors;
    private bool isFrozen;

    // Called by Pacman when a powered-up Pacman eats a ghost.
    // Now validates a nearby Pacman collision before marking eaten.
    public void OnEaten()
    {
        if (IsEaten) return;

        // Determine a sensible radius for overlap using the ghost's collider if present,
        // otherwise fall back to a small default.
        float radius = 0.5f;
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null)
        {
            radius = Mathf.Max(myCol.bounds.extents.x, myCol.bounds.extents.y) * 1.2f;
            radius = Mathf.Max(radius, 0.25f);
        }

        // Find overlapping colliders at the ghost position.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            // Try to locate the Pacman component (class name `testMove` in this project).
            var player = hit.GetComponent<testMove>();
            if (player != null)
            {
                // Only mark eaten when the player is powered up and the ghost is frightened.
                if (player.IsPoweredUp && IsFrightened)
                {
                    IsEaten = true;
                }
                break;
            }

            // Fallback detection by tag or layer if the component wasn't found.
            if (hit.CompareTag("Pacman") || hit.CompareTag("Player") ||
                hit.gameObject.layer == LayerMask.NameToLayer("Pacman"))
            {
                var p = hit.GetComponent<testMove>();
                if (p != null && p.IsPoweredUp && IsFrightened)
                {
                    IsEaten = true;
                }
                break;
            }
        }
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;

        if (behaviors == null)
        {
            behaviors = GetComponents<GhostBehavior>();
        }

        foreach (var behavior in behaviors)
        {
            if (behavior == null) continue;
            behavior.enabled = !frozen;
        }

        Movement movement = GetComponent<Movement>();
        if (movement != null)
        {
            movement.enabled = !frozen;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isFrozen)
        {
            return;
        }
    }
}