using System;
using UnityEngine;


public class PanelVisibilityRelay : MonoBehaviour
{
    public UIPanelID panelID;

    public static event Action<UIPanelID, bool> OnPanelVisible;

    private void OnEnable()
    {
        OnPanelVisible?.Invoke(panelID, true);
    }

    private void OnDisable()
    {
        OnPanelVisible?.Invoke(panelID, false);
    }
}
