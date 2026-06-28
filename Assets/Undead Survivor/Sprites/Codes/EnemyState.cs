// C++ 类比：纯虚基类，每个方法相当于 virtual void xxx() {}
// 状态不继承 MonoBehaviour，它们是纯 C# 对象，由 Enemy 驱动，不挂在 GameObject 上
public abstract class EnemyState
{
    protected Enemy owner; // 持有宿主引用，状态内可以直接操作敌人的组件和数据

    protected EnemyState(Enemy owner) { this.owner = owner; }

    public virtual void Enter()       {}   // 进入状态时调用一次（播动画、初始化计时器等）
    public virtual void Update()      {}   // 每帧逻辑（计时、状态切换判断）
    public virtual void FixedUpdate() {}   // 物理帧逻辑（移动、施力）
    public virtual void LateUpdate()  {}   // 渲染后逻辑（sprite flip 朝向）
    public virtual void Exit()        {}   // 离开状态时调用一次（清理速度、重置颜色等）
}
