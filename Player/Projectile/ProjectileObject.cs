using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileObject : MonoBehaviour
{
    //this script should be on the projectile prefab

    [SerializeField] private Rigidbody _rb;
    [SerializeField] private Collider _col;
    [SerializeField] private ProjectileEffect[] _projectileEffect;
    [SerializeField] private ParticleSystem _atractionParticleSystem;
    private CameraController _cam;

    private float _stunTime = 1.0f;
    private float _timeToDone;

    private bool _charging;

    private float _projectileSpeed;
    private float _maxChargeTime;
    private float _baseStunTime;
    private float _deltaSize;
    private string _enemyTag;
    private Vector3 _projectileRayOrigin;
    private LayerMask _ignorePlayer;
    private ParticleSystem _projectileImpactEffect;
    /// <summary>
    /// Returns length of time to be stunned
    /// </summary>
    public float stunTime
    {
        get { return _stunTime; }
    }

    // Use this for initialization
    void Start()
    {
        if (!_rb)
            _rb = GetComponent<Rigidbody>();
        if (!_col)
            _col = GetComponent<Collider>();
        if (_projectileEffect == null || _projectileEffect.Length == 0)
            _projectileEffect = GetComponentsInChildren<ProjectileEffect>();
        if (!_atractionParticleSystem)
            _atractionParticleSystem = GetComponent<ParticleSystem>();
        _rb.useGravity = false;
        _col.enabled = false;
        _rb.isKinematic = true;
    }

    public void StartCharge(float projectileSpeed, float maxChargeTime, float baseStunTime, float deltaSize, string enemyTag, CameraController cam, Vector3 projectileRayOrigin, LayerMask ignorePlayer, ParticleSystem projectileImpactEffect)
    {
        _projectileSpeed = projectileSpeed;
        _maxChargeTime = maxChargeTime;
        _baseStunTime = baseStunTime;
        _deltaSize = deltaSize;
        _enemyTag = enemyTag;
        _cam = cam;
        _projectileRayOrigin = projectileRayOrigin;
        _ignorePlayer = ignorePlayer;
        _projectileImpactEffect = projectileImpactEffect;

        for (int i = 0; i < _projectileEffect.Length; i++)
            _projectileEffect[i].OnStart(_maxChargeTime);

        _charging = true;

        StartCoroutine(Charge());
    }

    public void Fire()
    {
        if (_rb == null)
        {
            Destroy(gameObject);
            return;
        }
        Destroy(gameObject, 20.0f);
        _charging = false;

        for (int i = 0; i < _projectileEffect.Length; i++)
            _projectileEffect[i].UpdateSize(2.0f, _maxChargeTime, true);

        Vector3 direction = Vector3.zero;
        direction = _cam.GetCentreView(_projectileRayOrigin, _ignorePlayer) - transform.position;

        FinishedCharge();
        _rb.isKinematic = false;
        _rb.velocity = direction.normalized * _projectileSpeed;
        _col.enabled = true;
        transform.parent = null;
        StopAllCoroutines();
        _stunTime += _baseStunTime;
    }

    private void FinishedCharge()
    {
        _atractionParticleSystem.Stop();
    }

    private void PlayCollisionEffect()
    {
        if (_projectileImpactEffect == null)
            return;
        _projectileImpactEffect.transform.position = transform.position;
        transform.LookAt(GameManager.instance.player.transform.position);
        _projectileImpactEffect.Play();
    }

    private void OnTriggerEnter(Collider c)
    {
        //Debug.Log(c.ClosestP

        if (c.gameObject.CompareTag(_enemyTag))
        {
            SoundManager.instance.PlaySound(SoundManager.instance.auraProjectileImpact, GetComponent<AudioSource>(), true);
            c.gameObject.GetComponent<Enemy>().TakeHit(stunTime, true);

            PlayCollisionEffect();

            Destroy(gameObject);
        }
        else if (!c.gameObject.CompareTag(GameManager.Tags.Player.ToString()) && !c.gameObject.CompareTag(GameManager.Tags.Projectile.ToString()) && !c.gameObject.GetComponent<Collider>().isTrigger)
        {
            SoundManager.instance.PlaySound(SoundManager.instance.auraProjectileImpact, GetComponent<AudioSource>(), true);

            PlayCollisionEffect();

            Destroy(gameObject);
        }
    }

    private IEnumerator Charge()
    {
        float deltaC = 0.1f;

        yield return new WaitForSeconds(deltaC);

        if (_timeToDone < _maxChargeTime)
        {
            _stunTime += deltaC;
            _timeToDone += deltaC;
            for (int i = 0; i < _projectileEffect.Length; i++)
                _projectileEffect[i].UpdateSize(deltaC, _maxChargeTime, false);
            StartCoroutine(Charge());
        }
        else
        {
            _timeToDone = 0.0f;
            FinishedCharge();
        }
    }

    public void Cancel()
    {
        Destroy(gameObject);
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(2);
        Destroy(gameObject);
    }
}