using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
[DefaultExecutionOrder(9998)]
public class WorldRaycastProbe : MonoBehaviour
{
    public Camera cam;
    public float maxDistance = 100f;
    public LayerMask layers = ~0;

    void Update()
    {
        var c = cam ? cam : Camera.main;
        if (!c) return;

        var ray = c.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit3D, maxDistance, layers))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit3D.distance, Color.green);
            DebugManager.Log($"3D hit: {hit3D.collider.name}  layer={hit3D.collider.gameObject.layer}", this);
        }

        var hit2D = Physics2D.GetRayIntersection(ray, maxDistance, layers);
        if (hit2D.collider)
        {
            Debug.DrawLine(ray.origin, hit2D.point, Color.cyan);
            DebugManager.Log($"2D hit: {hit2D.collider.name}  layer={hit2D.collider.gameObject.layer}", this);
        }
    }
}
#endif
