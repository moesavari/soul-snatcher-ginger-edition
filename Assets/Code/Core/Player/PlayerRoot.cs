using UnityEngine;

[DisallowMultipleComponent]
public class PlayerRoot : MonoBehaviour
{
    [SerializeField] private PlayerFacade _facade;

    private void Awake()
    {
        if (!_facade) _facade = GetComponent<PlayerFacade>();
        if (_facade == null)
        {
            DebugManager.LogWarning("Missing PlayerFacade.", this);
            return;
        }

        if (PlayerContext.Instance == null)
        {
            var ctxGO = new GameObject("PlayerContext");
            ctxGO.AddComponent<PlayerContext>();
        }
        PlayerContext.Instance.Register(_facade);
    }

    private void OnDestroy()
    {
        if (PlayerContext.Instance != null)
            PlayerContext.Instance.Clear();
    }
}