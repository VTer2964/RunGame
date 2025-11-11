using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class FrogMovement : MonoBehaviour
{
    [SerializeField] private bool muteJumpSound = false;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float pauseAtBottom = 1f;
    [SerializeField] private float horizontalForce = 3f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Sound - Frog Jump")]
    [SerializeField] private AudioClip frogJumpClip;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isWaiting;

    private EnemySoundController soundController;
    private AudioSource jumpAudioSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 4.5f;

        soundController = GetComponent<EnemySoundController>();
        if (soundController != null)
        {
            var scType = typeof(EnemySoundController);
            var field = scType.GetField("continuousLoop",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(soundController, false);
        }

        jumpAudioSource = GetComponent<AudioSource>();
        if (jumpAudioSource == null)
            jumpAudioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        // Khi vừa chạm đất
        if (isGrounded && !wasGrounded)
        {
            animator.SetBool("isJumping", false); // trở về Idle
            if (!isWaiting)
                StartCoroutine(JumpRoutine());
        }
    }

    private IEnumerator JumpRoutine()
    {
        isWaiting = true;

        yield return new WaitForSeconds(pauseAtBottom);

        if (isGrounded)
        {
            Jump(); // animation được bật ngay tại đây
        }

        isWaiting = false;
    }

    private void Jump()
    {
        // bật animation ngay khi bắt đầu nhảy
        animator.SetBool("isJumping", true);

        // đổi hướng
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

        // reset vận tốc
        rb.linearVelocity = Vector2.zero;

        // tạo lực nhảy
        Vector2 jumpDir = new Vector2(transform.localScale.x > 0 ? 1 : -1, 1).normalized;
        rb.AddForce(new Vector2(jumpDir.x * horizontalForce, jumpDir.y * jumpForce), ForceMode2D.Impulse);

        // phát âm thanh
        if (!muteJumpSound && frogJumpClip != null)
        {
            if (soundController != null)
            {
                var src = GetComponent<AudioSource>();
                if (src != null)
                {
                    src.spatialBlend = 1f;
                    src.rolloffMode = AudioRolloffMode.Linear;
                    src.minDistance = 1f;
                    src.maxDistance = 3f;
                }
                soundController.PlayOneShot3D(frogJumpClip);
            }
            else if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ConfigureEnemy3DSource(jumpAudioSource);
                jumpAudioSource.spatialBlend = 1f;
                jumpAudioSource.minDistance = 1f;
                jumpAudioSource.maxDistance = 3f;
                SoundManager.Instance.PlayEnemyOneShot3D(jumpAudioSource, frogJumpClip, 1f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
