using UnityEngine;

public class SpawnManager : MonoSingleton<SpawnManager>
{
    [Header("Depth Settings")]
    [SerializeField] private float _targetZ = -0.01f;

    [Header("Filters")]
    [SerializeField] private bool _applyToAll = true;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private string[] _tagFilters;

    [Header("Scene Normalization")]
    [SerializeField] private bool _normalizeExistingOnStart = true;
    [SerializeField] private bool _logAdjustments = false;

    public float targetZ => _targetZ;

    protected override void Awake()
    {
        base.Awake();
        if (_normalizeExistingOnStart) NormalizeSceneRoots();
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null)
        {
            DebugManager.LogWarning("Tried to spawn a null prefab.", this);
            return null;
        }

        var go = parent == null
            ? Instantiate(prefab, position, rotation)
            : Instantiate(prefab, position, rotation, parent);


        NormalizeTransform(go.transform);
        return go;
    }

    private void NormalizeTransform(Transform t)
    {
        if (t == null) return;

        if (ShouldAdjust(t.gameObject))
        {
            Vector3 p = t.position;
            if (!Mathf.Approximately(p.z, _targetZ))
            {
                t.position = new Vector3(p.x, p.y, _targetZ);
                if(_logAdjustments) DebugManager.Log($"Adjusted Z for '{t.name}' to {_targetZ}.", this);
            }
        }

        for (int i = 0; i < t.childCount; i++)
        {
            Transform c = t.GetChild(i);
            if (ShouldAdjust(c.gameObject))
            {
                Vector3 cp = c.position;
                if (!Mathf.Approximately(cp.z, _targetZ))
                {
                    c.position = new Vector3(cp.x, cp.y, _targetZ);
                    if (_logAdjustments) DebugManager.Log($"Adjusted Z for '{c.name}' to {_targetZ}.", this);
                }
            }
        }
    }

    private void NormalizeSceneRoots()
    {
        var roots = gameObject.scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if(root == gameObject) continue;

            if (_applyToAll || ShouldAdjust(root))
                NormalizeTransform(root.transform);
        }
    }

    private bool ShouldAdjust(GameObject go)
    {
        if (_applyToAll) return true;

        bool tagOk = _tagFilters != null && _tagFilters.Length > 0
            ? MatchesAnyTag(go)
            : true;

        bool layerOk = _layerMask.value == 0
            ? true
            : ((_layerMask.value & (1 << go.layer)) != 0);

        return tagOk && layerOk;
    }

    private bool MatchesAnyTag(GameObject go)
    {
        foreach (var tag in _tagFilters)
        {
            if (!string.IsNullOrEmpty(tag) && go.CompareTag(tag))
                return true;
        }

        return false;
    }
}
