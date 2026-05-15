using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    enum State { Idle, Chase, Attack, Dead }

    [Header("References")]
    [Tooltip("사격 Raycast의 시작점 (무기 총구의 빈 GameObject)")]
    [SerializeField] Transform muzzle;

    [Header("Detection")]
    [Tooltip("이 거리 내에 플레이어가 들어오면 추적 시작")]
    [SerializeField] float detectRange = 20f;
    [Tooltip("이 거리 안으로 들어오면 정지하고 사격")]
    [SerializeField] float attackRange = 10f;
    [Tooltip("이 거리 밖으로 멀어지면 추적 포기 (detectRange보다 커야 함)")]
    [SerializeField] float loseTargetRange = 25f;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float turnSpeed = 8f;

    [Header("Combat")]
    [Tooltip("1회 사격 간격 (초)")]
    [SerializeField] float fireRate = 0.4f;
    [SerializeField] float damage = 10f;
    [SerializeField] float maxHp = 100f;
    [Tooltip("사격 Raycast가 충돌 가능한 레이어")]
    [SerializeField] LayerMask shootLayerMask = ~0;
    [Tooltip("플레이어 몸통 중앙을 조준하기 위한 높이 보정")]
    [SerializeField] float aimHeightOffset = 1.0f;

    // --- internal ---
    NavMeshAgent agent;
    Animator anim;
    Transform target;
    State state = State.Idle;
    float hp;
    float lastFireTime;

    static readonly int HashSpeed = Animator.StringToHash("Speed");
    static readonly int HashIsShooting = Animator.StringToHash("IsShooting");
    static readonly int HashDie = Animator.StringToHash("Die");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        agent.speed = moveSpeed;
        agent.updateRotation = false; // 회전은 직접 제어 (조준 정확도)
        hp = maxHp;
    }

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) target = playerGo.transform;
        else Debug.LogWarning("[EnemyAI] Player 태그를 가진 오브젝트를 찾지 못했습니다.");
    }

    void Update()
    {
        if (state == State.Dead || target == null) return;

        float dist = Vector3.Distance(transform.position, target.position);

        switch (state)
        {
            case State.Idle:
                anim.SetFloat(HashSpeed, 0f);
                anim.SetBool(HashIsShooting, false);
                if (dist < detectRange) state = State.Chase;
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.SetDestination(target.position);
                anim.SetFloat(HashSpeed, agent.velocity.magnitude);
                anim.SetBool(HashIsShooting, false);
                FaceDirection(agent.velocity);

                if (dist < attackRange) state = State.Attack;
                else if (dist > loseTargetRange) state = State.Idle;
                break;

            case State.Attack:
                agent.isStopped = true;
                anim.SetFloat(HashSpeed, 0f);
                anim.SetBool(HashIsShooting, true);
                FaceDirection(target.position - transform.position);

                if (Time.time >= lastFireTime + fireRate)
                {
                    Fire();
                    lastFireTime = Time.time;
                }

                if (dist > attackRange) state = State.Chase;
                break;
        }
    }

    void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    void Fire()
    {
        if (muzzle == null || target == null) return;

        Vector3 aimPoint = target.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (aimPoint - muzzle.position).normalized;

        if (Physics.Raycast(muzzle.position, dir, out RaycastHit hit, 100f, shootLayerMask))
        {
            Debug.DrawLine(muzzle.position, hit.point, Color.red, 0.1f);
            DealDamage(hit);
        }
    }

    void DealDamage(RaycastHit hit)
    {
        // 1순위: NeoFPS IDamageHandler 시도 (타입이 있으면 사용)
        var handlerComponent = hit.collider.GetComponentInParent<MonoBehaviour>();
        if (handlerComponent != null)
        {
            var idh = hit.collider.GetComponentInParent(
                System.Type.GetType("NeoFPS.IDamageHandler, NeoFPS")
                ?? typeof(MonoBehaviour));
            if (idh != null && idh.GetType().GetMethod("AddDamage",
                new[] { typeof(float) }) != null)
            {
                idh.GetType().GetMethod("AddDamage", new[] { typeof(float) })
                   .Invoke(idh, new object[] { damage });
                return;
            }
        }

        // 2순위: 일반 TakeDamage(float) 메서드를 가진 스크립트 호출
        hit.collider.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    // 외부에서 적을 피격시킬 때 호출
    public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;
        hp -= amount;
        if (hp <= 0f) Die();
    }

    void Die()
    {
        state = State.Dead;
        anim.SetTrigger(HashDie);
        anim.SetBool(HashIsShooting, false);
        anim.SetFloat(HashSpeed, 0f);
        if (agent != null) { agent.isStopped = true; agent.enabled = false; }
    }

    // Scene 뷰에서 범위 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}