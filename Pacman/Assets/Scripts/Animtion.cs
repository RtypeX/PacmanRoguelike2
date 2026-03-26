using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Animtion : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;

    // Reference to your movement script
    private testMove movementScript;

    // Stores last direction so animation doesn't snap when stopping
    private Vector2 lastDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // IMPORTANT: Replace with your actual movement script name
        movementScript = GetComponent<testMove>();
    }

    void Update()
    {
        // Get direction from movement script
        Vector2 direction = movementScript.currentDirection;

        bool isMoving = direction != Vector2.zero;

        // Store last direction for idle facing
        if (isMoving)
        {
            lastDirection = direction;
        }

        // Send values to Animator
        animator.SetFloat("MoveX", lastDirection.x);
        animator.SetFloat("MoveY", lastDirection.y);
        animator.SetBool("IsMoving", isMoving);
    }
}