using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>Tile-based ghost AI with four personality types; difficulty scales with the current level index.</summary>
public class GhostMovement : MonoBehaviour
{
    [Header("Referenzen")]
    public Tilemap tilemap;

    [HideInInspector] public Transform player;

    [Header("Settings")]
    public float speed = 3f;
    public LayerMask wallMask;

    private float centerTolerance = 0.05f;

    public enum GhostPersonality { Chaser, Ambusher, Random, Patroller }

    [Header("AI Behavior")]
    public GhostPersonality personality = GhostPersonality.Chaser;

    [Header("Scatter Settings")]
    [Tooltip("Alle X Sekunden wechselt der Geist kurz in den Zufallsmodus")]
    public float scatterInterval = 12f;
    [Tooltip("Wie lange der Scatter-Modus dauert")]
    public float scatterDuration = 3f;

    private Vector2 currentDir = Vector2.zero;
    private BoxCollider2D col;
    private golden_blood_collector goldenBloodCollector;
    private PacManMovement2D playerMovement;

    private Vector3Int lastDecisionCell = new Vector3Int(-999, -999, 0);

    private float scatterTimer = 0f;
    private bool isScattering = false;

    private Vector3 patrolTarget;
    private static readonly Vector2[] corners = {
        new Vector2(-10f, -10f),
        new Vector2( 10f, -10f),
        new Vector2(-10f,  10f),
        new Vector2( 10f,  10f),
    };
    private int currentCornerIndex = 0;

    private float randomBias = 0.4f; // Wie stark Random-Geist auf Spieler zielt
    private float patrolChaseRange = 6f;   // Ab welcher Distanz Patroller jagt
    private float ambushLookahead = 4f;   // Wie weit Ambusher vorausschaut

    void Start()
    {
        col = GetComponent<BoxCollider2D>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerMovement = p.GetComponent<PacManMovement2D>();
            }
        }

        if (player != null)
            goldenBloodCollector = player.GetComponent<golden_blood_collector>();

        currentCornerIndex = Random.Range(0, corners.Length);
        patrolTarget = corners[currentCornerIndex];

        scatterTimer = Random.Range(0f, scatterInterval);

        // Schwierigkeit basierend auf aktuellem Level anpassen
        ApplyLevelScaling();

        ChooseRandomDirection();
    }

    void ApplyLevelScaling()
    {
        int level = (LevelManager.Instance != null) ? LevelManager.Instance.currentLevelIndex : 0;


        // RandomWithBias: chase probability increases per level (max 85 %)
        randomBias = Mathf.Min(0.4f + level * 0.03f, 0.85f);

        // Patroller: chase range grows per level (max 14 tiles)
        patrolChaseRange = Mathf.Min(6f + level * 0.53f, 14f);

        // Ambusher: lookahead distance grows per level (max 8 tiles)
        ambushLookahead = Mathf.Min(4f + level * 0.27f, 8f);
    }

    void Update()
    {
        if (tilemap == null) return;

        UpdateScatterTimer();

        Vector3Int currentCell = tilemap.WorldToCell(transform.position);

        if (IsNearCellCenter(currentCell))
        {
            if (currentCell != lastDecisionCell)
            {
                if (IsAtIntersection() || !IsFree(currentDir))
                {
                    lastDecisionCell = currentCell;
                    MakeDecision(currentCell);
                }
                else
                {
                    lastDecisionCell = currentCell;
                }
            }
        }
    }

    void FixedUpdate()
    {
        transform.Translate(currentDir * speed * Time.fixedDeltaTime);
    }

    void UpdateScatterTimer()
    {
        scatterTimer += Time.deltaTime;

        if (!isScattering && scatterTimer >= scatterInterval)
        {
            isScattering = true;
            scatterTimer = 0f;
        }
        else if (isScattering && scatterTimer >= scatterDuration)
        {
            isScattering = false;
            scatterTimer = 0f;
        }
    }

    void MakeDecision(Vector3Int cell)
    {
        Vector2 oldDir = currentDir;

        if (goldenBloodCollector != null && goldenBloodCollector.invincible)
        {
            FleeFromPlayer();
        }
        else if (isScattering)
        {
            ChooseRandomDirection();
        }
        else
        {
            switch (personality)
            {
                case GhostPersonality.Chaser: ChasePlayer(); break;
                case GhostPersonality.Ambusher: AmbushPlayer(); break;
                case GhostPersonality.Random: RandomWithBias(); break;
                case GhostPersonality.Patroller: PatrolBehavior(); break;
            }
        }

        if (currentDir != oldDir && currentDir != Vector2.zero)
            transform.position = tilemap.GetCellCenterWorld(cell);
    }

    // -- AI Behaviour --

    void ChasePlayer()
    {
        if (player == null) { ChooseRandomDirection(); return; }
        MoveTowardsTarget(player.position);
    }

    void AmbushPlayer()
    {
        if (player == null) { ChooseRandomDirection(); return; }

        Vector2 playerDir = Vector2.zero;
        if (playerMovement != null)
            playerDir = ((Vector2)player.position - lastPlayerPos).normalized;

        Vector3 target = player.position + (Vector3)(playerDir * ambushLookahead);
        MoveTowardsTarget(target);
    }

    void RandomWithBias()
    {
        if (player == null) { ChooseRandomDirection(); return; }

        if (Random.value < randomBias)
            MoveTowardsTarget(player.position);
        else
            ChooseRandomDirection();
    }

    void PatrolBehavior()
    {
        if (player == null) { ChooseRandomDirection(); return; }

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (distToPlayer < patrolChaseRange)
        {
            ChasePlayer();
            return;
        }

        float distToCorner = Vector2.Distance(transform.position, patrolTarget);
        if (distToCorner < 2f)
        {
            currentCornerIndex = (currentCornerIndex + 1) % corners.Length;
            patrolTarget = corners[currentCornerIndex];
        }

        MoveTowardsTarget(patrolTarget);
    }

    void FleeFromPlayer()
    {
        if (player == null) { ChooseRandomDirection(); return; }
        Vector3 target = transform.position + (transform.position - player.position);
        MoveTowardsTarget(target);
    }

    // -- Movement Logic --

    void MoveTowardsTarget(Vector3 targetPos)
    {
        Vector2[] possibleDirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 bestDir = currentDir;
        float bestScore = float.MaxValue;

        Vector2 opposite = -currentDir;
        bool foundValidDir = false;

        foreach (Vector2 dir in possibleDirs)
        {
            if (dir == opposite) continue;
            if (!IsFree(dir)) continue;

            float dist = Vector2.Distance((Vector2)transform.position + dir, targetPos);
            float deadEndPenalty = IsDeadEnd(dir) ? 5f : 0f;
            float score = dist + deadEndPenalty;

            if (score < bestScore)
            {
                bestScore = score;
                bestDir = dir;
                foundValidDir = true;
            }
        }

        if (!foundValidDir && IsFree(opposite))
            currentDir = opposite;
        else
            currentDir = bestDir;
    }

    bool IsDeadEnd(Vector2 dir)
    {
        if (!IsFree(dir)) return true;

        Vector2 nextPos = (Vector2)transform.position + dir;
        int exits = 0;

        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 d in dirs)
        {
            if (d == -dir) continue;
            RaycastHit2D hit = Physics2D.BoxCast(nextPos, Vector2.one * 0.8f, 0f, d, 1.0f, wallMask);
            if (hit.collider == null) exits++;
        }

        return exits == 0;
    }

    void ChooseRandomDirection()
    {
        List<Vector2> validDirs = new List<Vector2>();
        Vector2 opposite = -currentDir;

        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 dir in dirs)
        {
            if (dir != opposite && IsFree(dir))
                validDirs.Add(dir);
        }

        if (validDirs.Count > 0)
            currentDir = validDirs[Random.Range(0, validDirs.Count)];
        else if (IsFree(opposite))
            currentDir = opposite;
    }

    // -- Helpers --

    private Vector2 lastPlayerPos;
    void LateUpdate()
    {
        if (player != null) lastPlayerPos = player.position;
    }

    bool IsNearCellCenter(Vector3Int cell)
    {
        Vector3 centerWorld = tilemap.GetCellCenterWorld(cell);
        return Vector2.Distance(transform.position, centerWorld) < centerTolerance;
    }

    bool IsAtIntersection()
    {
        int ways = 0;
        if (IsFree(Vector2.up)) ways++;
        if (IsFree(Vector2.down)) ways++;
        if (IsFree(Vector2.left)) ways++;
        if (IsFree(Vector2.right)) ways++;

        if (ways > 2) return true;
        if (ways == 2)
        {
            bool horizontal = IsFree(Vector2.left) && IsFree(Vector2.right);
            bool vertical = IsFree(Vector2.up) && IsFree(Vector2.down);
            return (!horizontal && !vertical);
        }

        return false;
    }

    bool IsFree(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, Vector2.one * 0.8f, 0f, dir, 1.0f, wallMask);
        return hit.collider == null;
    }
}