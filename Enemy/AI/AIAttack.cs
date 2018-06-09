using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAttack : MonoBehaviour
{
    [SerializeField] private AttackMode _attackMode;
    [SerializeField] private LayerMask _targetMask;
    [SerializeField] private Enemy _enemy;
    [SerializeField] private float _attackRate;
    [SerializeField] private float _attackAngle;
    [SerializeField] private float _speed = 5.0f;

    [Header("Melee")]
    [SerializeField]
    private float _attackReach = 1.0f;
    [SerializeField] private float _attackWidth = 0.5f;
    [SerializeField] private int _meleeDamageAmount = 10;

    [Header("Ranged")]
    [SerializeField]
    private float _projectileSpeed = 10.0f;
    [SerializeField] private GameObject _projectilePrefab = null;
    [SerializeField] private Transform _projectileOrigin = null;
    [SerializeField] private int _projectileDamageAmount = 10;

    private float _elapsedTime = 0.0f;

    public AttackMode attackMode { get { return _attackMode; } }

    public enum AttackMode
    {
        Melee,
        Ranged
    }

    void Start()
    {
        if (!_enemy)
            _enemy = GetComponent<Enemy>();
    }

    public void EnterAttack()
    {
        _elapsedTime = _attackRate;
        _enemy.navMeshAgent.speed = _speed;
        _enemy.navMeshAgent.autoBraking = false;
    }

    public void UpdateAttack()
    {
        //increase elapled time since last attack
        _elapsedTime += Time.deltaTime;

        // get the distance and angle between you and the target. Check them and the time since last attack to see if you can attack
        float distanceToTarget = Vector3.Distance(transform.position, _enemy.target.position);
        float angleToTarget = Vector3.Angle(transform.position, _enemy.target.position);
        if (_elapsedTime >= _attackRate && distanceToTarget <= _enemy.engageDistance && angleToTarget <= _attackAngle)
        {
            Attack();
            _elapsedTime = 0.0f;
        }

        if (distanceToTarget > _enemy.engageDistance)
        {
            _enemy.navMeshAgent.SetDestination(_enemy.target.position);
        }
        else
        {
            _enemy.navMeshAgent.SetDestination(transform.position);
            _enemy.RotateTowards(_enemy.target.position, 10.0f);
        }

        // < THINGS THAT NEED TO BE TRUE TO PHYSICALLY ATTACK >
        // x amount of time has passed since last attack 
        // player is within attacking range (very close for melee further away for ranged)
        // angle between player and enemy needs to be within a certain angle
        // < IF THAT IS NOT TRUE >
        // try to run away from player to make the angle less sharp

        // < THINGS THAT HAPPEN EVERY FRAME >
        // elapsed time increases by delta time
        // enemy moves towards attacking range distance unless they're already there
    }

    public void ExitAttack() { }

    public void Attack()
    {
        switch (_attackMode)
        {
            case AttackMode.Melee:
                Melee();
                break;
            case AttackMode.Ranged:
                Ranged();
                break;
        }
    }
    private void Melee()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);
        Debug.DrawRay(ray.origin, ray.direction, Color.black);

        if (Physics.SphereCast(ray, _attackWidth, out hit, _attackReach, _targetMask))
        {
            hit.collider.GetComponent<CharacterBehaviour>().Damage(_meleeDamageAmount);

            // play Melee sound effect
            SoundManager.instance.PlaySound(SoundManager.instance.guardMeleeAttack, _enemy.sfxSource, true);
        }
    }

    private void Ranged()
    {
        GameObject projectile = Instantiate(_projectilePrefab, _projectileOrigin.position, _enemy.transform.rotation);
        projectile.GetComponent<AIProjectile>().Launch(_projectileDamageAmount, _enemy.target.position, _projectileSpeed);
        Destroy(projectile, 30.0f);

        // play firing sound effect
        SoundManager.instance.PlaySound(SoundManager.instance.guardFireGun, _enemy.sfxSource, true);
    }
}
