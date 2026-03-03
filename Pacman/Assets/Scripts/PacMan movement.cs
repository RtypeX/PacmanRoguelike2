using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PacmanMovement : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private Vector2 nextDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            nextDirection = Vector2.up;

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            nextDirection = Vector2.down;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            nextDirection = Vector2.left;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            nextDirection = Vector2.right;
    }

    void FixedUpdate()
    {
        // Try to change direction if not blocked
        if (CanMove(nextDirection))
        {
            direction = nextDirection;
        }

        rb.velocity = direction * speed;
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
}}