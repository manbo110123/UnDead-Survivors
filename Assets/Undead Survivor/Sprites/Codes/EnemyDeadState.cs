using UnityEngine;

public class EnemyDeadState : EnemyState
{
    public EnemyDeadState(Enemy owner) : base(owner) {}

    public override void Enter()
    {
        // 禁用碰撞体和物理，防止死亡过程中继续触发碰撞
        owner.coll.enabled = false;
        owner.rigid.simulated = false;
        // 降低排序层，尸体显示在存活敌人下方
        owner.spriter.sortingOrder = 1;
        owner.anim.SetBool("Dead", true);

        if (GameManager.Instance.isLive)
        {
            // 改造后：调用 AddKill()，GameManager 统一处理击杀数据并发布事件
            // 好处：以后加"连击系统"、"掉落物品"，只需在 AddKill 里扩展
            GameManager.Instance.AddKill();
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead);
        }
    }
    // 死亡状态没有 Update/FixedUpdate，等 Animator 事件调用 Enemy.Dead() 回收对象
}
