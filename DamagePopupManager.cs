using System.Collections.Generic;
using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public GameObject popupPrefab;

    readonly List<DamagePopup> pool = new List<DamagePopup>();  //对象池

    void OnEnable()
    {
        GameEventSystem.OnDamageDealt += HandleDamageDealt;
    }

    void OnDisable()
    {
        GameEventSystem.OnDamageDealt -= HandleDamageDealt;
    }

    void HandleDamageDealt(float damage, Vector3 worldPos)
    {
        DamagePopup popup = GetFromPool();
        popup.gameObject.SetActive(true);
        popup.Play(damage, worldPos);
    }

    DamagePopup GetFromPool()
    {
        foreach (DamagePopup p in pool)
        {
            if (!p.gameObject.activeSelf)
                return p;
        }

        GameObject obj = Instantiate(popupPrefab, transform);
        DamagePopup newPopup = obj.GetComponent<DamagePopup>();
        pool.Add(newPopup);
        return newPopup;
    }
}
