using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIProjectile : MonoBehaviour
{
    private Rigidbody _rb;

    public int damageAmount
    {
        get;
        private set;
    }

    /// <summary>
    /// Sets up and shoots the projectile in direction of its given target
    /// </summary>
    public void Launch(int damage, Vector3 target, float speed)
    {
        _rb = GetComponent<Rigidbody>();
        damageAmount = damage;

        Vector3 direction = target - transform.position;
        _rb.velocity = direction.normalized * speed;
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.CompareTag("Player"))
        {
            c.gameObject.GetComponent<CharacterBehaviour>().Damage(damageAmount);
            Destroy(gameObject);
        }
        else if (!c.gameObject.CompareTag("Projectile"))
        {
            Destroy(gameObject);
        }
    }
}
