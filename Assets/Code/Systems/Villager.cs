using UnityEngine;

public class Villager : MonoBehaviour
{
    [SerializeField] private Transform _hideSpot;
    [SerializeField] private float _wanderRadius = 1.5f;
    [SerializeField] private float _moveSpeed = 1.4f;

    private Vector3 _startPos;
    private Vector3 _wanderTarget;

    private void Awake()
    {
        _startPos = transform.position;
        PickNewWander();
        TimeCycleManager.OnDayStarted += OnDay;
        TimeCycleManager.OnNightStarted += OnNight;
    }

    private void OnDestroy()
    {
        TimeCycleManager.OnDayStarted -= OnDay;
        TimeCycleManager.OnNightStarted -= OnNight;
    }

    private void Update()
    {
        if (TimeCycleManager.Instance.isNight)
            MoveTo(_hideSpot != null ? _hideSpot.position : _startPos);
        else Wander();
    }

    private void OnDay()
    {
        PickNewWander();
    }

    private void OnNight()
    {
        // TODO: panic mode
    }

    private void Wander()
    {
        if ((transform.position - _wanderTarget).sqrMagnitude < 0.05f)
            PickNewWander();

        MoveTo(_wanderTarget);
    }

    private void PickNewWander()
    {
        Vector2 r = Random.insideUnitCircle * _wanderRadius;
        _wanderTarget = _startPos + new Vector3(r.x, r.y, 0f);
    }

    private void MoveTo(Vector3 dest)
    {
        transform.position = Vector3.MoveTowards(transform.position, dest, _moveSpeed * Time.deltaTime);
    }
}
