using UnityEngine;

/// <summary>
/// CharacterController + Input.GetAxis (pensum).
/// Robot Kyle bruker Starter Assets Animator: Jump = bool, MotionSpeed, FreeFall, Speed — ikke bare trigger.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundMask;

    [Header("Animator (Starter Assets / Robot Kyle)")]
    [Tooltip("Tid før FreeFall settes (samme idé som Starter Assets FallTimeout).")]
    [SerializeField] private float fallTimeoutDuration = 0.15f;
    [SerializeField] private float animSpeedLerp = 12f;

    public float speedMultiplier = 1f;
    public float jumpMultiplier = 1f;

    private CharacterController cc;
    private Vector3 velocity;
    private bool isGrounded;
    private bool canDoubleJump;
    private bool hasDoubleJumped;
    private float speedBoostMultiplier = 1f;
    private Animator animator;

    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimJump = Animator.StringToHash("Jump");

    private int animGroundedHash;
    private bool animHasGroundedParam;
    private int animFreeFallHash;
    private bool animHasFreeFallParam;
    private int animMotionSpeedHash;
    private bool animHasMotionSpeedParam;
    private bool animHasJumpParam;
    private bool jumpParameterIsTrigger;
    private float fallTimeoutDelta;
    private float animSpeedBlend;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (cc == null)
            Debug.LogError("PlayerController: Mangler CharacterController.");
        if (groundMask.value == 0)
            groundMask = ~0;
        ResolveAnimatorParams();
    }

    void ResolveAnimatorParams()
    {
        animHasGroundedParam = false;
        animHasFreeFallParam = false;
        animHasMotionSpeedParam = false;
        animHasJumpParam = false;
        jumpParameterIsTrigger = false;
        if (animator == null) return;

        foreach (var p in animator.parameters)
        {
            switch (p.name)
            {
                case "IsGrounded":
                case "Grounded":
                    if (p.type == AnimatorControllerParameterType.Bool)
                    {
                        animGroundedHash = p.nameHash;
                        animHasGroundedParam = true;
                    }
                    break;
                case "FreeFall":
                    if (p.type == AnimatorControllerParameterType.Bool)
                    {
                        animFreeFallHash = p.nameHash;
                        animHasFreeFallParam = true;
                    }
                    break;
                case "MotionSpeed":
                    if (p.type == AnimatorControllerParameterType.Float)
                    {
                        animMotionSpeedHash = p.nameHash;
                        animHasMotionSpeedParam = true;
                    }
                    break;
                case "Jump":
                    if (p.type == AnimatorControllerParameterType.Trigger ||
                        p.type == AnimatorControllerParameterType.Bool)
                    {
                        animHasJumpParam = true;
                        jumpParameterIsTrigger = p.type == AnimatorControllerParameterType.Trigger;
                    }
                    break;
            }
        }
    }

    private void Start()
    {
        if (GameManager.Instance?.SelectedCharacter != null)
        {
            speedMultiplier = GameManager.Instance.SelectedCharacter.speedMultiplier;
            jumpMultiplier = GameManager.Instance.SelectedCharacter.jumpMultiplier;
        }

        fallTimeoutDelta = fallTimeoutDuration;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool hasMoveInput = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        Transform camTf = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = camTf != null ? camTf.forward : transform.forward;
        Vector3 right = camTf != null ? camTf.right : transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * v + right * h;
        if (moveDir.sqrMagnitude > 1e-4f)
            moveDir.Normalize();

        float currentSpeed = moveSpeed * speedMultiplier * speedBoostMultiplier;
        if (Input.GetButton("Fire3"))
            currentSpeed *= sprintMultiplier;

        Vector3 horizontalVel = hasMoveInput ? moveDir * currentSpeed : Vector3.zero;

        if (hasMoveInput)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);
        }

        bool groundedBeforeMove = SphereGrounded() || cc.isGrounded;

        if (Input.GetButtonDown("Jump"))
        {
            if (groundedBeforeMove)
            {
                velocity.y = jumpForce * jumpMultiplier;
                SetJumpAnimator(true);
                AudioManager.Instance?.PlayJump();
            }
            else if (canDoubleJump && !hasDoubleJumped)
            {
                velocity.y = jumpForce * jumpMultiplier * 0.85f;
                hasDoubleJumped = true;
                SetJumpAnimator(true);
            }
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 displacement = horizontalVel * Time.deltaTime + new Vector3(0f, velocity.y, 0f) * Time.deltaTime;
        cc.Move(displacement);

        isGrounded = cc.isGrounded || SphereGrounded();

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            hasDoubleJumped = false;
            SetJumpAnimator(false);
            fallTimeoutDelta = fallTimeoutDuration;
            if (animHasFreeFallParam)
                animator.SetBool(animFreeFallHash, false);
        }
        else
        {
            fallTimeoutDelta -= Time.deltaTime;
            if (animHasFreeFallParam && fallTimeoutDelta <= 0f)
                animator.SetBool(animFreeFallHash, true);
        }

        UpdateAnimator(horizontalVel.magnitude, hasMoveInput);
    }

    bool SphereGrounded()
    {
        Vector3 worldCenter = transform.TransformPoint(cc.center);
        float down = cc.height * 0.5f - cc.radius + cc.skinWidth;
        Vector3 probe = worldCenter + Vector3.down * down;
        return Physics.CheckSphere(probe, Mathf.Max(groundCheckRadius, cc.radius * 0.85f), groundMask,
            QueryTriggerInteraction.Ignore);
    }

    void SetJumpAnimator(bool value)
    {
        if (animator == null || !animHasJumpParam) return;
        if (jumpParameterIsTrigger)
        {
            if (value)
                animator.SetTrigger(AnimJump);
        }
        else
        {
            animator.SetBool(AnimJump, value);
        }
    }

    private void UpdateAnimator(float horizontalSpeed, bool hasMoveInput)
    {
        if (animator == null) return;

        float target = hasMoveInput ? horizontalSpeed : 0f;
        animSpeedBlend = Mathf.Lerp(animSpeedBlend, target, Mathf.Clamp01(Time.deltaTime * animSpeedLerp));
        if (target < 0.01f && animSpeedBlend < 0.05f)
            animSpeedBlend = 0f;

        animator.SetFloat(AnimSpeed, animSpeedBlend);

        if (animHasMotionSpeedParam)
        {
            // Starter Assets: 1 med tastatur når det er input, ellers 0 — styrer avspillingshastighet i «Idle Walk Run Blend».
            float motion = hasMoveInput
                ? Mathf.Max(Mathf.Abs(Input.GetAxisRaw("Horizontal")), Mathf.Abs(Input.GetAxisRaw("Vertical")))
                : 0f;
            animator.SetFloat(animMotionSpeedHash, Mathf.Clamp01(motion));
        }

        if (animHasGroundedParam)
            animator.SetBool(animGroundedHash, isGrounded);
    }

    public void SetDoubleJump(bool active) => canDoubleJump = active;
    public void SetSpeedBoost(float multiplier) => speedBoostMultiplier = multiplier;
    public void ResetSpeedBoost() => speedBoostMultiplier = 1f;
    public void ApplyJumpPadForce(float force) => velocity.y = force;

    /// <summary>
    /// Robot Kyle-animasjoner har Animation Events mot Starter Assets (OnLand, OnFootstep).
    /// </summary>
    public void OnLand() { }

    public void OnFootstep() { }

    private void OnDrawGizmosSelected()
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        if (cc == null) return;
        Gizmos.color = Color.green;
        Vector3 worldCenter = transform.TransformPoint(cc.center);
        float down = cc.height * 0.5f - cc.radius + cc.skinWidth;
        Vector3 probe = worldCenter + Vector3.down * down;
        Gizmos.DrawWireSphere(probe, Mathf.Max(groundCheckRadius, cc.radius * 0.85f));
    }
}
