using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float jumpForce = 12f;
    public float horizontalForce = 5f;
    public float bounceForce = 3f;
    public float bounceUpForce = 3f;

    [Header("Gravity")]
    public float fallMultiplier = 2.5f;
    public float gravityDelay = 0.15f;

    private float gravityDelayTimer;

    [Header("Input")]
    public float inputLockAfterBounce = 0.15f;

    private float inputLockTimer;

    [Header("Hit / Knockback")]
    public float hitBounceForce = 6f;
    public float hitUpForce = 4f;
    public float slideFriction = 0.95f;

    private bool isHit;

    [Header("Check")]
    public LayerMask wallLayer;

    private Rigidbody2D rb;
    private int direction = 1; // 1 = right, -1 = left

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isHit)
        {
            HandleSlide();
            HandleGravity();
            return; // ❗ khóa input hoàn toàn khi bị hit
        }

        HandleInput();
        HandleGravity();

        if (inputLockTimer > 0)
            inputLockTimer -= Time.deltaTime;
    }

    void HandleSlide()
    {
        // giảm tốc dần khi trượt
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x * slideFriction,
            rb.linearVelocity.y
        );
    }

    public float hitForceMultiplier = 1.5f;
    public float hitFreezeTime = 0.05f;
    public void Hit(Vector2 hitSource)
    {
        isHit = true;

        Vector2 hitDir = ((Vector2)transform.position - hitSource).normalized;
        hitDir.y = Mathf.Abs(hitDir.y) + 0.5f;
        hitDir.Normalize();

        rb.linearVelocity = Vector2.zero;

        // FREEZE FRAME (cực quan trọng)
        StartCoroutine(HitFreeze(hitDir));

        gravityDelayTimer = 0;
    }

    System.Collections.IEnumerator HitFreeze(Vector2 dir)
    {
        float originalTimeScale = Time.timeScale;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitFreezeTime);

        Time.timeScale = originalTimeScale;
        CameraShake.Instance.Shake(0.15f, 0.2f);

        rb.linearVelocity = new Vector2(
            dir.x * hitBounceForce * hitForceMultiplier,
            hitUpForce * hitForceMultiplier
        );
    }

    void HandleGravity()
    {
        // giảm timer
        if (gravityDelayTimer > 0)
        {
            gravityDelayTimer -= Time.deltaTime;
            return;
        }

        // bắt đầu tăng gravity khi rơi
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
    }

    void HandleInput()
    {
        if (inputLockTimer > 0) return;

        // Nhấn → jump
        if (Input.GetMouseButtonDown(0))
        {
            Jump();
        }

        // THÊM ĐOẠN NÀY 👇 (Jump Cut)
        if (Input.GetMouseButtonUp(0) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * 0.5f // chỉnh 0.3f - 0.7f để feel
            );
        }
    }

    void Jump()
    {
        rb.linearVelocity = Vector2.zero;

        Vector2 force = new Vector2(direction * horizontalForce, jumpForce);
        rb.AddForce(force, ForceMode2D.Impulse);

        gravityDelayTimer = gravityDelay;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            HandleWallBounce(collision);
        }
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Spike"))
        {
            Hit(collision.transform.position);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (isHit && rb.linearVelocity.y <= 0)
        {
            if (collision.contacts[0].normal.y > 0.5f)
            {
                Slam();
            }
        }
    }

    void Slam()
    {
        isHit = false;

        rb.linearVelocity = new Vector2(0, 0);

        inputLockTimer = 0.1f; // delay nhẹ cho feel

        // 👉 sau này thêm:
        // screen shake
        // particle dust
    }

    void HandleWallBounce(Collision2D collision)
    {
        // lấy normal của bề mặt va chạm
        Vector2 normal = collision.contacts[0].normal;

        // nếu là tường bên trái/phải
        if (Mathf.Abs(normal.x) > 0.5f)
        {
            Flip();

            // reset velocity trước để control sạch
            rb.linearVelocity = Vector2.zero;

            // bật ngược lại
            Vector2 bounceDir = new Vector2(direction, 0).normalized;

            rb.linearVelocity = new Vector2(
                bounceDir.x * bounceForce,
                bounceUpForce
            );
        }
        gravityDelayTimer = gravityDelay;
        inputLockTimer = inputLockAfterBounce;
    }

    void Flip()
    {
        direction *= -1;

        // lật sprite cho đẹp (optional)
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}

