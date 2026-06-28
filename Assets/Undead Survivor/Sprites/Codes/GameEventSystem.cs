using System;
using UnityEngine;
// ============================================================
// 事件总线（Event Bus）
//
// 设计思路：
//   各模块之间不再直接引用彼此，而是通过这个静态类通信。
//   发布者只管"发出事件"，订阅者只管"响应事件"，双方互不知情。
//
// C++ 类比：
//   类似 Qt 的 signal/slot，或者你手写的观察者模式（Observer Pattern）。
//   C# 的 event Action 就是类型安全的多播函数指针（multicast delegate）。
//   Action<float, float> ≈ std::function<void(float, float)>
//
// 为什么用 static 类：
//   事件总线是全局服务，不需要挂在任何 GameObject 上，
//   用静态类避免了单例的初始化顺序问题，也没有 Unity 生命周期依赖。
// ============================================================

public static class GameEventSystem
{
    // ── 游戏状态事件 ──────────────────────────────────────────
    public static event Action OnGameStarted;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action OnGameOver;
    public static event Action OnGameVictory;

    // ── 玩家事件 ──────────────────────────────────────────────
    // Action<float, float>：携带两个 float 参数的无返回值事件
    public static event Action<float, float> OnHealthChanged;   // (current, max)

    // ── 战斗事件 ──────────────────────────────────────────────
    public static event Action<float, Vector3> OnDamageDealt;   // (damage, worldPos)

    // ── 成长事件 ──────────────────────────────────────────────
    public static event Action<int, int> OnExpChanged;          // (current, required)
    public static event Action<int>      OnLevelUp;             // (newLevel)
    public static event Action<int>      OnEnemyKilled;         // (totalKills)
    public static event Action<float>    OnTimeChanged;         // (remainingTime)

    // ── 发布方法（Publish） ───────────────────────────────────
    //
    // 为什么要封装成方法而不直接用 event？
    // C# 的 event 关键字限制了只有声明类才能 Invoke，但 static 类没有"实例"，
    // 所以我们用 Publish 方法封装，外部调用 Publish 而不是直接 Invoke。
    //
    // ?.Invoke() 是空值安全调用：如果没有任何订阅者，就什么都不做，不会报空指针。
    // C++ 里你得自己判断 if (callback != nullptr) callback()，这里语法糖帮你做了。

    public static void PublishGameStarted()                           => OnGameStarted?.Invoke();
    public static void PublishGamePaused()                            => OnGamePaused?.Invoke();
    public static void PublishGameResumed()                           => OnGameResumed?.Invoke();
    public static void PublishGameOver()                              => OnGameOver?.Invoke();
    public static void PublishGameVictory()                           => OnGameVictory?.Invoke();
    public static void PublishHealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);
    public static void PublishDamageDealt(float damage, Vector3 worldPos) => OnDamageDealt?.Invoke(damage, worldPos);
    public static void PublishExpChanged(int current, int required)   => OnExpChanged?.Invoke(current, required);
    public static void PublishLevelUp(int newLevel)                   => OnLevelUp?.Invoke(newLevel);
    public static void PublishEnemyKilled(int totalKills)             => OnEnemyKilled?.Invoke(totalKills);
    public static void PublishTimeChanged(float remaining)            => OnTimeChanged?.Invoke(remaining);

    // ── 清空所有订阅 ──────────────────────────────────────────
    //
    // 【重要】静态事件的内存泄漏陷阱：
    //   static event 的生命周期是整个程序运行期间，不随场景销毁。
    //   如果场景重载（SceneManager.LoadScene），旧场景的 MonoBehaviour 被销毁，
    //   但 static event 仍持有这些已销毁对象的引用 → 内存泄漏 + 空引用崩溃。
    //
    // 解决方案有两种，这里用最简单的：
    //   场景重载前调用 ClearAll()，或者在每个订阅者的 OnDestroy 中取消订阅。
    //   我们两种都做：GameManager 在 GameRetry 前调 ClearAll，每个订阅者在 OnDestroy 取消。

    public static void ClearAll()
    {
        OnGameStarted  = null;
        OnGamePaused   = null;
        OnGameResumed  = null;
        OnGameOver     = null;
        OnGameVictory  = null;
        OnDamageDealt  = null;
        OnHealthChanged = null;
        OnExpChanged   = null;
        OnLevelUp      = null;
        OnEnemyKilled  = null;
        OnTimeChanged  = null;
    }
}
