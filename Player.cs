using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed;
    public Scanner scanner;
    public Hand[] hands;
    public RuntimeAnimatorController[] animCon;

    Rigidbody2D rigid;
    SpriteRenderer spriter;
    Animator anim;

    bool isHitFlashing;                        // 防止闪白协程重叠
    bool isKnockedBack;                        // 击退期间暂停 MovePosition，防止两者冲突
    float damageCooldown;                      // 受伤冷却，防止碰撞帧率差异导致伤害不稳定
    const float damageCooldownTime = 0.5f;    // 每 0.5 秒最多受一次伤
    static readonly Color baseColor = Color.white;

    void Awake()
    {
        rigid   = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim    = GetComponent<Animator>();
        scanner = GetComponent<Scanner>();
        hands   = GetComponentsInChildren<Hand>(true);
    }

    void Update()
    {
        // 受伤冷却倒计时
        if (damageCooldown > 0)
            damageCooldown -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.isLive)
            return;

        // 击退期间暂停玩家输入移动，让击退协程完全控制位置
        if (isKnockedBack)
            return;

        Vector2 nextVec = inputVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
    }

    void OnEnable()
    {
        speed *= Character.Speed;
        anim.runtimeAnimatorController = animCon[GameManager.Instance.playerId];
    }

    void OnMove(InputValue value)
    {
        if (!GameManager.Instance.isLive)
            return;
        inputVec = value.Get<Vector2>();
    }

    void LateUpdate()
    {
        if (!GameManager.Instance.isLive)
            return;

        anim.SetFloat("Speed", inputVec.magnitude);

        if (inputVec.x != 0)
            spriter.flipX = inputVec.x < 0;
    }

    // 碰撞开始时同时处理扣血和击退
    // 原来用 OnCollisionStay2D 持续扣血，但击退把玩家推离后 Stay 就停止触发
    // 改为 Enter + 冷却时间，扣血和击退不再相互干扰
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!GameManager.Instance.isLive)
            return;

        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy == null) return;

        // 扣血：冷却时间内不重复扣
        if (damageCooldown <= 0)
        {
            damageCooldown = damageCooldownTime;

            // 改造前：直接修改 GameManager.Instance.health（外部直接写数据，破坏封装）
            // 改造后：调用 TakeDamage()，由 GameManager 控制血量并发布事件
            // 类比 C++：通过 setter 修改私有成员，而不是直接访问公有变量
            GameManager.Instance.TakeDamage(10f);

            // 死亡判断现在在 TakeDamage 内部处理，这里只负责播放死亡动画
            if (GameManager.Instance.health <= 0)
            {
                for (int index = 2; index < transform.childCount; index++)
                    transform.GetChild(index).gameObject.SetActive(false);
                anim.SetTrigger("Dead");
            }
        }

        // 击退和闪红：每次碰撞都触发，不受扣血冷却影响
        TakeHitFeedback(enemy);
    }

    // 受击视觉反馈：闪红 + 击退位移
    void TakeHitFeedback(Enemy enemy)
    {
        // 击退方向：从怪物指向玩家
        Vector2 knockDir = ((Vector2)transform.position - enemy.rigid.position).normalized;

        if (!isKnockedBack)
            StartCoroutine(KnockbackRoutine(knockDir, enemy.knockbackForce));

        if (!isHitFlashing)
            StartCoroutine(HitFlash());
    }

    // 击退协程：直接用 MovePosition 推动玩家，持续 0.15 秒
    // 不依赖 AddForce，避免与 FixedUpdate 的 MovePosition 冲突
    IEnumerator KnockbackRoutine(Vector2 dir, float force)
    {
        isKnockedBack = true;
        float duration = 0.15f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            // 随时间衰减：刚被打时位移最大，之后快速减弱，模拟惯性衰减
            float t = 1f - (elapsed / duration);
            rigid.MovePosition(rigid.position + dir * force * t * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isKnockedBack = false;
    }

    // 闪红动画：变红 → 恢复，持续 0.15 秒
    IEnumerator HitFlash()
    {
        isHitFlashing = true;
        spriter.color = new Color(1f, 0.3f, 0.3f, 1f); // 偏红，视觉冲击比纯白更明显
        yield return new WaitForSeconds(0.15f);
        spriter.color = baseColor;
        isHitFlashing = false;
    }
}
