using UnityEngine;
using TMPro;

/// <summary>Pauses and resumes the game by toggling Time.timeScale; also shows/hides a pause overlay.</summary>
public class PauseManager : MonoBehaviour
{
    [Header("UI Referenzen")]
    public GameObject pauseOverlay;   // optional: Panel das bei Pause erscheint
    public TMP_Text pauseButtonText;  // optional: Text des Pause-Buttons

    private bool isPaused = false;

    // Dem Button im Inspector unter OnClick() zuweisen
    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseOverlay != null) pauseOverlay.SetActive(true);
        if (pauseButtonText != null) pauseButtonText.text = "▶";
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseOverlay != null) pauseOverlay.SetActive(false);
        if (pauseButtonText != null) pauseButtonText.text = "II";
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}