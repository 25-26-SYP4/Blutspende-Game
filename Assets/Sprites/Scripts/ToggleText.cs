using UnityEngine;

/// <summary>Toggles the info-text panel and hides scoreboard panels when the info button is clicked.</summary>
public class ToggleText : MonoBehaviour
{
    public GameObject textfield;
    public GameObject backgroundfield;

    public GameObject scorestuff;
    public GameObject scoreStuff2;

    public void onInfoButtonClick()
    {
        scorestuff.SetActive(false);
        scoreStuff2.SetActive(false);

        bool isActive = textfield.activeSelf;
        textfield.SetActive(!isActive);
        backgroundfield.SetActive(!isActive);
    }
}
