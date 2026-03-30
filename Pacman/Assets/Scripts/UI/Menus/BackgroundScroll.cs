using UnityEngine;
using UnityEngine.UI;

public class BackgroundScroll : MonoBehaviour
{
    public float scrollSpeed = 0.02f;
    private RawImage rawImage;

    private void Start() => rawImage = GetComponent<RawImage>();

    private void Update()
    {
        rawImage.uvRect = new Rect(
            rawImage.uvRect.x + scrollSpeed * Time.deltaTime,
            rawImage.uvRect.y + scrollSpeed * Time.deltaTime,
            rawImage.uvRect.width,
            rawImage.uvRect.height);
    }
}