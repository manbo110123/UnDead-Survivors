using UnityEngine;

public class EnemyHitState : EnemyState
{
    float elapsed;          // 硬直计时器
    bool forceApplied;      // 确保击退力只施加一次
    const float duration = 0.3f; // 硬直持续时间（秒）

    public EnemyHitState(Enemy owner) : base(owner) {}

    public override void Enter()
    {
        elapsed = 0f;
        forceApplied = false;
        // 播放受击动画并触发音效
        owner.anim.SetTrigger("Hit");
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Hit);
    }

    public override void FixedUpdate()
    {
        // 第一个物理帧施加击退力，模拟原来 WaitForFixedUpdate 的效果
        if (!forceApplied)
        {
            Vector3 dir = owner.transform.position - GameManager.Instance.player.transform.position;
            owner.rigid.AddForce(dir.normalized * 3f, ForceMode2D.Impulse);
            forceApplied = true;
        }

        elapsed += Time.fixedDeltaTime;
        // 硬直结束后切回追击状态
        if (elapsed >= duration)
            owner.StateMachine.Change(new EnemyChaseState(owner));
    }

    public override void Exit()
    {
        // 离开硬直状态时清零速度，防止残留惯性影响追击
        owner.rigid.velocity = Vector2.zero;
    }
}
