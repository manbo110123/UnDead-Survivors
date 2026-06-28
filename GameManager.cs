using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
// GameManager 改造说明：
//
// 改动前：GameManager 直接操控 HUD、Result、LevelUp 等所有模块
// 改动后：GameManager 只管理"数据"和"状态"，通过事件通知其他模块
//
// 删掉的东西：
//   - public HUD hud → HUD 自己订阅事件，GameManager 不再需要持有它
//
// 新增的东西：
//   - TakeDamage()：统一的受伤入口，保证每次血量变化都发布事件
//   - AddKill()：统一的击杀入口，保证击杀数和经验同步更新并发布事件
//
// 核心思路：
//   GameManager 是数据模型（Model），事件是通知机制。
//   任何模块想知道"血量变了"，订阅 OnHealthChanged 即可，不用每帧来查。
// ============================================================

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("# Game Control")]
    public bool isLive;
    public float gameTime;
    public float maxGameTime = 2 * 10f;
    bool isGameEnding; // 游戏结束流程已启动的标志，防止 GameOver 和 GameVictory 同时触发

    [Header("# Player Info")]
    public int playerId;
    public float health;
    public float maxHealth = 100;
    public int level;
    public int kill;
    public int exp;
    public int[] nextExp = { 3, 5, 10, 100, 150, 210, 280, 360, 450, 600 };

    [Header("# Game Object")]
    public PoolManager pool;
    public Player player;
    public LevelUp uiLevelUp;
    public Result uiResult;
    public GameObject enemyCleaner;
    // 注意：原来的 public HUD hud 已删除，HUD 通过事件自我更新

    void Awake()
    {
        Instance = this;
    }

    public void GameStart(int id)
    {
        playerId = id;
        health = maxHealth;

        player.gameObject.SetActive(true);
        uiLevelUp.Select(playerId % 2);
        Resume();

        AudioManager.instance.PlayBgm(true);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);

        // 游戏启动后广播所有初始状态，HUD 订阅后会立刻用这些值刷新显示
        // 这是"初始化广播"模式：事件系统只处理变化，所以启动时要主动推一次初始值
        GameEventSystem.PublishGameStarted();
        GameEventSystem.PublishHealthChanged(health, maxHealth);
        GameEventSystem.PublishExpChanged(exp, nextExp[0]);
        GameEventSystem.PublishLevelUp(level);      // 初始 level = 0，HUD 显示 Lv.0
        GameEventSystem.PublishEnemyKilled(kill);   // 初始 kill = 0，HUD 显示 0
    }

    public void GameOver()
    {
        // 已经在结束流程中则跳过，防止和 GameVictory 同时触发
        if (isGameEnding) return;
        isGameEnding = true;
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        isLive = false;

        // 发布游戏结束事件：AchieveManager、UI 等订阅者会自动响应
        GameEventSystem.PublishGameOver();

        yield return new WaitForSeconds(0.5f);

        uiResult.gameObject.SetActive(true);
        uiResult.Lose();
        Stop();

        AudioManager.instance.PlayBgm(false);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose);
    }

    public void GameVictroy()
    {
        // 已经在结束流程中则跳过，防止和 GameOver 同时触发
        if (isGameEnding) return;
        isGameEnding = true;
        StartCoroutine(GameVictroyRoutine());
    }

    IEnumerator GameVictroyRoutine()
    {
        isLive = false;
        enemyCleaner.SetActive(true);

        // 发布胜利事件
        GameEventSystem.PublishGameVictory();

        yield return new WaitForSeconds(0.5f);

        uiResult.gameObject.SetActive(true);
        uiResult.Win();
        Stop();

        AudioManager.instance.PlayBgm(false);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Win);
    }

    public void GameRetry()
    {
        // 重置结束标志，场景重载前清空所有事件订阅，防止内存泄漏
        // 类比 C++：析构时断开所有信号连接
        isGameEnding = false;
        GameEventSystem.ClearAll();
        SceneManager.LoadScene(0);
    }

    void Update()
    {
        if (!isLive)
            return;

        gameTime += Time.deltaTime;

        // 每帧发布剩余时间，HUD 订阅后自动刷新，不再需要 HUD 自己来查
        float remaining = maxGameTime - gameTime;
        GameEventSystem.PublishTimeChanged(remaining);

        if (gameTime > maxGameTime)
        {
            gameTime = maxGameTime;
            GameVictroy();
        }
    }

    // ── 新增：统一的受伤入口 ──────────────────────────────────
    // 改造前：Player 直接写 GameManager.Instance.health -= xxx
    // 改造后：Player 调 TakeDamage()，由 GameManager 控制数据并发布事件
    //
    // 好处：血量的变化逻辑集中在一处，以后加护盾、无敌帧等，只改这一个方法
    public void TakeDamage(float damage)
    {
        if (!isLive) return;

        health -= damage;
        health = Mathf.Clamp(health, 0, maxHealth);

        // 血量变化 → 立刻发布事件，HUD 血量条自动更新
        GameEventSystem.PublishHealthChanged(health, maxHealth);

        if (health <= 0)
            GameOver();
    }

    // ── 新增：统一的击杀入口 ──────────────────────────────────
    // 改造前：Enemy 直接写 GameManager.Instance.kill++ 和 GetExp()
    // 改造后：Enemy 调 AddKill()，GameManager 统一处理并发布事件
    public void AddKill()
    {
        if (!isLive) return;

        kill++;
        // 发布击杀事件：AchieveManager 通过订阅来检查成就，不再每帧轮询
        GameEventSystem.PublishEnemyKilled(kill);

        GetExp();
    }

    public void GetExp()
    {
        if (!isLive) return;

        exp++;
        int required = nextExp[Mathf.Min(level, nextExp.Length - 1)];

        // 经验变化 → 发布事件，HUD 经验条自动更新
        GameEventSystem.PublishExpChanged(exp, required);

        if (exp == required)
        {
            level++;
            exp = 0;
            // 升级 → 发布升级事件
            GameEventSystem.PublishLevelUp(level);
            uiLevelUp.Show();
        }
    }

    public void Stop()
    {
        isLive = false;
        Time.timeScale = 0;
        GameEventSystem.PublishGamePaused();
    }

    public void Resume()
    {
        isLive = true;
        Time.timeScale = 1;
        GameEventSystem.PublishGameResumed();
    }
}
