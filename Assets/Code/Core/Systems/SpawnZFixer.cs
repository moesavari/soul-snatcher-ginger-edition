using UnityEngine;

public class SpawnZFixer : MonoBehaviour
{
    [SerializeField] private float _overrideZ = float.NaN;

    private void Awake()
    {
        float z = float.IsNaN(_overrideZ) ? (SpawnManager.Instance != null ? SpawnManager.Instance.targetZ : -0.01f) : _overrideZ;
        Vector3 p = transform.position;
        if (!Mathf.Approximately(p.z, z)) 
            transform.position = new Vector3(p.x, p.y, z);
    }
}
