using UnityEngine;

public class EnemyChaseState : EnemyState
{
    float dashTimer;            // Boss 冲刺循环计时器
    const float dashInterval = 4f; // 每隔 4 秒触发一次蓄力→冲刺

    public EnemyChaseState(Enemy owner) : base(owner) {}

    public override void Enter()
    {
        // 进入追击状态时重置计时，避免刚切回来就立刻再次冲刺
        dashTimer = 0f;
    }

    public override void Update()
    {
        // 只有 Boss 才有冲刺计时，普通小怪跳过
        if (!owner.isBoss) return;

        dashTimer += Time.deltaTime;
        if (dashTimer >= dashInterval)
            owner.StateMachine.Change(new BossChargeState(owner));
    }

    public override void FixedUpdate()
    {
        if (owner.target == null) return;

        // 每物理帧向玩家方向移动，速度由 SpawnData 配置
        Vector2 dir = owner.target.position - owner.rigid.position;
        owner.rigid.MovePosition(owner.rigid.position + dir.normalized * owner.speed * Time.fixedDeltaTime);
        owner.rigid.velocity = Vector2.zero;
    }

    public override void LateUpdate()
    {
        if (owner.target == null) return;
        // 根据玩家相对位置翻转 sprite，保证朝向正确
        owner.spriter.flipX = owner.target.position.x < owner.rigid.position.x;
    }
}
