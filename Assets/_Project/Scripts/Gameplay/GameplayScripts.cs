using UnityEngine;
using UnityEngine.Events;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 6, -12);
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float mouseSensitivity = 2f;

    private float yaw;
    private float pitch = 15f;

    private void Start()
    {
        if (!CompareTag("MainCamera"))
            gameObject.tag = "MainCamera";

        var cam = GetComponent<Camera>();
        if (cam != null && !cam.enabled)
            cam.enabled = true;

        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -10f, 60f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position + rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    public void SetTarget(Transform t) => target = t;
}

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    public UnityEvent<int, int> OnHealthChanged = new UnityEvent<int, int>();
    public UnityEvent OnDeath = new UnityEvent();

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    private void Awake() => currentHealth = maxHealth;

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        AudioManager.Instance?.PlayDamage();
        OnHealthChanged.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            OnDeath.Invoke();
            GetComponent<PlayerRespawn>()?.Respawn();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }
}

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private Vector3 defaultSpawnPoint = new Vector3(0, 2, 0);

    private Vector3 lastCheckpoint;
    private bool isRespawning;

    private void Start() => lastCheckpoint = defaultSpawnPoint;

    public void SetCheckpoint(Vector3 pos) => lastCheckpoint = pos;

    public void Respawn()
    {
        if (isRespawning) return;
        StartCoroutine(DoRespawn());
    }

    private System.Collections.IEnumerator DoRespawn()
    {
        isRespawning = true;
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = lastCheckpoint + Vector3.up;
        if (cc != null) cc.enabled = true;
        GetComponent<PlayerHealth>()?.ResetHealth();
        isRespawning = false;
    }
}

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] public Transform pointA;
    [SerializeField] public Transform pointB;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float waitTime = 0.8f;

    private Vector3 target;
    private bool waiting;

    private void Start()
    {
        if (pointA != null && pointB != null)
            target = pointB.position;
        else
            enabled = false;
    }

    private void Update()
    {
        if (waiting || pointA == null || pointB == null) return;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.05f)
            StartCoroutine(Switch());
    }

    private System.Collections.IEnumerator Switch()
    {
        waiting = true;
        yield return new WaitForSeconds(waitTime);
        target = (target == pointA.position) ? pointB.position : pointA.position;
        waiting = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            other.transform.SetParent(transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            other.transform.SetParent(null);
    }
}

public class FallingPlatform : MonoBehaviour
{
    [SerializeField] private float fallDelay = 0.8f;
    [SerializeField] private float respawnTime = 4f;
    [SerializeField] private Color warningColor = Color.red;

    private Rigidbody rb;
    private Vector3 startPos;
    private Quaternion startRot;
    private Color originalColor;
    private Renderer rend;
    private bool falling;

    private void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        startPos = transform.position;
        startRot = transform.rotation;
        rend = GetComponent<Renderer>();
        if (rend != null) originalColor = rend.material.color;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Player") || falling) return;
        StartCoroutine(Fall());
    }

    private System.Collections.IEnumerator Fall()
    {
        falling = true;
        if (rend != null) rend.material.color = warningColor;
        yield return new WaitForSeconds(fallDelay * 0.5f);
        if (rend != null) rend.material.color = originalColor;
        yield return new WaitForSeconds(fallDelay * 0.5f);

        rb.isKinematic = false;

        yield return new WaitForSeconds(respawnTime);

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(startPos, startRot);
        falling = false;
    }
}

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float force = 20f;
    [SerializeField] private ParticleSystem bounceEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var pc = other.GetComponent<PlayerController>();
        if (pc != null) pc.ApplyJumpPadForce(force);

        bounceEffect?.Play();
        AudioManager.Instance?.PlayJump();
    }
}

public class Hazard : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private bool killInstantly;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        if (killInstantly)
            health.TakeDamage(health.MaxHealth);
        else
            health.TakeDamage(damage);
    }
}

public class Collectible : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private ParticleSystem collectEffect;

    private Vector3 startPos;

    private void Start() => startPos = transform.position;

    private void Update()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager.Instance?.AddOrb(scoreValue);
        AudioManager.Instance?.PlayCollectOrb();

        if (collectEffect != null)
        {
            collectEffect.transform.parent = null;
            collectEffect.Play();
            Destroy(collectEffect.gameObject, 2f);
        }

        Destroy(gameObject);
    }
}

public class Checkpoint : MonoBehaviour
{
    [SerializeField] public Renderer flagRenderer;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private ParticleSystem activateEffect;

    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || activated) return;
        activated = true;

        if (flagRenderer != null)
            flagRenderer.material.color = activeColor;

        activateEffect?.Play();
        AudioManager.Instance?.PlayCheckpoint();

        other.GetComponent<PlayerRespawn>()?.SetCheckpoint(transform.position);
        CheckpointManager.Instance?.RegisterCheckpoint(this);
    }
}

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    private Checkpoint lastCheckpoint;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterCheckpoint(Checkpoint cp) => lastCheckpoint = cp;
    public Checkpoint GetLastCheckpoint() => lastCheckpoint;
}

public class FinishPortal : MonoBehaviour
{
    [SerializeField] private string nextScene = "";
    [SerializeField] private ParticleSystem portalEffect;
    [SerializeField] private Light portalLight;

    private bool triggered;

    private void Start()
    {
        // Ikke bruk ?. på uassignerte SerializeField — Unity kan kaste UnassignedReferenceException.
        if (portalEffect)
            portalEffect.Play();
        if (portalLight)
            StartCoroutine(PulsateLight());
    }

    private System.Collections.IEnumerator PulsateLight()
    {
        if (!portalLight) yield break;
        float baseIntensity = portalLight.intensity;
        while (portalLight)
        {
            portalLight.intensity = baseIntensity + Mathf.Sin(Time.time * 3f) * 0.8f;
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || triggered) return;
        triggered = true;

        AudioManager.Instance?.PlayPortal();
        AudioManager.Instance?.PlayLevelComplete();

        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        int orbs = GameManager.Instance != null ? GameManager.Instance.OrbsCollected : 0;
        float time = LevelTimer.Instance != null ? LevelTimer.Instance.ElapsedTime : 0f;

        var ui = FindFirstObjectByType<LevelCompleteUI>();
        if (ui != null)
        {
            ui.Show(score, orbs, time);
            return;
        }

        if (!string.IsNullOrEmpty(nextScene))
            GameManager.Instance?.LoadScene(nextScene);
        else
            GameManager.Instance?.LoadScene("MainMenu");
    }
}

public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }

    private float elapsed;
    private bool running = true;

    public float ElapsedTime => elapsed;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        if (running && !paused)
            elapsed += Time.deltaTime;
    }

    public void Stop() => running = false;
    public void ResetTimer() { elapsed = 0f; running = true; }

    public string GetFormattedTime()
    {
        int min = (int)(elapsed / 60);
        int sec = (int)(elapsed % 60);
        return $"{min:00}:{sec:00}";
    }
}

public class VehicleController : MonoBehaviour
{
    [SerializeField] private float motorForce = 800f;
    [SerializeField] private float brakeForce = 1200f;
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private WheelCollider frontLeftWheel;
    [SerializeField] private WheelCollider frontRightWheel;
    [SerializeField] private WheelCollider rearLeftWheel;
    [SerializeField] private WheelCollider rearRightWheel;
    [SerializeField] private Transform frontLeftTransform;
    [SerializeField] private Transform frontRightTransform;
    [SerializeField] private Transform rearLeftTransform;
    [SerializeField] private Transform rearRightTransform;

    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isBraking = Input.GetButton("Fire1");
    }

    private void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
        UpdateWheelVisuals();
    }

    private void HandleMotor()
    {
        float motor = verticalInput * motorForce;
        float brake = isBraking ? brakeForce : 0f;

        rearLeftWheel.motorTorque = motor;
        rearRightWheel.motorTorque = motor;
        frontLeftWheel.brakeTorque = brake;
        frontRightWheel.brakeTorque = brake;
        rearLeftWheel.brakeTorque = brake;
        rearRightWheel.brakeTorque = brake;
    }

    private void HandleSteering()
    {
        float steer = maxSteerAngle * horizontalInput;
        frontLeftWheel.steerAngle = steer;
        frontRightWheel.steerAngle = steer;
    }

    private void UpdateWheelVisuals()
    {
        UpdateWheel(frontLeftWheel, frontLeftTransform);
        UpdateWheel(frontRightWheel, frontRightTransform);
        UpdateWheel(rearLeftWheel, rearLeftTransform);
        UpdateWheel(rearRightWheel, rearRightTransform);
    }

    private void UpdateWheel(WheelCollider col, Transform trans)
    {
        if (trans == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        trans.position = pos;
        trans.rotation = rot;
    }
}

public class PowerUpPickup : MonoBehaviour
{
    public enum PowerUpType { SpeedBoost, DoubleJump, Shield, ExtraHealth }

    [SerializeField] private PowerUpType type = PowerUpType.SpeedBoost;
    [SerializeField] private float duration = 8f;
    [SerializeField] private float rotateSpeed = 80f;
    [SerializeField] private ParticleSystem collectEffect;

    private void Update() => transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        ApplyPowerUp(other.gameObject);
        AudioManager.Instance?.PlayPowerup();

        collectEffect?.transform.SetParent(null);
        collectEffect?.Play();
        if (collectEffect != null) Destroy(collectEffect.gameObject, 2f);

        Destroy(gameObject);
    }

    private void ApplyPowerUp(GameObject player)
    {
        var pc = player.GetComponent<PlayerController>();
        var ph = player.GetComponent<PlayerHealth>();
        var host = player.GetComponent<MonoBehaviour>();

        switch (type)
        {
            case PowerUpType.SpeedBoost:
                if (pc != null && host != null) host.StartCoroutine(SpeedBoost(pc));
                break;
            case PowerUpType.DoubleJump:
                if (pc != null && host != null) host.StartCoroutine(DoubleJump(pc));
                break;
            case PowerUpType.ExtraHealth:
                ph?.Heal(1);
                break;
        }
    }

    private System.Collections.IEnumerator SpeedBoost(PlayerController pc)
    {
        pc.SetSpeedBoost(1.8f);
        yield return new WaitForSeconds(duration);
        pc.ResetSpeedBoost();
    }

    private System.Collections.IEnumerator DoubleJump(PlayerController pc)
    {
        pc.SetDoubleJump(true);
        yield return new WaitForSeconds(duration);
        pc.SetDoubleJump(false);
    }
}

public class RotatingObstacle : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float speed = 60f;

    private void Update() => transform.Rotate(rotationAxis * speed * Time.deltaTime);
}

public class PickupMagnet : MonoBehaviour
{
    [SerializeField] private float magnetRadius = 5f;
    [SerializeField] private float magnetForce = 15f;

    private void Update()
    {
        var cols = Physics.OverlapSphere(transform.position, magnetRadius);
        foreach (var col in cols)
        {
            if (col.GetComponent<Collectible>() == null) continue;
            Vector3 dir = (transform.position - col.transform.position).normalized;
            col.transform.position += dir * magnetForce * Time.deltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
