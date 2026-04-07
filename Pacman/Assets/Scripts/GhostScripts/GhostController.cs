using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostController : MonoBehaviour
{
    // Public conditions requested — added but intentionally inert.
    // Visibility matches how `testMove` / `Pacman` expects to read them.
    public bool IsFrightened { get; private set; } = false;
    public bool IsEaten { get; private set; } = false;

    // Public string for classification (leave empty in code; set in Inspector)
    [Tooltip("Set to 'red', 'pink', 'cyan' or 'orange' in the Inspector")]
    public string ghostColor;

    // Classification flags derived from `ghostColor`
    public bool IsRed { get; private set; }
    public bool IsPink { get; private set; }
    public bool IsCyan { get; private set; }
    public bool IsOrange { get; private set; }

    // cache any GhostBehavior components on this GameObject (may be multiple)
    private GhostBehavior[] behaviors;

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

    private void OnEnable()
    {
        // Mirror `IsPoweredUp` in `testMove` via its static events.
        testMove.OnPowerUpStart += HandlePowerUpStart;
        testMove.OnPowerUpEnd += HandlePowerUpEnd;
    }

    private void OnDisable()
    {
        testMove.OnPowerUpStart -= HandlePowerUpStart;
        testMove.OnPowerUpEnd -= HandlePowerUpEnd;
    }

    // Ensure behaviors are cached early
    private void Awake()
    {
        behaviors = GetComponents<GhostBehavior>();
    }

    private void HandlePowerUpStart()
    {
        IsFrightened = true;

        // Propagate to frightened behavior (if present) by enabling it.
        if (behaviors != null)
        {
            foreach (var b in behaviors)
            {
                if (b == null) continue;

                // Only enable the frightened behavior(s)
                var gf = b as GhostFrightened;
                if (gf != null)
                {
                    gf.Enable();
                }
            }
        }
    }

    private void HandlePowerUpEnd()
    {
        IsFrightened = false;

        // Disable frightened behavior(s)
        if (behaviors != null)
        {
            foreach (var b in behaviors)
            {
                if (b == null) continue;

                var gf = b as GhostFrightened;
                if (gf != null)
                {
                    gf.Disable();
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        DetermineClassification();

        // Required check: verify if this ghost is red at Start
        if (IsRed)
        {
            Debug.Log($"{name}: classified as RED.");
        }
    }

    private void DetermineClassification()
    {
        IsRed = IsPink = IsCyan = IsOrange = false;

        if (string.IsNullOrWhiteSpace(ghostColor))
            return;

        string type = ghostColor.Trim().ToLowerInvariant();
        switch (type)
        {
            case "red":
                IsRed = true;
                break;
            case "pink":
                IsPink = true;
                break;
            case "cyan":
                IsCyan = true;
                break;
            case "orange":
                IsOrange = true;
                break;
            default:
                Debug.LogWarning($"{name}: Unknown ghostColor '{ghostColor}'. Expected 'red', 'pink', 'cyan' or 'orange'.");
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
