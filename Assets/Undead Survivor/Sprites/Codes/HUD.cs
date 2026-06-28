using UnityEngine;
using UnityEngine.UI;

// ============================================================
// HUD 改造说明：
//
// 改造前：每帧 LateUpdate 主动查询 GameManager（轮询模式）
//   问题：即使数据没变，每帧都在做无效的 switch-case 和 string.Format
//
// 改造后：订阅事件，只在数据实际变化时才更新 UI（推送模式）
//   好处：UI 更新次数从"每帧"降低到"数据变化时"
//
// C++ 类比：
//   改造前 = 每帧调一次 getHealth() 轮询
//   改造后 = 注册回调，血量变化时引擎自动调你的 OnHealthChanged()
//
// 订阅生命周期：
//   OnEnable  → 订阅（开始监听）
//   OnDisable → 取消订阅（停止监听，防止对象被禁用后还收到回调）
//
//   为什么不在 Start/Awake 订阅？
//   因为 GameObject 可能被反复 Enable/Disable，
//   在 OnEnable/OnDisable 配对可以确保启用时监听、禁用时停止。
// ============================================================

public class HUD : MonoBehaviour
{
    public enum InfoType { Exp, Level, Kill, Time, Health }
    public InfoType type;

    Text myText;
    Slider mySlider;

    void Awake()
    {
        myText   = GetComponent<Text>();
        mySlider = GetComponent<Slider>();
    }

    // ── 订阅事件 ─────────────────────────────────────────────
    // += 操作符：向事件注册一个监听方法
    // 类比 C++：signal.connect(this, &HUD::OnHealthChanged)
    void OnEnable()
    {
        switch (type)
        {
            case InfoType.Exp:
                GameEventSystem.OnExpChanged    += OnExpChanged;
                break;
            case InfoType.Level:
                GameEventSystem.OnLevelUp       += OnLevelChanged;
                break;
            case InfoType.Kill:
                GameEventSystem.OnEnemyKilled   += OnKillChanged;
                break;
            case InfoType.Time:
                GameEventSystem.OnTimeChanged   += OnTimeChanged;
                break;
            case InfoType.Health:
                GameEventSystem.OnHealthChanged += OnHealthChanged;
                break;
        }
    }

    // ── 取消订阅 ─────────────────────────────────────────────
    // -= 操作符：从事件移除监听方法
    // 【必须】：不取消订阅 → 对象销毁后 static event 仍持有引用 → 内存泄漏
    // 类比 C++：析构函数里 signal.disconnect(this)
    void OnDisable()
    {
        switch (type)
        {
            case InfoType.Exp:
                GameEventSystem.OnExpChanged    -= OnExpChanged;
                break;
            case InfoType.Level:
                GameEventSystem.OnLevelUp       -= OnLevelChanged;
                break;
            case InfoType.Kill:
                GameEventSystem.OnEnemyKilled   -= OnKillChanged;
                break;
            case InfoType.Time:
                GameEventSystem.OnTimeChanged   -= OnTimeChanged;
                break;
            case InfoType.Health:
                GameEventSystem.OnHealthChanged -= OnHealthChanged;
                break;
        }
    }

    // ── 事件响应方法 ─────────────────────────────────────────
    // 这些方法只在事件发布时调用，不是每帧执行的

    void OnExpChanged(int current, int required)
    {
        mySlider.value = (float)current / required;
    }

    void OnLevelChanged(int newLevel)
    {
        myText.text = string.Format("Lv.{0}", newLevel);
    }

    void OnKillChanged(int totalKills)
    {
        myText.text = totalKills.ToString();
    }

    void OnTimeChanged(float remaining)
    {
        int min = Mathf.FloorToInt(remaining / 60);
        int sec = Mathf.FloorToInt(remaining % 60);
        myText.text = string.Format("{0:D2}:{1:D2}", min, sec);
    }

    void OnHealthChanged(float current, float max)
    {
        mySlider.value = current / max;
    }
}
