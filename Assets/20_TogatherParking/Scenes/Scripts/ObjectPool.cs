using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    /// 프리팹과 풀 크기를 지정해 미리 생성(선택적)
    public void Preload(GameObject prefab, int size, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary[prefab] = new Queue<GameObject>();

        var queue = poolDictionary[prefab];
        while (queue.Count < size)
        {
            GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, parent ?? transform);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }
    }

    /// 풀에서 오브젝트를 꺼내 반환. 없으면 새로 생성.
    public GameObject Get(GameObject prefab, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary[prefab] = new Queue<GameObject>();

        var queue = poolDictionary[prefab];
        GameObject obj;
        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, parent ?? transform);
        }
        obj.SetActive(true);
        return obj;
    }

    /// 오브젝트를 풀로 반환
    public void Return(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary[prefab] = new Queue<GameObject>();
        poolDictionary[prefab].Enqueue(obj);
    }
}