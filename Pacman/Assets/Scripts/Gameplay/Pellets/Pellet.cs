using UnityEngine;

public class Pellet : MonoBehaviour
{
    public int points = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            testMove player = other.GetComponent<testMove>();
            if (player == null) return;

            player.AddScore(points);
            GameManager.Instance.OnPelletEaten();
            gameObject.SetActive(false);
        }
    }
}
