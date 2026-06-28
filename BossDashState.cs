using UnityEngine;

// Boss 冲刺状态：锁定蓄力结束时玩家位置，高速直线冲刺
// 方向在 Enter 时固定，玩家可以通过移动来闪避
public class BossDashState : EnemyState
{
    float elapsed;          // 冲刺计时器
    Vector2 dashDir;        // 冲刺方向，Enter 时锁定，冲刺过程中不变
    const float duration  = 0.4f;  // 冲刺持续时间（秒）
    const float dashSpeed = 18f;   // 冲刺速度，远高于普通追击速度

    public BossDashState(Enemy owner) : base(owner) {}

    public override void Enter()
    {
        elapsed = 0f;
        owner.spriter.color = Color.red; // 红色表示冲刺中

        // 蓄力结束时锁定玩家位置，方向固定后玩家可以闪避
        if (owner.target != null)
            dashDir = ((Vector2)owner.target.position - owner.rigid.position).normalized;
    }

    public override void FixedUpdate()
    {
        elapsed += Time.fixedDeltaTime;
        // 沿固定方向高速移动
        owner.rigid.MovePosition(owner.rigid.position + dashDir * dashSpeed * Time.fixedDeltaTime);

        // 冲刺结束，切回追击状态
        if (elapsed >= duration)
            owner.StateMachine.Change(new EnemyChaseState(owner));
    }

    public override void LateUpdate()
    {
        // 冲刺时也保持 sprite 朝向正确
        if (owner.target != null)
            owner.spriter.flipX = owner.target.position.x < owner.rigid.position.x;
    }

    public override void Exit()
    {
        // 恢复颜色，清零速度，防止残留惯性
        owner.spriter.color = Color.white;
        owner.rigid.velocity = Vector2.zero;
    }
}
