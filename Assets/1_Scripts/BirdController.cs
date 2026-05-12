using System.Collections;
using UnityEngine;

/// <summary>
/// X Project — Core bird controller
/// Attach to Player GameObject (cần: Rigidbody2D, Collider2D)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BirdController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 6f;          // tốc độ bay
    [SerializeField] private float verticalRatio = 0.7f; // tỉ lệ vY / vX (góc chéo)

    [Header("Input Lock")]
    [SerializeField] private float inputLockDuration = 0.5f; // giây bị khóa sau khi trúng đòn

    [Header("Bounce Back")]
    [SerializeField] private float bounceForce = 8f;    // lực bắn ngược khi trúng hazard

    // --- State ---
    private Rigidbody2D rb;
    private int directionX = 1;       // 1 = phải, -1 = trái
    private bool inputLocked = false;
    private bool isAlive = true;

    // Events để các system khác lắng nghe (skill, UI, v.v.)
    public System.Action OnBounceBack;
    public System.Action OnDie;

    // -------------------------------------------------------

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;         // không có gravity — pure diagonal
        rb.freezeRotation = true;
    }

    void Start()
    {
        ApplylinearVelocity();
    }

    void Update()
    {
        if (!isAlive) return;

        // Input: tap / click / spacebar
        if (!inputLocked && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            FlipDirection();
        }
    }

    // -------------------------------------------------------
    // Public API — dùng cho skills và external systems
    // -------------------------------------------------------

    /// <summary>Flip hướng X ngay lập tức (dùng cho skill Instant Flip)</summary>
    public void FlipDirection()
    {
        directionX *= -1;
        ApplylinearVelocity();
    }

    /// <summary>Khóa input trong N giây (dùng sau khi trúng hazard)</summary>
    public void LockInput(float duration = -1f)
    {
        float d = duration > 0 ? duration : inputLockDuration;
        StopCoroutine(nameof(InputLockRoutine));
        StartCoroutine(InputLockRoutine(d));
    }

    /// <summary>Bounce back — bắn nhân vật theo hướng ngược lại</summary>
    public void TriggerBounceBack()
    {
        directionX *= -1;
        rb.linearVelocity = new Vector2(-rb.linearVelocity.x, bounceForce) * 0.8f;
        LockInput();
        OnBounceBack?.Invoke();
    }

    /// <summary>Slow down tốc độ (dùng cho skill Slow Field từ phía enemy)</summary>
    public void ApplySpeedMultiplier(float multiplier, float duration)
    {
        StartCoroutine(SpeedMultiplierRoutine(multiplier, duration));
    }

    public void Die()
    {
        if (!isAlive) return;
        isAlive = false;
        rb.linearVelocity = Vector2.zero;
        OnDie?.Invoke();
        // Thêm animation / respawn logic ở đây sau
    }

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    void ApplylinearVelocity()
    {
        rb.linearVelocity = new Vector2(directionX * speed, speed * verticalRatio);
    }

    IEnumerator InputLockRoutine(float duration)
    {
        inputLocked = true;
        yield return new WaitForSeconds(duration);
        inputLocked = false;
    }

    IEnumerator SpeedMultiplierRoutine(float multiplier, float duration)
    {
        float originalSpeed = speed;
        speed *= multiplier;
        ApplylinearVelocity();
        yield return new WaitForSeconds(duration);
        speed = originalSpeed;
        ApplylinearVelocity();
    }

    // -------------------------------------------------------
    // Collision
    // -------------------------------------------------------

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!isAlive) return;

        if (col.gameObject.CompareTag("Wall"))
        {
            // Chạm tường → flip X tự động, giữ vY
            directionX *= -1;
            rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);
        }
        else if (col.gameObject.CompareTag("Hazard"))
        {
            // Gai, saw, v.v. → bounce back + lock input
            TriggerBounceBack();
        }
        else if (col.gameObject.CompareTag("Lethal"))
        {
            // Instant kill (gai đặc biệt, pit, v.v.)
            Die();
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!isAlive) return;

        if (col.CompareTag("Checkpoint"))
        {
            //CheckpointManager.Instance?.RegisterCheckpoint(transform.position);
        }
    }
}