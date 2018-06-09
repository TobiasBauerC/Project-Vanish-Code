/*
Instructions:
1: Add this script onto the player.
2: Add an empty gameobject onto the player that will the projectile spawn point. Drag and drop that gameobject into the Projectile Spawn field.
3: Drag and drop the camera into the Cam Controller field.
4: Place the prefab called Play_Projectile into the projectile prefab field. 
5: Default Projectile Settings are fine. If you want to change, deltasize is the change in size, projectile speed is how fast the projectile flies
	max charge time is the maximum amount of time you can charge the projectile.
6: Enemy string is the tag the enemy will have.
7: Drag every componenet you want disabled into the componenets to disable array. These componenets will be deactivated while this skill is being used.
6: Finished :)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePlayer : MonoBehaviour
{
    [Header("Definitions")]
    [SerializeField]
    protected float _deltaSize = 0.05f;
    [SerializeField] protected float _projectileSpeed = 10.0f;
    [SerializeField] protected float _maxChargeTime = 10.0f;
    [SerializeField] private float _baseCoolDownTime = 1.0f;
    public float coolDown
    {
        get { return _baseCoolDownTime; }
        set { _baseCoolDownTime = value; }
    }

    [SerializeField] private float _baseStunTime = 1.0f;
    [SerializeField] private bool _useCoolDown = true;
    [SerializeField] private string _enemyTag = "Enemy";
    [SerializeField] private LayerMask _ignorePlayer;

    [Header("Drag and Drop")]
    [SerializeField]
    private Transform _projectileSpawn;
    [SerializeField] private CameraController _camController;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _projectileRayOrigin;
    [SerializeField] private ParticleSystem _projectileImpactEffect;
    //[SerializeField] private CoolDown _coolDown;
    [SerializeField] private CharacterBehaviour _CB;
    /** For cool down HUD */
    [SerializeField] private OnPlayerHUD _onPlayerHUD;

    [Header("Disable")]

    [SerializeField]
    private MonoBehaviour[] _componentsToDisable;

    private WaitForSeconds _timeCountWait = new WaitForSeconds(1.0f);
    private ProjectileObject _currentProjectile;
    private Coroutine _timerCountCoroutine;
    private bool _projSpawned = false;
    private float _coolDownTime = 1.0f;

    Animator _animator;

    // sound variables
    public bool _shouldSpeak;
    // private WaitForSecondsRealtime _dialogueWait = new WaitForSecondsRealtime(3.0f);
    private bool _charging = false;
    float dampVelocity = 0;
    float dampVelocity2 = 0;

    

    /// <summary>
    /// Returns Cool Down Time (time until power can be used again)
    /// </summary>
    public float coolDownTime
    {
        get { return _coolDownTime; }
    }

    void Start()
    {
        _shouldSpeak = true;
        _projSpawned = false;

        _CB = GetComponent<CharacterBehaviour>();
        _animator = GetComponent<Animator>();

        _charging = false;

        if (!_camController)
        {
            // Debug.LogWarning("No CameraController reference in inspector. Looking up object.");
            _camController = Camera.main.GetComponent<CameraController>();
        }
        if (!_projectilePrefab)
            Debug.LogError("No prefab for projectile");
        if (!_projectileSpawn)
            Debug.LogError("No spawnpoint for projectile");
        // if (!_coolDown)
        // {
        //     // Debug.LogWarning("No CoolDown reference in inspector. Looking up object.");
        //     _coolDown = GameObject.Find("CoolDownProj").GetComponent<CoolDown>();
        // }
        /** Check to see if there is an OnPlayerHUD */
        if (!_onPlayerHUD)
            _onPlayerHUD = GetComponent<OnPlayerHUD>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.gameOver)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0))
            ProjSpawn();

        if (Input.GetMouseButtonDown(1))
        {
            if (_currentProjectile)
                Cancel();
        }

        if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
        {
            if (_currentProjectile)
            {
                ProjFire();
            }
        }

        if (_charging)
        {
            //float newVolume = Mathf.SmoothDamp(SoundManager.instance.SoundSource.volume, 1, ref dampVelocity, _maxChargeTime);
            //SoundManager.instance.SoundSource.volume = newVolume;
            float newPitch = Mathf.SmoothDamp(SoundManager.instance.SoundSource.pitch, 2, ref dampVelocity2, _maxChargeTime);
            SoundManager.instance.SoundSource.pitch = newPitch;
        }
    }

    private void ProjSpawn()
    {
        if (!_projSpawned)
        {
            if (_useCoolDown)
            {
                _timerCountCoroutine = StartCoroutine(TimeCounter());
            }

            if (!_CB.IsCrouching)
            {
                _animator.SetBool("Crouching", true);
            }

            _animator.SetBool("isCharging", true);

            var projectile = (GameObject)Instantiate(_projectilePrefab,
                _projectileSpawn.position,
                _projectileSpawn.rotation,
                _projectileSpawn);

            _currentProjectile = projectile.GetComponent<ProjectileObject>();

            _projSpawned = true;

            SetComponentsActive(false);

            _currentProjectile.StartCharge(_projectileSpeed,
                _maxChargeTime,
                _baseStunTime,
                _deltaSize,
                _enemyTag,
                _camController,
                _projectileRayOrigin.position,
                _ignorePlayer,
                _projectileImpactEffect != null ? _projectileImpactEffect : null);

            // play charge sound
            _charging = true;
            //SoundManager.instance.SoundSource.volume = 1f;
            SoundManager.instance.SoundSource.pitch = 1;
            SoundManager.instance.PlaySound(SoundManager.instance.auraProjectileChargeStart, SoundManager.instance.SoundSource, true);
            SoundManager.instance.SoundSource.loop = true;
            StartCoroutine(ProjectileChargeSound(SoundManager.instance.SoundSource.clip.length));
        }
    }

    private IEnumerator ProjectileChargeSound(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if (_charging)
        {
            SoundManager.instance.PlaySound(SoundManager.instance.auraProjectileChargeLoop, SoundManager.instance.SoundSource, true);
        }
        // SoundManager.instance.SoundSource.loop = true;
    }

    public void ProjFire()
    {
        if (_currentProjectile)
        {
            _currentProjectile.Fire();

            _animator.SetTrigger("Fire");


            StopChargingSound();
            SoundManager.instance.PlaySound(SoundManager.instance.auraProjectileFire, SoundManager.instance.SoundSource, true);

            // dialogue triggering
            if (_shouldSpeak)
            {
                //_shouldSpeak = false;
                SoundManager.instance.PlayAuraDialogue(SoundManager.instance.AuraVoiceSource, SoundManager.AuraDialogueEvent.Fire, ref SoundManager.instance.auraProjectileDialogueArrayIndex, false);
                //StartCoroutine(DialogueCooldown());
            }

            if (_useCoolDown)
            {
                if (_coolDownTime < _baseCoolDownTime)
                    _coolDownTime = _baseCoolDownTime;

                // if (_coolDown)
                //     _coolDown.StartCoolDown(_coolDownTime);
                /** Start the cool down for the projectile */
                _onPlayerHUD.StartProjectileCoolDown(_coolDownTime);

                StopAllCoroutines();
                StartCoroutine(CoolDown());
            }
            else
            {
                _projSpawned = false;
            }

            if (!_CB.IsCrouching)
            {
                _animator.SetBool("Crouching", false);
            }

            _animator.SetBool("isCharging", false);
        }

        SetComponentsActive(true);
        _currentProjectile = null;
    }

    private void SetComponentsActive(bool pState)
    {
        if (_componentsToDisable.Length <= 0)
            return;

        foreach (MonoBehaviour script in _componentsToDisable)
            script.enabled = pState;
    }

    private IEnumerator CoolDown()
    {
        yield return new WaitForSeconds(_coolDownTime);

        _coolDownTime = 0.0f;
        _projSpawned = false;
    }

    private IEnumerator TimeCounter()
    {
        _coolDownTime += 1.0f;
        yield return _timeCountWait;

        if (_coolDownTime < 10)
            StartCoroutine(TimeCounter());
    }

    /*
    private IEnumerator DialogueCooldown()
    {
        Debug.Log("STARTING DIALOGUE COROUTINE");
        yield return _dialogueWait;
        Debug.Log("ENDING DIALOGUE COROUTINE");
        _shouldSpeak = true;
    }
    */

    public void Cancel()
    {
        Debug.Log("Cancelling projectile");

        _animator.SetBool("isCharging", false);

        if(gameObject.GetComponent<CharacterBehaviour>().IsCrouching)
        {
            _animator.SetBool("Crouching", true);
        }
        else
        {
            _animator.SetBool("Crouching", false);
        }

        StopCoroutine(_timerCountCoroutine);
        _coolDownTime = 0.0f;
        _projSpawned = false;
        _currentProjectile.Cancel();
        StopChargingSound();
        // SoundManager.instance.SoundSource.Stop();
    }

    private void StopChargingSound()
    {
        _charging = false;
        // StartCoroutine(StopCharging());
        SoundManager.instance.SoundSource.Stop();
        SoundManager.instance.SoundSource.pitch = 1;
        SoundManager.instance.SoundSource.loop = false;
    }

    private IEnumerator StopCharging()
    {
        yield return new WaitForEndOfFrame();
        _charging = false;
    }

    public bool HasProjectile()
    {
        return _charging;
    }
}