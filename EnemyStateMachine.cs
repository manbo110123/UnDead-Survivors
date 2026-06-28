// 纯 C# 类，不挂在 GameObject 上，由 Enemy 持有
// C++ 类比：持有 EnemyState* current 指针，切换时调用 Exit/Enter，实现运行时多态
public class EnemyStateMachine
{
    public EnemyState Current { get; private set; } // 当前状态，外部只读

    // 切换状态：先调旧状态的 Exit，再调新状态的 Enter
    public void Change(EnemyState next)
    {
        Current?.Exit();
        Current = next;
        Current.Enter();
    }

    // Enemy 的 Update/FixedUpdate/LateUpdate 全部委托到这里
    // Enemy 本身不关心"当前在做什么"，全部由状态对象决定
    public void Update()      => Current?.Update();
    public void FixedUpdate() => Current?.FixedUpdate();
    public void LateUpdate()  => Current?.LateUpdate();
}
