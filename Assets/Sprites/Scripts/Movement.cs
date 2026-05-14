using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
/// <summary>Pac-Man-style tile-based player movement with keyboard and swipe input.</summary>
public class PacManMovement2D : MonoBehaviour
{
    public Tilemap tilemap;
    public float speed = 3f; // Faster than ~3 makes corner navigation unreliable
    public float centerTolerance = 0.1f; // Generous tolerance for smooth cell snapping
    public LayerMask wallMask;

    private Vector2 currentDir = Vector2.zero;
    private Vector2 nextDir = Vector2.zero;

    private Vector3 fp;
    private Vector3 lp;
    private float dragDistance;
    private BoxCollider2D col;
    private Animator animator;

    void Start()
    {
        col = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        dragDistance = Screen.height * 5 / 100;
    }

    void Update()
    {
        if (tilemap == null) return;

        HandleInput();

        // Immediate 180-degree U-turn: allow reversing direction without waiting for a cell centre
        if (nextDir == -currentDir && nextDir != Vector2.zero)
        {
            currentDir = nextDir;
        }

        // Turn at intersections (90 degrees)
        if (IsInCellCenter())
        {
            if (nextDir != Vector2.zero && IsFree(nextDir))
            {
                SnapToCellCenter();
                currentDir = nextDir;
            }
            // Stop exactly at cell centre when the forward path is blocked
            else if (!IsFree(currentDir))
            {
                SnapToCellCenter(); // Exakt in der Mitte stoppen
                currentDir = Vector2.zero;
            }
        }
    }

    private void HandleInput()
    {
        // Keyboard
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) nextDir = Vector2.up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) nextDir = Vector2.down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) nextDir = Vector2.left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) nextDir = Vector2.right;

        // Touch-Eingabe
        HandleSwipeInput();
    }

    private void HandleSwipeInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) { fp = touch.position; lp = touch.position; }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Ended)
            {
                lp = touch.position;
                if (Mathf.Abs(lp.x - fp.x) > dragDistance || Mathf.Abs(lp.y - fp.y) > dragDistance)
                {
                    if (Mathf.Abs(lp.x - fp.x) > Mathf.Abs(lp.y - fp.y))
                        nextDir = (lp.x > fp.x) ? Vector2.right : Vector2.left;
                    else
                        nextDir = (lp.y > fp.y) ? Vector2.up : Vector2.down;
                    fp = lp;
                }
            }
        }
    }

    void FixedUpdate()
    {
        transform.position += (Vector3)(currentDir * speed * Time.fixedDeltaTime);

        if (animator != null)
            animator.SetBool("walking", currentDir != Vector2.zero);
    }

    bool IsFree(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;

        // Slightly undersized box cast to avoid snagging on tile corners
        Vector2 castSize = col.size * 0.8f;
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, castSize, 0f, dir, 0.6f, wallMask);

        return hit.collider == null;
    }

    bool IsInCellCenter()
    {
        Vector3Int cell = tilemap.WorldToCell(transform.position);
        Vector3 center = tilemap.GetCellCenterWorld(cell);

        return Vector2.Distance(transform.position, center) < centerTolerance;
    }

    void SnapToCellCenter()
    {
        Vector3Int cell = tilemap.WorldToCell(transform.position);
        Vector3 center = tilemap.GetCellCenterWorld(cell);

        transform.position = center;
    }
    public void StopImmediately()
    {
        currentDir = Vector2.zero;
        nextDir = Vector2.zero;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (LevelManager.Instance != null)
            {
                // Wir übergeben das getroffene Objekt an den LevelManager
                LevelManager.Instance.OnPlayerHit(collision.gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckEnemyContact(other.gameObject);
    }


    private void CheckEnemyContact(GameObject contactedObject)
    {
        if (contactedObject.CompareTag("Enemy"))
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnPlayerHit(contactedObject);
            }
        }
    }

}