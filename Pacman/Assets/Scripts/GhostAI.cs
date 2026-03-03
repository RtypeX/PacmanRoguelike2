using System.Collections;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class GhostAI : MonoBehaviour
{
    public float speed = 4f;
    public Transform target;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.left;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void FixedUpdate()
    {
        if (AtIntersection())
        {
            ChooseDirection();
        }

        rb.velocity = direction * speed;
    }

    bool AtIntersection()
    {
        int paths = 0;

        if (CanMove(Vector2.up)) paths++;
        if (CanMove(Vector2.down)) paths++;
        if (CanMove(Vector2.left)) paths++;
        if (CanMove(Vector2.right)) paths++;

        return paths >= 3;
    }

    void ChooseDirection()
    {
        List<Vector2> possibleDirections = new List<Vector2>();
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (Vector2 dir in directions)
        {
            if (CanMove(dir) && dir != -direction)
            {
                possibleDirections.Add(dir);
            }
        }

        if (possibleDirections.Count == 0) return;

        Vector2 bestDir = possibleDirections[0];
        float shortestDistance = Mathf.Infinity;

        foreach (Vector2 dir in possibleDirections)
        {
            Vector2 newPos = (Vector2)transform.position + dir;
            float distance = Vector2.Distance(newPos, target.position);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                bestDir = dir;
            }
        }

        direction = bestDir;
    }

    bool CanMove(Vector2 dir)
    {
        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position,
            0.2f,
            dir,
            0.55f
        );

        return hit.collider == null;
    }
}