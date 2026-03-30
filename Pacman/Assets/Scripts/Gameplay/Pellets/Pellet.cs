using UnityEngine;

public class Pellet : MonoBehaviour
{
    public int pointValue = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            testMove player = other.GetComponent<testMove>();
            if (player == null) return;

            player.AddScore(pointValue);
            GameManager.Instance.OnPelletEaten();
            gameObject.SetActive(false);
        }
    }
}
