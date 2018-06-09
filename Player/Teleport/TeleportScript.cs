// Teleport Mechanic
// Author: Garrett May

// Instructions for Use:
// Attach this script to the player
// Attach Teleport Point Prefab (Garrett's Folder/Prefabs) into the scene as a child of the player
// Attach Player Shadow Prefab (Garrett's Folder/Prefabs) into the scene as a child of the player
// Attach Teleport Radius (Garrett's Folder/Prefabs) into the scene as a child of the player
// Drag the newly added Teleport point prefab into the Serialized Field "Teleport Point" that appears on this script in the inspector
// Drag the newly added Player Shadow prefab into the Serialized Field "Fake Player" that appears on this script in the inspector
// Drag the newly added Teleport Radius prefab into the Serialized Field "Radius" that appears on this script in the inspector
// Check each newly added prefabs transform positions and make sure they are all set to (0,0,0);
// Check to make sure that the camera in the scene is tagged as "MainCamera"

// Drag the Player/CameraController into the Main Cam variable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportScript : MonoBehaviour
{
    // fake player, teleport point and radius objects assigned in the inspector
    [SerializeField] private GameObject _fakePlayer;
    [SerializeField] private GameObject _teleportPoint;
    [SerializeField] private GameObject _radius;

    // minimum and maximum teleport distances
    private float _maxTeleportDistance = 10;

    // used for cooldown of teleport
    [SerializeField] private bool _isCooled;
    private float _coolDown = 2;
    public float cooldown
    {
        get { return _coolDown; }
        set { _coolDown = value; }
    }

    // used for making sure player cannot hold down shift while cooling down then not be able to see radius
    private bool _isActive;
    public bool isActive { get { return _isActive; } }

    // layermask for Raycast Collision
    [SerializeField]
    LayerMask _layerMask;

    // teleporting behind things varaibles
    // player renderer
    [SerializeField] private Renderer _teleportPointRenderer;
    [SerializeField] private ParticleSystem _teleportParticleSystem;
    // default layermask
    [SerializeField] private LayerMask _everythingExceptActorsLayerMask;


    [SerializeField]
    private MonoBehaviour[] componentsToDisable;

    // hud reference for cooldown display
    // [SerializeField] CoolDown _hudCooldown;

    // Cam Controller
    [SerializeField] CameraController _mainCam;

    // player movement script
    [SerializeField] private CharacterControllerScheme _charBehaviourScript;
    [SerializeField] private CharacterBehaviour _characterBehaviour;
    /** HUD for cool down */
    [SerializeField] private OnPlayerHUD _onPlayerHUD;
    private Rigidbody _rb;
    private Animator _anim;

    /** Camera for screen raycasting */
    private Camera _mainCamera;

    /** Determine if player is in a keypad */
    private bool _isInKeypad;
    public bool isInKeypad
    {
        get { return _isInKeypad; }
        set { _isInKeypad = value; }
    }

    // sound variables
    private bool _shouldSpeak = true;

    // Use this for initialization
    void Start()
    {
        // make sure there is a teleport point in the scene
        if (!_teleportPoint)
        {
            Debug.Log("No Teleport Point Prefab added to Player.");
            _teleportPoint = GameObject.Find("TeleportPoint");
        }
        // make sure there is a Player Shadow in the scene
        if (!_fakePlayer)
        {
            Debug.Log("No player shadow prefab added to scene.");
            _fakePlayer = GameObject.Find("PlayerShadow");
        }
        // make sure there is a teleport radius in the scene
        if (!_radius)
        {
            Debug.Log("No teleport radius added to scene");
            _radius = GameObject.Find("TeleportRadius");
        }
        /** Check for OnPlayerHUD */
        if (!_onPlayerHUD)
            _onPlayerHUD = GetComponent<OnPlayerHUD>();

        if (!_mainCam)
        {
            _mainCam = Camera.main.GetComponent<CameraController>();
        }

        if (!_charBehaviourScript)
        {
            _charBehaviourScript = gameObject.GetComponent<CharacterControllerScheme>();
        }
        if (!_characterBehaviour)
        {
            _characterBehaviour = GetComponent<CharacterBehaviour>();
        }

        if (!_teleportPointRenderer)
        {
            _fakePlayer.SetActive(true);
            _teleportPointRenderer = GetComponentInChildren<MeshRenderer>();
            _fakePlayer.SetActive(false);
        }
        if (!_teleportParticleSystem)
        {
            _fakePlayer.SetActive(true);
            _teleportParticleSystem = GetComponentInChildren<ParticleSystem>();
            _fakePlayer.SetActive(false);
        }

        _rb = gameObject.GetComponent<Rigidbody>();
        _anim = gameObject.GetComponent<Animator>();
        _mainCamera = Camera.main;

        // initializing variables
        _isCooled = true;
        _isActive = false;
    }

    // Update is called once per frame
    void Update()
    {
        /** If game is not running, return */
        if (GameManager.instance.gameOver)
        {
            return;
        }

        /** Check to see if "Setup" input pressed */
        if (Input.GetKeyDown(KeyCode.Space) && !_isActive && _isCooled)
        {
            Setup();
        }

        /** Set the position of the indicator every frame tele is active */
        if (Input.GetKey(KeyCode.Space) && _isActive && _isCooled)
        {
            Vector3 targetPosition = FindBestPosition();
            bool isVisible = DetermineCanSee(targetPosition);
            SetTelePositionAndVisibility(targetPosition, isVisible);
            if (Input.GetKeyDown(KeyCode.Mouse0) && isVisible)
            {
                Teleport();
            }
        }

        /** Turn off teleport if player does not fire it or it is still on and not cooled down */
        if (Input.GetKeyUp(KeyCode.Space) && _isActive)
        {
            Deactivate();
        }
    }

    /** Initialize the teleport components */
    private void Setup()
    {
        /** Lock the camera rotation */
        GameManager.instance.LockCamera();
        GameManager.instance.UnlockCursor();
        /** activate the fake player and radius for visuals and position tracking */
        _fakePlayer.SetActive(true);
        _teleportPoint.SetActive(true);
        _radius.SetActive(true);
        _teleportPointRenderer.enabled = true;
        _teleportParticleSystem.Play();
        /** signal the teleport is active */
        _isActive = true;
        _anim.SetBool("isTele", true);
        /** Disable scripts that should not be running at the same time */
        foreach (MonoBehaviour script in componentsToDisable)
        {
            script.enabled = false;
        }
    }

    /** Turn off teleport when the player has not fired the teleport */
    private void Deactivate()
    {
        if (!_isInKeypad)
        {
            /** Unlock the camera rotation */
            GameManager.instance.UnlockCamera();
            GameManager.instance.LockCursor();
        }
        /** Turn of visuals and tracking */
        _fakePlayer.SetActive(false);
        _teleportPoint.SetActive(false);
        _radius.SetActive(false);
        /** Insure cool down does not start and set animation */
        _isCooled = true;
        _isActive = false;
        _anim.SetBool("isTele", false);
        /** enable scripts that should not be running at the same time */
        foreach (MonoBehaviour script in componentsToDisable)
        {
            script.enabled = true;
        }
    }

    /** Returns the best position for the fake player to be placed   */
    private Vector3 FindBestPosition()
    {
        /** Set up ray, hit, and Vector */
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPosition = transform.position;

        /** Shoot a ray forward to see where you would like to set the position */
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _layerMask))
        {
            /** Store the position you hit */
            targetPosition = hit.point;
            /** Move the position slightly closer to the player */
            Vector3 directionToMe = transform.position - targetPosition;
            directionToMe = new Vector3(directionToMe.x, 0.0f, directionToMe.z);
            targetPosition += (directionToMe.normalized * 1.0f);
            /** Determin if the point is not on the ground */
            if (Physics.Raycast(targetPosition, Vector3.down, out hit, Mathf.Infinity, _layerMask))
            {
                /** set the position to new location */
                targetPosition = hit.point;
            }
        }

        /** Check to see if the teleport point is close enough to the player on a flat plane */
        if (Vector3.Distance(transform.position, targetPosition) > _maxTeleportDistance)
        {
            /** Get direction between player and point */
            Vector3 direction = targetPosition - transform.position;
            /** Normalize vector of position */
            Vector3 normalizedDirection = direction.normalized;
            /** Set the position to the normalized vector times the maximum distance, but keep its y value */
            targetPosition = transform.position + (normalizedDirection * _maxTeleportDistance);
            targetPosition = new Vector3(targetPosition.x, targetPosition.y + 0.5f, targetPosition.z);

            /** One more check to make sure it it not floating */
            if (Physics.Raycast(targetPosition, Vector3.down, out hit, Mathf.Infinity, _layerMask))
            {
                /** set the target position to its new position */
                targetPosition = hit.point;
            }
        }

        /** return the final position */
        return targetPosition;
    }

    /** Returns wether or not the fake player is visible to the player */
    private bool DetermineCanSee(Vector3 targetPosition)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
        bool visibleToCamera = GeometryUtility.TestPlanesAABB(planes, _teleportPointRenderer.bounds);

        RaycastHit hit;
        if ((Physics.Raycast(_mainCamera.transform.position, targetPosition, out hit, _everythingExceptActorsLayerMask) && hit.collider.gameObject.layer.ToString() != "Water") || !visibleToCamera)
        {
            return false;
        }
        return true;
    }

    /** Sets the visibility then the position of the fake player */
    private void SetTelePositionAndVisibility(Vector3 targetPosition, bool isVisible)
    {
        /** Determin if the target position should be visible */
        if (_teleportPointRenderer.enabled && !isVisible)
        {
            _teleportPointRenderer.enabled = false;
            _teleportParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else if (!_teleportPointRenderer.enabled && isVisible)
        {
            _teleportPointRenderer.enabled = true;
            _teleportParticleSystem.Play();
        }

        /** Set position of fake player to target position */
        _fakePlayer.transform.position = targetPosition;
    }

    /** Teleports the player to the fake player's position */
    private void Teleport()
    {
        /** Set the post teleport animation */
        _anim.SetTrigger("isTeleDone");
        /** Unlock the camera */
        GameManager.instance.UnlockCamera();
        GameManager.instance.LockCursor();
        /** Turn off the visual points */
        _fakePlayer.SetActive(false);
        _teleportPoint.SetActive(false);
        _radius.SetActive(false);
        /** Let other scripts know that the teleport is no longer active */
        _isActive = false;
        /** Start the teleport cool down and call cool down teleport function on HUD*/
        StartCoroutine(Cooldown());
        _onPlayerHUD.StartTeleportCoolDown(2.0f);
        /** Start the teleport movement */
        StartCoroutine(DelayedTeleport(_fakePlayer.transform.position));
        /** Play the teleport sound effect */
        SoundManager.instance.PlaySound(SoundManager.instance.teleportActivate, SoundManager.instance.SoundSource, true);
        /** Trigger dialogue */
        if (_shouldSpeak)
        {
            _shouldSpeak = false;
            SoundManager.instance.PlayAuraDialogue(SoundManager.instance.AuraVoiceSource, SoundManager.AuraDialogueEvent.Teleport, ref SoundManager.instance.auraTeleportDialogueArrayIndex, false);
            StartCoroutine(DialogueCooldown());
        }
        /** Disable character movement to be reenabled after teleporting */
        _charBehaviourScript.enabled = false;
        _rb.velocity = Vector3.zero;
        /** Re-enable the effected scriptss  */
        foreach (MonoBehaviour script in componentsToDisable)
        {
            script.enabled = true;
        }
    }

    /** For making sure the teleport indicators are not on why script is off */
    void OnDisable()
    {
        if (_characterBehaviour.isOnElevator && _isActive)
            Deactivate();
    }

    /** Cool down timer */
    private IEnumerator Cooldown()
    {
        _isCooled = false;
        yield return new WaitForSeconds(_coolDown);
        _isCooled = true;
    }

    /** Delay before actually moving player position */
    private IEnumerator DelayedTeleport(Vector3 tempTeleportPosition)
    {
        StartCoroutine(_mainCam.TeleportTransition());
        yield return new WaitForSeconds(0.2f);
        transform.SetPositionAndRotation(new Vector3(tempTeleportPosition.x, tempTeleportPosition.y + 1.0f /* adding 1 to counteract the new player prefab origin point */ /* Limiting teleport in y: transform.position.y */ , tempTeleportPosition.z), transform.rotation);
        _charBehaviourScript.enabled = true;
    }

    /** Cool down to make sure player doesn't speak too often */
    private IEnumerator DialogueCooldown()
    {
        yield return new WaitForSeconds(3);
        _shouldSpeak = true;
    }
}