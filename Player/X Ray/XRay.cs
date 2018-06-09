using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRay : MonoBehaviour
{
    [SerializeField] private Transform _orb;
    [SerializeField] private float _maxDiameter = 80.0f;
    public float maxDiameter { get { return _maxDiameter; } }

    [SerializeField] private float _speed = 2.5f;
    [SerializeField] private float _effectTime = 5.0f;
    [SerializeField] private float _coolDownTime = 10.0f;
    [SerializeField] private OnPlayerHUD _onPlayerHUD;
    [SerializeField] private Transform _cameraTransform;
    public Transform cameraTransform { get { return _cameraTransform; } }
    public float coolDown
    {
        get { return _coolDownTime; }
        set { _coolDownTime = value; }
    }

    public float coolDownTime
    {
        get { return _effectTime + _coolDownTime + (_maxDiameter / _speed); }
    }

    public List<XRayable> _effectedEnemies = new List<XRayable>();

    private float _currentDiameter = 0.0f;
    private bool _xraySearching = false;
    private bool _xrayActive = false;
    public bool xrayActive { get { return _xrayActive; } }
    private bool _canXray = true;
    private Coroutine _xrayTimer;
    private Coroutine _xrayCoolDown;

    Animator _animator;

    void Start()
    {
        _animator = GetComponent<Animator>();

        if (_maxDiameter <= 0.0f)
            Debug.LogError("The value for xray radius is too low. Make sure it is more than 0.0f");
        if (_speed <= 0.0f)
            Debug.LogError("The value for xray speed is too low. Make sure it is more than 0.0f");
        if (!_cameraTransform)
            _cameraTransform = Camera.main.transform;

        //seting default values
        _orb.localScale = Vector3.zero;
        _orb.GetComponent<Collider>().isTrigger = true;
        _currentDiameter = 0.0f;
        _xraySearching = false;
        _canXray = true;

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

        if (_effectedEnemies.Count > 0)
        {
            for (int i = 0; i < _effectedEnemies.Count; i++)
            {
                _effectedEnemies[i].UpdateXrayStatus();
            }
        }

        if (Input.GetKeyDown(KeyCode.E) && _canXray)
        {
            ActivateXray();
        }

        if (_xraySearching)
        {
            UpdateScale(_speed, _maxDiameter);
        }
    }

    /// <summary>
    /// Adds an xray object.
    /// </summary>
    /// <param name="xrayObject">Xray object.</param>
    public void AddXrayObject(GameObject xrayObject)
    {
        _effectedEnemies.Add(new XRayable(xrayObject, this));
    }

    /// <summary>
    /// Activates the xray.
    /// </summary>
    private void ActivateXray()
    {
        //Initialize the xray
        if (_xrayTimer != null)
            StopCoroutine(_xrayTimer);
        _canXray = false;
        // _coolDown.StartCoolDown(coolDownTime);
        /** Start the cool down for xray */
        _onPlayerHUD.StartXrayCoolDown(coolDownTime);
        _xraySearching = false;
        _orb.localScale = Vector3.zero;
        _currentDiameter = 0.0f;

        _animator.SetTrigger("XRAY");

        //start the search
        _xraySearching = true;
        _xrayActive = true;

        // play aura dialogue
        SoundManager.instance.PlayAuraDialogue(SoundManager.instance.AuraVoiceSource, SoundManager.AuraDialogueEvent.Xray, ref SoundManager.instance.auraXrayDialogueArrayIndex, false);

        // play sound effect
        SoundManager.instance.PlaySound(SoundManager.instance.xRayActivate, SoundManager.instance.SoundSource, false);
    }

    /// <summary>
    /// Updates the scale.
    /// </summary>
    /// <param name="speed">Speed.</param>
    private void UpdateScale(float speed, float maxRadius)
    {
        _currentDiameter += speed * Time.deltaTime;

        Vector3 newScale = _orb.localScale;
        newScale.x = _currentDiameter;
        newScale.y = _currentDiameter;
        newScale.z = _currentDiameter;
        _orb.localScale = newScale;

        if (_currentDiameter >= maxRadius)
        {
            _xraySearching = false;
            _orb.localScale = Vector3.zero;
            _xrayTimer = StartCoroutine(XrayTimer());
        }
    }

    private void EndXray()
    {
        _xrayActive = false;
        foreach (XRayable x in _effectedEnemies)
            x.EndXray();
        _effectedEnemies.Clear();
    }

    private IEnumerator XrayTimer()
    {
        yield return new WaitForSeconds(_effectTime);
        EndXray();
        _xrayCoolDown = StartCoroutine(XrayCoolDown());
    }

    private IEnumerator XrayCoolDown()
    {
        yield return new WaitForSeconds(_coolDownTime);
        _canXray = true;
    }
}

/** Class for the object that is beign xrayed */
public class XRayable
{
    private XRay _owner;
    private Transform _targetTransform;
    private Renderer[] _targetRenderers;

    /// <summary>
    /// Initializes a new instance of the <see cref="T:XRay.XRayable"/> class.
    /// </summary>
    /// <param name="target">Target.</param>
    public XRayable(GameObject target, XRay owner)
    {
        Enemy enemy = target.GetComponent<Enemy>();
        _targetTransform = target.transform;
        _owner = owner;

        if (!enemy)
        {
            Supporting.Log(string.Format("Can't Xray {0} as it's not an enemy", 1));
        }

        _targetRenderers = enemy.xRayAffectedMeshes;
        if (_targetRenderers.Length > 0)
        {
            foreach (Renderer renderer in _targetRenderers)
            {
                if (renderer)
                {
                    ApplyXRayEffect(renderer);
                }
                else
                {
                    throw new ArgumentNullException(string.Format("Renderer not found for {0}", _targetTransform.name));
                }
            }
        }
        else
        {
            throw new ArgumentNullException(string.Format("Renderer not found for {0}", _targetTransform.name));
        }
    }

    /** Check to see if the xray effect should be on the enemy or not */
    public void UpdateXrayStatus()
    {
        if (!_owner.xrayActive)
            return;

        foreach (Renderer renderer in _targetRenderers)
        {
            int mask = ~LayerMask.NameToLayer(GameManager.Layers.Actors.ToString());
            bool canSee = !Physics.Linecast(_owner.cameraTransform.position, _targetTransform.position, mask);
            int currentStatus = (int) renderer.material.GetFloat("_XOpacity");
            if (canSee && currentStatus == 1)
            {
                RemoveXRayEffect(renderer);
            }
            else if (!canSee && currentStatus == 0)
            {
                ApplyXRayEffect(renderer);
            }
        }
    }

    public void EndXray()
    {
        // Debug.Log("I'm getting called");
        foreach (Renderer renderer in _targetRenderers)
        {
            RemoveXRayEffect(renderer);
        }
    }

    private void ApplyXRayEffect(Renderer renderer)
    {
        renderer.material.SetFloat("_XOpacity", 1);
        if (renderer.material.GetFloat("_XLength") != _owner.maxDiameter)
            renderer.material.SetFloat("_XLength", _owner.maxDiameter);

        Supporting.Log(string.Format("Setting {0} xray opacity to 1", renderer.name), 2);
    }

    private void RemoveXRayEffect(Renderer renderer)
    {
        renderer.material.SetFloat("_XOpacity", 0);

        Supporting.Log(string.Format("Setting {0} xray opacity to 0", renderer.name), 2);
    }
}