using UnityEngine;
using UnityEngine.UI;

/// <summary>Toggles global audio mute state and persists the preference via PlayerPrefs.</summary>
public class MuteButton : MonoBehaviour
{
    [Header("Button Sprites")]
    public Sprite muteSprite;
    public Sprite unmuteSprite;

    [Header("Referenz")]
    public Image buttonImage;

    private bool isMuted = false;

    void Start()
    {
        isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        ApplyMute();
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        ApplyMute();

        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplyMute()
    {
        AudioListener.volume = isMuted ? 0f : 1f;

        if (buttonImage == null) return;
        if (isMuted && muteSprite != null)
            buttonImage.sprite = muteSprite;
        else if (!isMuted && unmuteSprite != null)
            buttonImage.sprite = unmuteSprite;
    }
}