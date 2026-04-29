using UnityEngine;

/// <summary>FSM + Seek — pensum AI.</summary>
public class EnemyFSM : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Stunned,
        Dead
    }

    [SerializeField] private EnemyState currentState = EnemyState.Patrol;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float waypointReachDistance = 0.4f;
    private int currentWaypointIndex;

    [SerializeField] private float detectionRadius = 7f;
    [SerializeField] private float giveUpRadius = 13f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float stunDuration = 1f;
    private float stunTimer;

    [SerializeField] private int maxHealth = 2;
    private int currentHealth;
    [SerializeField] private int damageToPlayer = 1;
    [SerializeField] private ParticleSystem deathEffect;
    [SerializeField] private Animator animator;

    private static readonly int AnimWalking = Animator.StringToHash("IsWalking");
    private static readonly int AnimChasing = Animator.StringToHash("IsChasing");
    private static readonly int AnimHit = Animator.StringToHash("Hit");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    private Transform player;
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning($"EnemyFSM på {name}: Ingen 'Player' tag.");
    }

    private void Update()
    {
        if (isDead) return;

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol_Update();
                CheckForPlayer();
                break;
            case EnemyState.Chase:
                Chase_Update();
                CheckGiveUp();
                break;
            case EnemyState.Stunned:
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                    ChangeState(EnemyState.Patrol);
                break;
            case EnemyState.Dead:
                break;
        }
    }

    private void Patrol_Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude < waypointReachDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            new Vector3(target.position.x, transform.position.y, target.position.z),
            moveSpeed * Time.deltaTime);

        if (direction.magnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);

        animator?.SetBool(AnimWalking, true);
        animator?.SetBool(AnimChasing, false);
    }

    private void Chase_Update()
    {
        if (player == null) return;

        Vector3 rawDirection = player.position - transform.position;
        Vector3 normDirection = rawDirection.normalized;
        normDirection.y = 0f;

        transform.position = Vector3.MoveTowards(
            transform.position,
            new Vector3(player.position.x, transform.position.y, player.position.z),
            chaseSpeed * Time.deltaTime);

        if (normDirection.magnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(normDirection), 12f * Time.deltaTime);

        animator?.SetBool(AnimChasing, true);
        animator?.SetBool(AnimWalking, false);
    }

    private void CheckForPlayer()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) < detectionRadius)
            ChangeState(EnemyState.Chase);
    }

    private void CheckGiveUp()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > giveUpRadius)
            ChangeState(EnemyState.Patrol);
    }

    private void ChangeState(EnemyState newState) => currentState = newState;

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        collision.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(damageToPlayer);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        animator?.SetTrigger(AnimHit);

        if (currentHealth <= 0)
            Die();
        else
        {
            ChangeState(EnemyState.Stunned);
            stunTimer = stunDuration;
        }
    }

    private void Die()
    {
        isDead = true;
        ChangeState(EnemyState.Dead);
        animator?.SetTrigger(AnimDie);

        if (deathEffect != null)
        {
            deathEffect.transform.parent = null;
            deathEffect.Play();
            Destroy(deathEffect.gameObject, 2f);
        }

        AudioManager.Instance?.PlayEnemyDeath();
        GameManager.Instance?.RegisterEnemyDefeated();

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, giveUpRadius);
    }
}
