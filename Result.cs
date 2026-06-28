using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Result : MonoBehaviour
{
    public GameObject[] titles;

    public void Lose()
    {
        // 显示失败图标，确保胜利图标关闭
        titles[0].SetActive(true);
        titles[1].SetActive(false);
    }

    public void Win()
    {
        // 显示胜利图标，确保失败图标关闭
        titles[0].SetActive(false);
        titles[1].SetActive(true);
    }
}
