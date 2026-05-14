using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ScoreEntry
{
    public string id;
    public string name;
    public int score;
}

[System.Serializable]
public class ScoreResponse
{
    public bool success;
    public string message;
    public ScoreEntry data;
}

/// <summary>HTTP client for posting and retrieving scores from the Node.js score server.</summary>
public class ScoreboardClient : MonoBehaviour
{
    private string serverUrl = null; // cached after first call

    private string GetServerUrl()
    {
        if (serverUrl != null) return serverUrl;

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string absoluteUrl = Application.absoluteURL;
            Debug.Log($"[ScoreboardClient] absoluteURL = '{absoluteUrl}'");

            Uri uri = new Uri(absoluteUrl);
            Debug.Log($"[ScoreboardClient] uri.Host = '{uri.Host}', uri.Scheme = '{uri.Scheme}'");

            serverUrl = $"{uri.Scheme}://{uri.Host}:3000";
            Debug.Log($"[ScoreboardClient] Server URL = '{serverUrl}'");
            return serverUrl;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScoreboardClient] URL error: {e.Message}");
        }
#endif
        serverUrl = "http://localhost:3000";
        return serverUrl;
    }

    public void AddScore(string id, string playerName, int score, Action<bool, string> callback = null)
    {
        StartCoroutine(AddScoreCoroutine(id, playerName, score, callback));
    }

    public void GetScoreboard(Action<List<ScoreEntry>> callback)
    {
        StartCoroutine(GetScoreboardCoroutine(callback));
    }

    private IEnumerator AddScoreCoroutine(string id, string playerName, int score, Action<bool, string> callback)
    {
        ScoreEntry entry = new ScoreEntry { id = id, name = playerName, score = score };
        string jsonData = JsonUtility.ToJson(entry);

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        UnityWebRequest request = new UnityWebRequest(GetServerUrl() + "/score", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 10;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[ScoreboardClient] Score gespeichert [{id}] {playerName}: {score}");
            callback?.Invoke(true, "Score gespeichert");
        }
        else
        {
            Debug.LogError($"[ScoreboardClient] Fehler beim Speichern: {request.error} | URL: {GetServerUrl()}/score");
            callback?.Invoke(false, request.error);
        }

        request.Dispose();
    }

    private IEnumerator GetScoreboardCoroutine(Action<List<ScoreEntry>> callback)
    {
        string url = GetServerUrl() + "/scoreboard";
        Debug.Log($"[ScoreboardClient] Lade Scoreboard von: {url}");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 10;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"[ScoreboardClient] Scoreboard empfangen: {jsonResponse}");

            List<ScoreEntry> scores = ParseScoreboardJson(jsonResponse);
            Debug.Log($"[ScoreboardClient] {scores.Count} Eintraege geparst.");
            callback?.Invoke(scores);
        }
        else
        {
            Debug.LogError($"[ScoreboardClient] Fehler beim Abrufen: {request.error} | URL: {url}");
            callback?.Invoke(new List<ScoreEntry>());
        }

        request.Dispose();
    }

    private List<ScoreEntry> ParseScoreboardJson(string json)
    {
        try
        {
            string wrappedJson = "{\"items\":" + json + "}";
            ScoreboardWrapper wrapper = JsonUtility.FromJson<ScoreboardWrapper>(wrappedJson);
            return wrapper?.items ?? new List<ScoreEntry>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScoreboardClient] JSON-Parsing fehlgeschlagen: {e.Message}\nJSON: {json}");
            return new List<ScoreEntry>();
        }
    }

    [System.Serializable]
    private class ScoreboardWrapper
    {
        public List<ScoreEntry> items;
    }
}
