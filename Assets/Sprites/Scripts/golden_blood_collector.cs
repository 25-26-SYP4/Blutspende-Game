using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Detects golden blood tiles, activates temporary invincibility, and handles the visual flash warning.</summary>
public class golden_blood_collector : MonoBehaviour
{
    public Tilemap bloodTilemap;
    public LayerMask bloodLayer;
    public bool invincible = false;
    public float INVINCIBLE_DURATION = 8f;

    [Header("Charakter Sprites")]
    public SpriteRenderer characterSpriteRenderer;
    public Sprite redSprite;
    public Sprite goldenSprite;

    [Header("Audio")]
    public AudioClip goldenActivateSound;
    [Range(0f, 1f)] public float activateVolume = 1f;
    private AudioSource audioSource;

    private float invincibleTimer = 0f;
    private float flashTimer = 0f;
    private float flashInterval = 0.15f;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (bloodTilemap == null) return;

        Vector2 pos = transform.position;
        Collider2D hit = Physics2D.OverlapCircle(pos, 0.1f, bloodLayer);

        if (hit != null)
        {
            Vector3Int cell = bloodTilemap.WorldToCell(transform.position);

            if (bloodTilemap.HasTile(cell))
            {
                bloodTilemap.SetTile(cell, null);
                bloodTilemap.RefreshTile(cell);
                ActivateGoldenMode();
            }
        }

        if (invincible)
        {
            invincibleTimer -= Time.deltaTime;

            if (invincibleTimer < 3f)
            {
                HandleFlashing();
            }

            if (invincibleTimer <= 0f)
            {
                DeactivateGoldenMode();
            }
        }
    }

    void HandleFlashing()
    {
        flashTimer += Time.deltaTime;
        if (flashTimer >= flashInterval)
        {
            flashTimer = 0f;
            if (characterSpriteRenderer.color.a > 0.5f)
                characterSpriteRenderer.color = new Color(1, 1, 1, 0.4f);
            else
                characterSpriteRenderer.color = Color.white;
        }
    }

    void ActivateGoldenMode()
    {
        invincible = true;
        invincibleTimer = INVINCIBLE_DURATION;
        flashTimer = 0f;

        if (goldenActivateSound != null)
            audioSource.PlayOneShot(goldenActivateSound, activateVolume);

        if (animator != null)
            animator.SetBool("golden", true);

        if (characterSpriteRenderer != null && goldenSprite != null)
        {
            characterSpriteRenderer.sprite = goldenSprite;
            characterSpriteRenderer.color = Color.white;
        }
    }

    public void DeactivateGoldenMode()
    {
        invincible = false;
        invincibleTimer = 0f;

        if (animator != null)
            animator.SetBool("golden", false);

        if (characterSpriteRenderer != null && redSprite != null)
        {
            characterSpriteRenderer.sprite = redSprite;
            characterSpriteRenderer.color = Color.white;
        }
    }
}