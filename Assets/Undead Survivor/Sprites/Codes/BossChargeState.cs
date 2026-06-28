using UnityEngine;

// Boss 蓄力状态：停止移动，黄色闪烁警告玩家即将冲刺
public class BossChargeState : EnemyState
{
    float elapsed;          // 蓄力计时器
    float flashTimer;       // 闪烁计时器
    const float duration = 0.8f;       // 蓄力持续时间（秒）
    const float flashInterval = 0.1f;  // 每 0.1 秒切换一次颜色
    bool flashOn;

    public BossChargeState(Enemy owner) : base(owner) {}

    public override void Enter()
    {
        elapsed = 0f;
        flashTimer = 0f;
        flashOn = false;
        // 停止移动，蓄力期间站定
        owner.rigid.velocity = Vector2.zero;
    }

    public override void Update()
    {
        elapsed += Time.deltaTime;
        flashTimer += Time.deltaTime;

        // 黄白交替闪烁，给玩家视觉警告
        if (flashTimer >= flashInterval)
        {
            flashTimer = 0f;
            flashOn = !flashOn;
            owner.spriter.color = flashOn ? Color.yellow : Color.white;
        }

        // 蓄力结束，切换到冲刺状态
        if (elapsed >= duration)
            owner.StateMachine.Change(new BossDashState(owner));
    }

    public override void Exit()
    {
        // 离开蓄力状态时恢复白色，防止颜色残留
        owner.spriter.color = Color.white;
    }
}
