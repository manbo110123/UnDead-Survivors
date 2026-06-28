using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed;
    public float health;
    public float maxHealth;
    public RuntimeAnimatorController[] animCon;
    public bool isBoss;           // 是否是 Boss，由 SpawnData 初始化时赋值
    public float knockbackForce;  // 打玩家时的击退力，普通怪小，Boss 大

    // internal：同一程序集内可访问，状态类可以直接用，外部模块不可见
    // 原来是 private，现在改成 internal，让各个 State 类能直接读写组件
    internal Rigidbody2D target;
    internal Rigidbody2D rigid;
    internal Collider2D  coll;
    internal Animator    anim;
    internal SpriteRenderer spriter;

    // 状态机实例，每帧 Update/FixedUpdate/LateUpdate 全部委托给当前状态
    public EnemyStateMachine StateMachine { get; private set; }

    void Awake()
    {
        rigid    = GetComponent<Rigidbody2D>();
        coll     = GetComponent<Collider2D>();
        anim     = GetComponent<Animator>();
        spriter  = GetComponent<SpriteRenderer>();
        StateMachine = new EnemyStateMachine();
    }

    void OnEnable()
    {
        if (GameManager.Instance != null && GameManager.Instance.player != null)
            target = GameManager.Instance.player.GetComponent<Rigidbody2D>();
        else
            StartCoroutine(DelayedTargetSetup());

        coll.enabled = true;
        rigid.simulated = true;
        spriter.sortingOrder = 2;
        spriter.color = Color.white;
        transform.localScale = Vector3.one;  // 重置缩放，防止对象池复用时残留 Boss 大小
        anim.SetBool("Dead", false);
        health = maxHealth;

        // 对象池取出后进入追击状态
        StateMachine.Change(new EnemyChaseState(this));
    }

    void Update()
    {
        if (!GameManager.Instance.isLive) return;
        StateMachine.Update();
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.isLive) return;
        StateMachine.FixedUpdate();
    }

    void LateUpdate()
    {
        if (!GameManager.Instance.isLive) return;
        StateMachine.LateUpdate();
    }

    IEnumerator DelayedTargetSetup()
    {
        // 等一帧再取目标，避免 GameManager 还未初始化时报空引用
        yield return null;
        if (GameManager.Instance != null && GameManager.Instance.player != null)
            target = GameManager.Instance.player.GetComponent<Rigidbody2D>();
    }

    public void Init(SpawnData data)
    {
        anim.runtimeAnimatorController = animCon[data.SpriteType];
        speed     = data.Speed;
        maxHealth = data.Health;
        health    = data.Health;
        isBoss         = data.IsBoss;
        knockbackForce = data.KnockbackForce;

        // Boss 放大两倍，普通敌人保持原始大小
        transform.localScale = isBoss ? Vector3.one * 2f : Vector3.one;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 死亡状态或受击硬直期间忽略子弹（受击硬直 = 小怪无敌帧）
        if (!collision.CompareTag("Bullet") || StateMachine.Current is EnemyDeadState || StateMachine.Current is EnemyHitState)
            return;

        float damage = collision.GetComponent<Bullet>().damage;
        health -= damage;

        // 发布伤害事件，飘字系统通过事件总线订阅并响应
        GameEventSystem.PublishDamageDealt(damage, transform.position);

        if (health > 0)
        {
            if (isBoss)
            {
                // Boss 只播受击动画，不进入硬直状态，不中断冲刺
                // 这体现了状态机的优势：行为可以单独组合，不需要额外布尔标志
                anim.SetTrigger("Hit");
            }
            else
            {
                // 小怪进入硬直状态（受击动画 + 击退 + 0.3s 无敌帧）
                StateMachine.Change(new EnemyHitState(this));
            }
        }
        else
        {
            StateMachine.Change(new EnemyDeadState(this));
        }
    }

    // 死亡动画末尾的 Animation Event 调用此方法，把对象还给对象池
    public void Dead()
    {
        gameObject.SetActive(false);
    }
}
