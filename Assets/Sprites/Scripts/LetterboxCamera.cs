using UnityEngine;

/// <summary>Adjusts the main camera's orthographic size so the target sprite fills the screen without distortion.</summary>
public class LetterboxCamera : MonoBehaviour
{
    public SpriteRenderer targetBounds; // assign the background/maze sprite here

    void Start()
    {
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = targetBounds.bounds.size.x / targetBounds.bounds.size.y;

        if (screenRatio >= targetRatio)
        {
            Camera.main.orthographicSize = targetBounds.bounds.size.y / 2;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            Camera.main.orthographicSize = targetBounds.bounds.size.y / 2 * differenceInSize;
        }
    }
}
