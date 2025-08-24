using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]

public class ZombieBite : MonoBehaviour
{
    [SerializeField] private int _damagePerTick = 1;
    [SerializeField] private float _tickInterval = 0.6f;

    private bool _isBiting = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        
        if (_isBiting) return;

        StartCoroutine(BiteLoop(collision));
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        _isBiting = false;
    }

    private IEnumerator BiteLoop(Collider2D playerCol)
    {
        _isBiting = true;

        while (_isBiting && playerCol != null)
        {
            if (playerCol.TryGetComponent<Health>(out var hp))
                hp.TakeDamage(_damagePerTick, transform.position, gameObject);

            yield return new WaitForSeconds(_tickInterval);
        }
    }
}