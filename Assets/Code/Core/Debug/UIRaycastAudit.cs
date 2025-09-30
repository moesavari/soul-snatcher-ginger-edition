using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIRaycastAudit : MonoBehaviour
{
    [Tooltip("Root of the UI you want to audit (e.g., InventoryHUD or EquipmentPanel root).")]
    public GameObject root;

    [ContextMenu("Run Audit")]
    public void RunAudit()
    {
        if (!root) root = gameObject;

        var canvas = root.GetComponentInParent<Canvas>(true);
        if (!canvas) { DebugManager.LogError("[UIRaycastAudit] No Canvas found in parents."); return; }

        var gr = canvas.GetComponent<GraphicRaycaster>();
        DebugManager.Log($"[UIRaycastAudit] Canvas: {canvas.name}  mode={canvas.renderMode}  hasGraphicRaycaster={(gr != null && gr.isActiveAndEnabled)}");

        if (!gr || !gr.isActiveAndEnabled)
            DebugManager.LogError("[UIRaycastAudit] Missing or disabled GraphicRaycaster on this Canvas.");

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && !canvas.worldCamera)
            DebugManager.LogWarning("[UIRaycastAudit] Canvas needs an Event Camera (ScreenSpace-Camera/WorldSpace). Assign Main Camera.");

        // Check CanvasGroups that kill raycasts
        foreach (var cg in canvas.GetComponentsInChildren<CanvasGroup>(true))
        {
            if (!cg.blocksRaycasts)
                DebugManager.LogWarning($"[UIRaycastAudit] CanvasGroup blocksRaycasts=FALSE at {cg.name} (children won't receive pointer).");
        }

        // Check typical slot images/buttons
        var images = canvas.GetComponentsInChildren<Image>(true);
        int off = images.Count(i => i.raycastTarget == false);
        int on = images.Length - off;
        DebugManager.Log($"[UIRaycastAudit] Images: {images.Length} total | raycastTarget ON={on}, OFF={off}");

        // Print first few likely slot buttons with raycastTarget state
        var buttons = canvas.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons.Take(10))
        {
            var img = b.GetComponent<Image>();
            DebugManager.Log($"[UIRaycastAudit] Button '{b.name}' raycastTarget={(img ? img.raycastTarget : false)} interactable={b.interactable}");
        }
    }

    void Start() { RunAudit(); }
}
