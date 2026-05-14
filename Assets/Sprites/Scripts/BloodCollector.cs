using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Tracks blood-tile collection, updates the score bar, and signals the LevelManager when the level is complete.</summary>
public class BloodCollector : MonoBehaviour
{
    [Header("Referenzen")]
    public Tilemap bloodTilemap;
    public TextMeshProUGUI scoreText;

    [Header("Die Bar (Sprite Renderer)")]
    public SpriteRenderer scorebarBorder;
    public SpriteRenderer scorebarFill;

    [Header("Einstellungen")]
    public float widthPerPoint = 0.05f;
    public float borderCornerPadding = 0.15f;

    [Header("Audio")]
    public AudioClip collectSound;
    public AudioClip levelCompleteSound;
    [Range(0f, 1f)] public float collectVolume = 1f;
    [Range(0f, 1f)] public float levelCompleteVolume = 1f;
    private AudioSource audioSource;

    [Header("Status")]

    public static int globalScore = 0;

    private int currentPointsInLevel = 0;
    private int maxPointsInLevel;
    private bool levelCompleted = false;
    private Vector3Int lastCell;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        InitializeLevel();
        UpdateScoreUI();
    }

    public void InitializeLevel()
    {
        if (bloodTilemap == null) return;

        maxPointsInLevel = CountAllBloodTiles();

        SetupSprite(scorebarBorder);
        SetupSprite(scorebarFill);

        if (scorebarBorder != null)
        {
            float totalWidth = (maxPointsInLevel * widthPerPoint) + borderCornerPadding;
            scorebarBorder.size = new Vector2(totalWidth, scorebarBorder.size.y);
        }

        currentPointsInLevel = 0;
        levelCompleted = false;
        lastCell = Vector3Int.zero;

        UpdateVisuals();
    }

    void SetupSprite(SpriteRenderer sr)
    {
        if (sr == null) return;
        sr.transform.localScale = Vector3.one;
        sr.drawMode = SpriteDrawMode.Sliced;
    }

    void Update()
    {
        if (bloodTilemap == null || levelCompleted) return;

        Vector3Int cell = bloodTilemap.WorldToCell(transform.position);
        if (cell != lastCell && bloodTilemap.HasTile(cell))
        {
            lastCell = cell;
            bloodTilemap.SetTile(cell, null);
            CollectBlood();
        }
    }

    void CollectBlood()
    {
        globalScore += 10;
        currentPointsInLevel++;

        if (collectSound != null)
            audioSource.PlayOneShot(collectSound, collectVolume);

        TilemapCollider2D collider = bloodTilemap.GetComponent<TilemapCollider2D>();
        if (collider != null) collider.ProcessTilemapChanges();

        UpdateScoreUI();
        UpdateVisuals();
        CheckLevelComplete();
    }

    void UpdateVisuals()
    {
        if (scorebarFill == null) return;
        float targetFillWidth = currentPointsInLevel * widthPerPoint;
        float finalWidth = Mathf.Max(0.01f, targetFillWidth);
        scorebarFill.size = new Vector2(finalWidth, 0.9125f);
    }

    int CountAllBloodTiles()
    {
        int count = 0;
        bloodTilemap.CompressBounds();
        BoundsInt bounds = bloodTilemap.cellBounds;
        TileBase[] tiles = bloodTilemap.GetTilesBlock(bounds);
        foreach (TileBase tile in tiles) if (tile != null) count++;
        return count;
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = globalScore.ToString();
        }
    }

    void CheckLevelComplete()
    {
        if (currentPointsInLevel >= maxPointsInLevel && !levelCompleted)
        {
            levelCompleted = true;
            Time.timeScale = 0f;

            StartCoroutine(TriggerNextLevelDelayed());
        }
    }

    IEnumerator TriggerNextLevelDelayed()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        if (LevelManager.Instance != null) LevelManager.Instance.OnLevelComplete();
    }


    public static void ResetGlobalScore()
    {
        globalScore = 0;

        BloodCollector bc = FindFirstObjectByType<BloodCollector>();
        if (bc != null)
        {
            bc.UpdateScoreUI();
        }
    }
}