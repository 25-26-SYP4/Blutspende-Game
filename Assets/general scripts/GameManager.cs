using UnityEngine;

/// <summary>Coordinates high-level game events and provides access to the scoreboard.</summary>
public class GameManager : MonoBehaviour
{
    private ScoreboardClient scoreboardClient;

    void Start()
    {
        scoreboardClient = GetComponent<ScoreboardClient>();
    }

    // Score updates are sent directly via LevelManager.SendScoreUpdate().
    // Use this class for any additional UI logic (e.g. displaying the scoreboard).

    void ShowScoreboard()
    {
        scoreboardClient.GetScoreboard((scores) =>
        {
            foreach (var entry in scores)
            {
                Debug.Log($"[{entry.id}] {entry.name}: {entry.score}");
            }
        });
    }
}
