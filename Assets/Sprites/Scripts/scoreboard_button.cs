using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>Fetches and displays the top-3 scoreboard when clicked; highlights the current player's rank.</summary>
public class scoreboard_button : MonoBehaviour
{
    public GameObject textfield;
    public GameObject backgroundfield;

    public GameObject infostuff;
    public GameObject infostuff2;

    [Header("Schrift")]
    public float fontSize = 36f;
    public string highlightColor = "#FFD700";

    private ScoreboardClient scoreboardClient;
    private TMP_Text tmp;

    void Start()
    {
        // LevelManager hat immer einen ScoreboardClient auf sich selbst –
        // den bevorzugen, damit wir dieselbe Instanz wie der Rest des Spiels nutzen.
        if (LevelManager.Instance != null)
            scoreboardClient = LevelManager.Instance.GetComponent<ScoreboardClient>();

        if (scoreboardClient == null)
            scoreboardClient = FindObjectOfType<ScoreboardClient>();

        if (scoreboardClient == null)
            scoreboardClient = gameObject.AddComponent<ScoreboardClient>();

        tmp = textfield != null ? textfield.GetComponent<TMP_Text>() : null;

        if (tmp != null)
        {
            tmp.enableAutoSizing  = false;
            tmp.fontSize          = fontSize;
            tmp.enableWordWrapping = false;
            tmp.overflowMode      = TextOverflowModes.Overflow;
        }
    }

    public void onScoreButtonClick()
    {
        if (infostuff  != null) infostuff.SetActive(false);
        if (infostuff2 != null) infostuff2.SetActive(false);

        bool isActive = textfield.activeSelf;
        textfield.SetActive(!isActive);
        backgroundfield.SetActive(!isActive);

        if (!isActive)
        {
            if (tmp != null) tmp.text = "Laden...";

            scoreboardClient.GetScoreboard((scores) =>
            {
                if (tmp == null) return;

                if (scores == null || scores.Count == 0)
                {
                    tmp.text = "Noch keine Einträge.";
                    return;
                }

                tmp.text = BuildScoreboardText(scores);
            });
        }
    }

    private string TruncateName(string name)
    {
        if (name.Length <= 9) return name;
        return name.Substring(0, 9) + "..";
    }

    private string BuildScoreboardText(List<ScoreEntry> scores)
    {
        scores.Sort((a, b) => b.score.CompareTo(a.score));

        string playerId  = PlayerPrefs.GetString("PlayerId", "");
        int    playerRank = -1;

        for (int i = 0; i < scores.Count; i++)
        {
            if (scores[i].id == playerId) { playerRank = i + 1; break; }
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        int topCount = Mathf.Min(3, scores.Count);
        for (int i = 0; i < topCount; i++)
        {
            string line = $"#{i + 1}  {TruncateName(scores[i].name)}  -  {scores[i].score}";
            if (i + 1 == playerRank)
                line = $"<color={highlightColor}>{line}</color>";
            sb.AppendLine(line);
        }

        if (playerRank > 0 && playerRank <= 3)
            return sb.ToString().TrimEnd();

        if (playerRank == -1)
        {
            if (scores.Count > 3) sb.AppendLine(".....");
            return sb.ToString().TrimEnd();
        }

        if (playerRank > 4) sb.AppendLine(".....");
        string ownLine = $"#{playerRank}  {TruncateName(scores[playerRank - 1].name)}  -  {scores[playerRank - 1].score}";
        sb.AppendLine($"<color={highlightColor}>{ownLine}</color>");

        return sb.ToString().TrimEnd();
    }
}