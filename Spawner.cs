using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Transform[] spawnPoint;
    public SpawnData[] spawnData;
    public SpawnData bossData;       // Boss 独立配置，在 Inspector 里单独设置
    public float bossInterval = 30f; // 每隔多少秒生成一个 Boss，独立于普通刷怪逻辑

    // levelTime：每个关卡阶段持续时间，由 maxGameTime / spawnData 条数自动计算
    // 例如 maxGameTime=200，两条 spawnData → levelTime=100，第 100 秒骷髅开始刷新
    public float levelTime;

    int level;      // 当前关卡阶段索引，对应 spawnData 数组下标
    float timer;    // 普通怪刷新计时器
    float bossTimer;// Boss 刷新计时器

    void Awake()
    {
        spawnPoint = GetComponentsInChildren<Transform>();
        // 根据总时长和关卡数量自动计算每阶段时长
        levelTime = GameManager.Instance.maxGameTime / spawnData.Length;
    }

    void Update()
    {
        if (!GameManager.Instance.isLive)
            return;

        // ── 普通怪刷新逻辑 ────────────────────────────────────
        timer += Time.deltaTime;
        // 根据游戏时间决定当前阶段，超出最大值时钳制到最后一级
        level = Mathf.Min(Mathf.FloorToInt(GameManager.Instance.gameTime / levelTime), spawnData.Length - 1);

        if (timer > spawnData[level].SpawnTime)
        {
            timer = 0;
            Spawn(spawnData[level]);
        }

        // ── Boss 独立计时，与关卡阶段无关 ─────────────────────
        bossTimer += Time.deltaTime;
        if (bossTimer >= bossInterval)
        {
            bossTimer = 0;
            Spawn(bossData);
        }
    }

    // 通用生成方法，普通怪和 Boss 都走这里
    void Spawn(SpawnData data)
    {
        GameObject enemy = GameManager.Instance.pool.Get(0);
        enemy.transform.position = spawnPoint[Random.Range(1, spawnPoint.Length)].position;
        enemy.GetComponent<Enemy>().Init(data);
    }
}

//序列化
[System.Serializable]
public class SpawnData
{
    public int SpriteType;
    public float SpawnTime;
    public int Health;
    public float Speed;
    public bool IsBoss;
    public float KnockbackForce; // 怪物打玩家时的击退力，Boss 填大一点（如 8），普通怪填小一点（如 3）
}
