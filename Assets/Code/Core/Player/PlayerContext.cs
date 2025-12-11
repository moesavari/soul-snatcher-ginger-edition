using System;
using UnityEngine;

[DefaultExecutionOrder(-5000)]
public class PlayerContext : MonoSingleton<PlayerContext>
{
    [SerializeField] private PlayerFacade _facade;
    public PlayerFacade facade => _facade;

    public event Action OnPlayerReady;
    public event Action OnPlayerCleared;

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
