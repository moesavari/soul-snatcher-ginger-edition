using System;
using UnityEngine;

[DefaultExecutionOrder(-5000)]
public class PlayerContext : MonoBehaviour
{
    public static PlayerContext Instance { get; private set; }

    [SerializeField] private PlayerFacade _facade;
    public PlayerFacade facade => _facade;

    public event Action OnPlayerReady;
    public event Action OnPlayerCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(PlayerFacade f)
    {
        _facade = f;
        OnPlayerReady?.Invoke();
    }

    public void Clear()
    {
        _facade = null;
        OnPlayerCleared?.Invoke();
    }
}