using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class UIRaycastAudit : MonoBehaviour
{
    [Tooltip("Root of the UI you want to audit (e.g., InventoryHUD or EquipmentPanel root).")]
    public GameObject root;

    [ContextMenu("Run Audit")]
    public void RunAudit()
    {
        if (!root) root = gameObject;

        var canvas = root.GetComponentInParent<Canvas>(true);
        if (!canvas) { DebugManager.LogError("No Canvas found in parents.", this); return; }

        var gr = canvas.GetComponent<GraphicRaycaster>();
        DebugManager.Log($"Canvas: {canvas.name}  mode={canvas.renderMode}  hasGraphicRaycaster={(gr != null && gr.isActiveAndEnabled)}", this);

        if (!gr || !gr.isActiveAndEnabled)
            DebugManager.LogError("Missing or disabled GraphicRaycaster on this Canvas.", this);

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && !canvas.worldCamera)
            DebugManager.LogWarning("Canvas needs an Event Camera (ScreenSpace-Camera/WorldSpace). Assign Main Camera.", this);

        foreach (var cg in canvas.GetComponentsInChildren<CanvasGroup>(true))
        {
            if (!cg.blocksRaycasts)
                DebugManager.LogWarning($"CanvasGroup blocksRaycasts=FALSE at {cg.name} (children won't receive pointer).", this);
        }

        var images = canvas.GetComponentsInChildren<Image>(true);
        int off = images.Count(i => i.raycastTarget == false);
        int on = images.Length - off;
        DebugManager.Log($"Images: {images.Length} total | raycastTarget ON={on}, OFF={off}", this);

        var buttons = canvas.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons.Take(10))
        {
            var img = b.GetComponent<Image>();
            DebugManager.Log($"Button '{b.name}' raycastTarget={(img ? img.raycastTarget : false)} interactable={b.interactable}", this);
        }
    }

    void Start() { RunAudit(); }
}
#endif
