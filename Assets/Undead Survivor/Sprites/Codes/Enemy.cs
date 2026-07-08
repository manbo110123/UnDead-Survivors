using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed;
    public float health;
    public float maxHealth;
    public RuntimeAnimatorController[] animCon;
    //追踪的目标
    public Rigidbody2D target;

    //怪物是否存活
    bool isLive = true;
    bool isKnockedBack = false;  // ★ 新增：击退中暂停追踪

    Rigidbody2D rigid;
    Collider2D coll;
    Animator anim;
    SpriteRenderer spriter;
    WaitForFixedUpdate wait;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
        wait = new WaitForFixedUpdate();
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.isLive)
            return;

        // 死亡 或 击退中 → 不移动
        if (!isLive || isKnockedBack)
            return;

        Vector2 dirVec = target.position - rigid.position;
        Vector2 nextVec = dirVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
        rigid.velocity = Vector2.zero;
    }

    void LateUpdate()
    {
        if (!GameManager.Instance.isLive)
            return;

        if (!isLive || anim.GetCurrentAnimatorStateInfo(0).IsName("Hit"))
            return;

        spriter.flipX = target.position.x < rigid.position.x;
    }

    //怪物激活
    void OnEnable()
    {
        // 加一个空值保护，如果 player 还没准备好，延迟一帧再获取
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            target = GameManager.Instance.player.GetComponent<Rigidbody2D>();
        }
        else
        {
            StartCoroutine(DelayedTargetSetup());
        }

        target = GameManager.Instance.player.GetComponent<Rigidbody2D>();
        isLive = true;
        isKnockedBack = false;  // ★ 新增：激活时重置
        coll.enabled = true;
        rigid.simulated = true;
        spriter.sortingOrder = 2;
        anim.SetBool("Dead", false);
        health = maxHealth;
    }

    IEnumerator DelayedTargetSetup()
    {
        yield return null; // 等待一帧
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            target = GameManager.Instance.player.GetComponent<Rigidbody2D>();
        }
    }

    //初始化怪物属性
    public void Init(SpawnData data)
    {
        anim.runtimeAnimatorController = animCon[data.SpriteType];
        speed = data.Speed;
        maxHealth = data.Health;
        health = data.Health;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Bullet") || !isLive)
            return;

        health -= collision.GetComponent<Bullet>().damage;
        StartCoroutine(KnockBack());

        //活着，就触发被攻击之类的行为
        if (health > 0)
        {
            anim.SetTrigger("Hit");
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Hit);
        }
        else
        {
            isLive = false;
            coll.enabled = false;
            rigid.simulated = false;
            spriter.sortingOrder = 1;
            anim.SetBool("Dead", true);
            GameManager.Instance.kill++;
            GameManager.Instance.GetExp();

            if(GameManager.Instance.isLive)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead);
        }
    }

    IEnumerator KnockBack()
    {
        isKnockedBack = true;  // ★ 开始击退，暂停追踪

        yield return wait;  // 等待1个物理帧

        Vector3 playerPos = GameManager.Instance.player.transform.position;
        Vector3 dirVec = transform.position - playerPos;
        rigid.AddForce(dirVec.normalized * 3, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.15f);  // ★ 等待击退效果完成

        isKnockedBack = false;  // ★ 恢复追踪
    }

    public void Dead()
    {
        gameObject.SetActive(false);
    }
}