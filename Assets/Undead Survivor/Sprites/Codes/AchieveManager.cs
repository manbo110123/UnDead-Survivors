using System.Collections;
using System;
using UnityEngine;

// ============================================================
// AchieveManager 改造说明：
//
// 改造前：LateUpdate 每帧检查 kill >= 10、gameTime == maxGameTime
//   问题：每帧执行一次 switch-case，条件永远不满足时全是无效计算
//
// 改造后：订阅 OnEnemyKilled 和 OnGameVictory，只在相关事件发生时检查
//   好处：完全消除无效轮询，逻辑更清晰
// ============================================================

public class AchieveManager : MonoBehaviour
{
    public GameObject[] lockCharacter;
    public GameObject[] unlockCharacter;
    public GameObject uiNotice;

    enum Achive { UnlockPotato, UnlockBean }
    Achive[] achives;
    WaitForSecondsRealtime wait;

    void Awake()
    {
        achives = (Achive[])Enum.GetValues(typeof(Achive));
        wait = new WaitForSecondsRealtime(5);

        if (!PlayerPrefs.HasKey("MyData"))
            Init();
    }

    void Init()
    {
        PlayerPrefs.SetInt("MyData", 1);
        foreach (Achive achive in achives)
            PlayerPrefs.SetInt(achive.ToString(), 0);
    }

    void Start()
    {
        UnlockCharacter();
    }

    // ── 订阅：只关心击杀数和胜利事件 ────────────────────────
    void OnEnable()
    {
        GameEventSystem.OnEnemyKilled += OnEnemyKilled;
        GameEventSystem.OnGameVictory += OnGameVictory;
    }

    void OnDisable()
    {
        GameEventSystem.OnEnemyKilled -= OnEnemyKilled;
        GameEventSystem.OnGameVictory -= OnGameVictory;
    }

    void UnlockCharacter()
    {
        for (int i = 0; i < lockCharacter.Length; i++)
        {
            bool isUnlock = PlayerPrefs.GetInt(achives[i].ToString()) == 1;
            lockCharacter[i].SetActive(!isUnlock);
            unlockCharacter[i].SetActive(isUnlock);
        }
    }

    // 击杀数变化时检查土豆角色解锁
    void OnEnemyKilled(int totalKills)
    {
        if (totalKills >= 10)
            TryUnlock(Achive.UnlockPotato);
    }

    // 胜利时解锁豆子角色
    void OnGameVictory()
    {
        TryUnlock(Achive.UnlockBean);
    }

    void TryUnlock(Achive achive)
    {
        if (PlayerPrefs.GetInt(achive.ToString()) == 1)
            return; // 已解锁，不重复触发

        PlayerPrefs.SetInt(achive.ToString(), 1);

        int index = (int)achive;
        for (int i = 0; i < uiNotice.transform.childCount; i++)
            uiNotice.transform.GetChild(i).gameObject.SetActive(i == index);

        StartCoroutine(NoticeRoutine());
    }

    IEnumerator NoticeRoutine()
    {
        uiNotice.SetActive(true);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
        yield return wait;
        uiNotice.SetActive(false);
    }
}
