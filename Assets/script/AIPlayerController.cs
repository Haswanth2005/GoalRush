using UnityEngine;

/// <summary>
/// Brain for a single AI player. The AITeam script decides who is "active chaser".
/// When active: chase ball and shoot toward the player's goal.
/// When idle:   drift to a support position.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AIPlayerController : MonoBehaviour
{
    public enum AIState { Idle, ChaseBall }

    [Header("Movement")]
    public float moveSpeed    = 5.5f;
    public float rotSpeed     = 12f;
    public float acceleration = 26f;
    public float deceleration = 30f;

    [Header("Shooting")]
    public float shootRange   = 2.5f;   // distance from ball to shoot
    public float shootAtGoalRange = 16f; // only shoot when reasonably close to goal
    public float minKickForce = 14f;
    public float maxKickForce = 22f;
    public float kickUpAngle  = 10f;    // loft degrees
    public float shootCooldown = 1.8f;
    public float goalAimRadius = 2.2f;  // random spread around goal center

    [Header("Ball Winning")]
    public float tackleRange = 1.8f;
    public float tackleCooldown = 0.35f;
    public float postTackleBallPush = 4f;

    [Header("Support Position")]
    public Vector3 supportOffset = new Vector3(0f, 0f, 5f);  // relative idle spot

    // ---- set by AITeam ----
    [HideInInspector] public Transform playerGoalTarget;   // position to shoot AT
    [HideInInspector] public AIState state = AIState.Idle;

    // ---- internal ----
    private Rigidbody _rb;
    private float     _shootTimer;
    private float     _tackleTimer;
    private Animator  _animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    // ─────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
        {
            _animator.applyRootMotion = false;
            _animator.updateMode = AnimatorUpdateMode.Fixed;
        }
    }

    // ─────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Playing)
        {
            Stop();
            return;
        }
        if (Ball.Instance == null) { Stop(); return; }

        _shootTimer -= Time.fixedDeltaTime;
        _tackleTimer -= Time.fixedDeltaTime;

        switch (state)
        {
            case AIState.Idle:      UpdateIdle();      break;
            case AIState.ChaseBall: UpdateChaseBall(); break;
        }
    }

    // ─────────────────────────────────────────────
    private void UpdateIdle()
    {
        // Drift toward support position (a fixed offset behind the team)
        Vector3 target = transform.parent != null
            ? transform.parent.position + supportOffset
            : transform.position + supportOffset;

        MoveToward(target, moveSpeed * 0.5f);
    }

    private void UpdateChaseBall()
    {
        Player possessor = FindCurrentBallPossessor();
        if (possessor != null)
        {
            Vector3 possessorPos = possessor.transform.position;
            float possessorDist = HorizontalDistance(transform.position, possessorPos);

            if (possessorDist <= tackleRange && _tackleTimer <= 0f)
            {
                TryDispossess(possessor);
            }
            else
            {
                MoveToward(possessorPos, moveSpeed);
                return;
            }
        }

        Vector3 ballPos = Ball.Instance.transform.position;
        float distToBall = HorizontalDistance(transform.position, ballPos);
        float distBallToGoal = playerGoalTarget == null ? float.MaxValue : HorizontalDistance(ballPos, playerGoalTarget.position);

        if (distToBall <= shootRange && distBallToGoal <= shootAtGoalRange && _shootTimer <= 0f)
        {
            Shoot();
            return;
        }

        MoveToward(ballPos, moveSpeed);
    }

    // ─────────────────────────────────────────────
    private void MoveToward(Vector3 worldTarget, float speed)
    {
        Vector3 dir = worldTarget - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
        {
            Stop();
            return;
        }

        dir.Normalize();

        Vector3 vel = _rb.linearVelocity;
        Vector3 currentHorizontal = new Vector3(vel.x, 0f, vel.z);
        Vector3 targetHorizontal = dir * speed;
        float accel = targetHorizontal.sqrMagnitude > currentHorizontal.sqrMagnitude ? acceleration : deceleration;
        Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetHorizontal, accel * Time.fixedDeltaTime);

        if (newHorizontal.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(newHorizontal.normalized);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, Time.fixedDeltaTime * rotSpeed));
        }

        vel.x = newHorizontal.x;
        vel.z = newHorizontal.z;
        _rb.linearVelocity = vel;
        UpdateAnimatorFromVelocity(newHorizontal);
    }

    private void Stop()
    {
        Vector3 vel = _rb.linearVelocity;
        vel.x = 0f;
        vel.z = 0f;
        _rb.linearVelocity = vel;

        UpdateAnimatorFromVelocity(Vector3.zero);
    }

    // ─────────────────────────────────────────────
    private void Shoot()
    {
        _shootTimer = shootCooldown;

        if (Ball.Instance == null || playerGoalTarget == null) return;

        Vector3 goalPos  = new Vector3(playerGoalTarget.position.x, 0f, playerGoalTarget.position.z);
        Vector3 ballPos  = new Vector3(Ball.Instance.transform.position.x, 0f, Ball.Instance.transform.position.z);

        // Aim to a random point around goal center for natural but threatening shots.
        Vector3 toGoal = (goalPos - ballPos);
        Vector3 side = Vector3.Cross(Vector3.up, toGoal.normalized);
        Vector3 aimPoint = goalPos + side * Random.Range(-goalAimRadius, goalAimRadius);

        Vector3 dir = (aimPoint - ballPos).normalized;
        dir.Normalize();

        float upComp = Mathf.Tan(kickUpAngle * Mathf.Deg2Rad);
        dir = (dir + Vector3.up * upComp).normalized;

        float distToGoal = Mathf.Max(1f, toGoal.magnitude);
        float t = Mathf.InverseLerp(shootAtGoalRange, 1f, distToGoal);
        float force = Mathf.Lerp(minKickForce, maxKickForce, t);

        Ball.Instance.rb.angularVelocity = Vector3.zero;
        Ball.Instance.rb.linearVelocity  = dir * force;
    }

    private Player FindCurrentBallPossessor()
    {
        var players = Object.FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p != null && p.hasPossession) return p;
        }
        return null;
    }

    private void TryDispossess(Player possessor)
    {
        if (possessor == null || Ball.Instance == null) return;

        var possession = possessor.GetComponent<Possession>();
        if (possession != null) possession.ReleaseBall();
        possessor.hasPossession = false;
        _tackleTimer = tackleCooldown;

        // Nudge ball away from dribbler so AI can immediately contest it.
        Vector3 dir = (transform.position - possessor.transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.Normalize();

        Vector3 vel = Ball.Instance.rb.linearVelocity;
        vel.x = dir.x * postTackleBallPush;
        vel.z = dir.z * postTackleBallPush;
        Ball.Instance.rb.linearVelocity = vel;
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void UpdateAnimatorFromVelocity(Vector3 horizontalVel)
    {
        if (_animator == null) return;

        Vector3 localVel = transform.InverseTransformDirection(horizontalVel);
        float speed01 = Mathf.Clamp01(horizontalVel.magnitude / Mathf.Max(0.01f, moveSpeed));

        _animator.SetFloat(SpeedHash, speed01, 0.08f, Time.fixedDeltaTime);
        _animator.SetFloat(MoveXHash, Mathf.Clamp(localVel.x / Mathf.Max(0.01f, moveSpeed), -1f, 1f), 0.08f, Time.fixedDeltaTime);
        _animator.SetFloat(MoveYHash, Mathf.Clamp(localVel.z / Mathf.Max(0.01f, moveSpeed), -1f, 1f), 0.08f, Time.fixedDeltaTime);
    }

    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
}
