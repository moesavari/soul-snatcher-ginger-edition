using Game.Systems;
using UnityEngine;

public class Villager : MonoBehaviour
{
    [SerializeField] private Transform _hideSpot;
    [SerializeField] private float _wanderRadius = 1.5f;
    [SerializeField] private float _moveSpeed = 1.4f;

    [Header("Values Adjustment")]
    [SerializeField] private int _repAmount = 10;
    [SerializeField] private int _soulAmount = 1;

    private Vector3 _startPos;
    private Vector3 _wanderTarget;

    private void Awake()
    {
        _startPos = transform.position;
        PickNewWanderTarget();
        GameEvents.DayStarted += OnDay;
        GameEvents.NightStarted += OnNight;
    }

    private void OnDestroy()
    {
        GameEvents.DayStarted -= OnDay;
        GameEvents.NightStarted -= OnNight;
    }

    private void Update()
    {
        MoveTo(TimeCycleManager.Instance.isNight
                ? (_hideSpot != null ? _hideSpot.position : _startPos)
                : _wanderTarget);

        if (Input.GetKeyDown(KeyCode.R))
        {
            OnSoulAbsorb();
            GetComponent<Health>().Die();
        }
    }

    private void OnDay()
    {
        PickNewWanderTarget();
    }

    private void OnNight()
    {
        // TODO: panic mode
    }

    private void Wander()
    {
        if ((transform.position - _wanderTarget).sqrMagnitude < 0.05f)
            PickNewWanderTarget();

        MoveTo(_wanderTarget);
    }

    private void PickNewWanderTarget()
    {
        _wanderTarget = _startPos + new Vector3(
                        UnityEngine.Random.insideUnitCircle.x * _wanderRadius,
                        UnityEngine.Random.insideUnitCircle.y * _wanderRadius,
                        0f);
    }

    public void OnSoulAbsorb()
    {
        SoulSystem.Instance.AddSouls(_soulAmount);
        ReputationSystem.Instance.AddReputation(-_repAmount);
    }

    private void MoveTo(Vector3 dest)
    {
        transform.position = Vector3.MoveTowards(transform.position, dest, _moveSpeed * Time.deltaTime);
    }
}
