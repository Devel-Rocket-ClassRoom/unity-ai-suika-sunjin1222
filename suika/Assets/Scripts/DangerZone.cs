using System.Collections.Generic;
using UnityEngine;

// 컨테이너 위쪽 경계선 위에 과일이 일정 시간 이상 남아있으면 게임 오버
// BoxCollider2D (IsTrigger=true) 를 달고 danger line 위에 배치
public class DangerZone : MonoBehaviour
{
    [SerializeField] private float gameOverDelay = 2f;

    private readonly Dictionary<Fruit, float> fruitsInZone = new();

    void OnTriggerEnter2D(Collider2D other)
    {
        Fruit f = other.GetComponent<Fruit>();
        if (f != null && f.isActive && !fruitsInZone.ContainsKey(f))
            fruitsInZone[f] = Time.time;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Fruit f = other.GetComponent<Fruit>();
        if (f != null) fruitsInZone.Remove(f);
    }

    void Update()
    {
        // 딕셔너리를 복사해 순회 (inside update 중 삭제 방지)
        foreach (var kv in new Dictionary<Fruit, float>(fruitsInZone))
        {
            if (kv.Key == null) { fruitsInZone.Remove(kv.Key); continue; }
            if (!kv.Key.isActive) { fruitsInZone.Remove(kv.Key); continue; }

            if (Time.time - kv.Value >= gameOverDelay)
            {
                GameManager.Instance.TriggerGameOver();
                return;
            }
        }
    }
}
