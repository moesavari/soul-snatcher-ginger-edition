using System.Collections.Generic;
using UnityEngine;

public static class ObjectPool
{
    private static readonly Dictionary<GameObject, Stack<GameObject>> _pool = new();

    public static GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;
        if (_pool.TryGetValue(prefab, out var stack) && stack.Count > 0)
        {
            var go = stack.Pop();
            var t = go.transform;
            t.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            return go;
        }
        var inst = Object.Instantiate(prefab, pos, rot);
        var tag = inst.GetComponent<PoolTag>();
        if (tag == null) tag = inst.AddComponent<PoolTag>();
        tag.prefab = prefab;
        return inst;
    }

    public static void Release(GameObject instance)
    {
        if (instance == null) return;
        var tag = instance.GetComponent<PoolTag>();
        if (tag == null || tag.prefab == null) { Object.Destroy(instance); return; }

        instance.SetActive(false);
        if (!_pool.TryGetValue(tag.prefab, out var stack))
        {
            stack = new Stack<GameObject>(16);
            _pool[tag.prefab] = stack;
        }
        stack.Push(instance);
    }

    private sealed class PoolTag : MonoBehaviour { public GameObject prefab; }
}
