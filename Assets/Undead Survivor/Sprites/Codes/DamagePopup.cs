using System.Collections;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    TextMesh textMesh;

    void Awake()
    {
        textMesh = GetComponent<TextMesh>();
    }

    public void Play(float damage, Vector3 worldPos)
    {
        transform.position = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0f);
        textMesh.text  = Mathf.RoundToInt(damage).ToString();
        textMesh.color = Color.white;
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        float duration = 0.8f;
        float elapsed  = 0f;
        Vector3 origin = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = origin + new Vector3(0f, t * 1.2f, 0f);
            textMesh.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
