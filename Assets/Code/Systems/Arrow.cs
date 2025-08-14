using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _lifeSeconds = 4f;

    private void Awake()
    {
        Destroy(gameObject, _lifeSeconds);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Zombie>(out var zombie))
        {
            zombie.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}
