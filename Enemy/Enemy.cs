using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AIPatrol))]
[RequireComponent(typeof(AIChase))]
[RequireComponent(typeof(AIStunned))]
[RequireComponent(typeof(AIController))]
[RequireComponent(typeof(AIFieldOfView))]
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : Actor
{
    #region delegates
    public delegate void EnemyHitDelegate();
    public event EnemyHitDelegate enemyHit;

    public delegate void RecoverDelegate();
    public event RecoverDelegate recover;

    public delegate void Idle();
    public event Idle idle;

    public delegate void PlayerDetectedDelegate();
    public event PlayerDetectedDelegate playerDetected;

    public delegate void PlayerLostDelegate();
    public event PlayerLostDelegate playerLost;

    public delegate void FinishedSearching();
    public event FinishedSearching finishedSearching;

    public delegate void StartedSearching();
    public event StartedSearching startedSearching;

    public delegate void PlayerReachable();
    public event PlayerReachable playerReachable;

    public delegate void PlayerNotReachable();
    public event PlayerNotReachable playerNotReachable;
    #endregion

    private StateManager _stateManager = new StateManager();
    public StateManager stateManager
    {
        get { return _stateManager; }
    }

    public State currentState
    {
        get { return _stateManager.currentState; }
    }

    [SerializeField] private int _voiceNum;
    public int voiceNum
    {
        get { return _voiceNum; }
    }

    [SerializeField] private AudioSource _dialogueSource;
    public AudioSource dialogueSource
    {
        get { return _dialogueSource; }
    }

    [SerializeField] private AudioSource _sfxSource;
    public AudioSource sfxSource
    {
        get { return _sfxSource; }
    }
    private bool _shouldSpeak = true;

    [SerializeField] private Transform _target;
    public Transform target
    {
        get { return _target; }
    }

    [SerializeField] private Vector3 _lastKnownPlayerPosition;
    public Vector3 lastKnownPlayerPosition
    {
        get { return _lastKnownPlayerPosition; }
    }

    [SerializeField] private float _targetLocationTolerance;
    public float targetLocationTolerance
    {
        get { return _targetLocationTolerance; }
    }

    [SerializeField] private float _engageDistance;
    public float engageDistance
    {
        get { return _engageDistance; }
    }

    [SerializeField] private float _disengageDistance;
    public float disengageDistance
    {
        get { return _disengageDistance; }
    }

    [SerializeField] private float _minDistToNode = 0.0f;
    public float minDistToNode
    {
        get { return _minDistToNode; }
    }

    [SerializeField] private NavMeshAgent _navMeshAgent;
    public NavMeshAgent navMeshAgent
    {
        get { return _navMeshAgent; }
    }

    [SerializeField] private InteractableEnvironment _securePoint;
    public InteractableEnvironment securePoint
    {
        get { return _securePoint; }
    }

    [Header("Capsule Values")]
    [SerializeField] private CapsuleCollider _capsuleCollider;

    [SerializeField] private Vector3 _capsuleRegCenter;
    public Vector3 CapsuleRegCenter
    {
        get { return _capsuleRegCenter; }
    }

    [SerializeField] private Vector3 _capsuleStunnedCenter;
    public Vector3 CapsuleStunnedCenter
    {
        get { return _capsuleStunnedCenter; }
    }

    [SerializeField] int[] _capsuleDirection;

    public float chaseElapseTime
    {
        get { return _aiChase.elapsedTime; }
    }

    private bool _callForHelp = true;
    public bool callForHelp
    {
        get { return _callForHelp; }
        set { _callForHelp = value; }
    }

    [Header("Weapons From Hand Transform")]
    [SerializeField] private GameObject _fazer;
    [SerializeField] private GameObject _baton;

    [Header("Component References")]
    [SerializeField]
    private AIIdle _aiIdle;
    [SerializeField] private AIPatrol _aiPatrol;
    [SerializeField] private AIStunned _aiStunned;
    [SerializeField] private AIChase _aiChase;
    [SerializeField] private AIAttack _aiAttack;
    [SerializeField] private AISearch _aiSearch;
    [SerializeField] private AIFieldOfView _aiFOV;
    [SerializeField] private LayerMask _checkFacingMask;
    [SerializeField] private Animator _anim;

    [SerializeField] private Renderer[] _xRayAffectedMeshes;
    public Renderer[] xRayAffectedMeshes
    {
        get { return _xRayAffectedMeshes; }
    }

    [Header("Debug")]
    [SerializeField]
    private bool _debug;
    public bool debug
    {
        get { return _debug; }
    }

    [SerializeField] private bool _debugAll;

    [Header("Check Path Validity")]
    [SerializeField]
    private float _checkPathTimer = 0f;
    private WaitForSeconds _waitForSeconds;

    public float idleTime { get; set; }
    private bool _postPlayerDeath = false;

    private bool _ready = false;

    public bool isSpeaking
    {
        get { return _dialogueSource ? _dialogueSource.isPlaying : false; }
    }

    private enum AnimationParameters { Speed, Idle, Patrol, Chase, Attack, Stunned, Search, StunnedState, IdleState, AttackState }

    void Start()
    {
        _ready = false;
        EnemyManager.instance.Add(this);
        _anim.SetBool("Range", _aiAttack.attackMode == AIAttack.AttackMode.Ranged);

        if (_aiAttack.attackMode == AIAttack.AttackMode.Ranged)
        {
            _baton.SetActive(false);
        }
        else
        {
            _fazer.SetActive(false);
        }
    }

    public void Init()
    {
        InitializeFSM();
        idleTime = -1.0f; // default idle time to invalid.
        _postPlayerDeath = false;
        _waitForSeconds = new WaitForSeconds(_checkPathTimer);
        _currentFloor = Room.DetermineActorFloor(this);
        StartCoroutine(CheckPathToPlayer());
        if (_voiceNum == -1)
            _voiceNum = Mathf.FloorToInt(Random.Range(0, 5));

        Room.playerEnteredRoom += DetermineScanningForPlayer;
        Room.playerExitedRoom += DetermineScanningForPlayer;

        _ready = true;
    }

    private void InitializeFSM()
    {
        stateManager.Add(State.Name.Idle);
        stateManager.Add(State.Name.Patrol);
        stateManager.Add(State.Name.Chase);
        stateManager.Add(State.Name.Attack);
        stateManager.Add(State.Name.Stunned);
        stateManager.Add(State.Name.Search);

        stateManager.Add(new ChaseToAttack());
        //stateManager.Add(new ChaseToIdle());      // should not exist
        stateManager.Add(new ChaseToStunned());
        stateManager.Add(new ChaseToSearch());

        //stateManager.Add(new AttackToIdle());     // should not exist
        stateManager.Add(new AttackToStunned());
        stateManager.Add(new AttackToChase());
        stateManager.Add(new AttackToSearch());

        stateManager.Add(new IdleToChase());
        stateManager.Add(new IdleToPatrol()); // TODO make functionality where this becomes a thing again
        stateManager.Add(new IdleToStunned());
        stateManager.Add(new IdleToSearch());

        stateManager.Add(new PatrolToChase());
        stateManager.Add(new PatrolToIdle());
        stateManager.Add(new PatrolToStunned());
        stateManager.Add(new PatrolToSearch());

        //stateManager.Add(new StunnedToIdle());    // should not exist
        stateManager.Add(new SearchToChase());
        stateManager.Add(new SearchToPatrol());
        stateManager.Add(new SearchToStunned());

        stateManager.Add(new StunnedToSearch());
        stateManager.Add(new StunnedToChase());

        stateManager.Add(Condition.Name.EnemyHit);
        stateManager.Add(Condition.Name.PlayerDetected);
        stateManager.Add(Condition.Name.EnemyRecover);
        stateManager.Add(Condition.Name.FinishedSearching);
        stateManager.Add(Condition.Name.PlayerReachable);

        stateManager.Init(this);

        _stateManager.SetCurrentState(State.Name.Patrol);
    }

    private void Update()
    {
        if (!_ready)
            return;

        if (!_target && GameManager.instance)
        {
            GameObject player = GameManager.instance.player;
            if (player)
            {
                _target = player.transform;
            }
        }

        if (_debug || _debugAll)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                Supporting.Log(string.Format("Enemy is in state {0}", currentState), 2);
                Debug.Break();
            }
        }

        _anim.SetFloat(AnimationParameters.Speed.ToString(), _navMeshAgent.velocity.magnitude * 0.2f);
        stateManager.Update();

        if (GameManager.instance.gameOver)
            ShutDown();
    }

    public void SetVisualIndicator()
    {
        Color colorToUse;

        switch (currentState.name)
        {
            case State.Name.Search:
                colorToUse = EnemyManager.instance.searchColor;
                break;
            case State.Name.Attack:
            case State.Name.Chase:
                colorToUse = EnemyManager.instance.engagedColor;
                break;
            case State.Name.Patrol:
            case State.Name.Idle:
            case State.Name.Stunned:
            default:
                colorToUse = EnemyManager.instance.normalColor;
                break;
        }

        foreach (Renderer renderer in _xRayAffectedMeshes)
        {
            renderer.material.SetColor("_CEmessiveBlue", colorToUse);
        }
    }

    #region IDLE
    public void IdleOnEnter()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is entering Idle State", gameObject.name), gameObject); }

        _anim.SetTrigger(AnimationParameters.Idle.ToString());
        _anim.SetBool(AnimationParameters.IdleState.ToString(), true);
        _aiIdle.EnterIdle();
    }

    public void IdleOnUpdate()
    {
        if (_debugAll) { Supporting.Log(string.Format("{0} is in Idle State", gameObject.name), gameObject); }
        _aiIdle.UpdateIdle();
    }

    public void IdleOnExit()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is exiting Idle State", gameObject.name), gameObject); }
        _anim.SetBool(AnimationParameters.IdleState.ToString(), false);
        _aiIdle.ExitIdle();
    }
    #endregion

    #region PATROL
    public void PatrolOnEnter()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is entering Patrol State", gameObject.name), gameObject); }

        _anim.SetTrigger(AnimationParameters.Patrol.ToString());

        _aiPatrol.EnterPatrol();
    }

    public void PatrolOnUpdate()
    {
        if (_debugAll) { Supporting.Log(string.Format("{0} is in Patrol State", gameObject.name), gameObject); }

        _aiPatrol.UpdatePatrol();
    }

    public void PatrolOnExit()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is exiting Patrol State", gameObject.name), gameObject); }

        _aiPatrol.ExitPatrol();
    }
    #endregion

    #region CHASE
    public void ChaseOnEnter()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is entering Chase State", gameObject.name), gameObject); }

        _anim.SetTrigger(AnimationParameters.Chase.ToString());

        _aiChase.EnterChase();
        _aiFOV.On();
    }

    public void ChaseOnUpdate()
    {
        if (_debugAll) { Supporting.Log(string.Format("{0} is in Chase State", gameObject.name), gameObject); }

        _aiChase.UpdateChase();
    }

    public void ChaseOnExit()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is exiting Chase State", gameObject.name), gameObject); }

        _aiChase.ExitChase();
    }
    #endregion

    #region ATTACK
    public void AttackOnEnter()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is entering Attack State", gameObject.name), gameObject); }

        _anim.SetTrigger(AnimationParameters.Attack.ToString());
        _anim.SetBool(AnimationParameters.AttackState.ToString(), true);
        _aiAttack.EnterAttack();
    }

    public void AttackOnUpdate()
    {
        if (_debugAll) { Supporting.Log(string.Format("{0} is in Attack State", gameObject.name), gameObject); }

        _aiAttack.UpdateAttack();
    }

    public void AttackOnExit()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is exiting Attack State", gameObject.name), gameObject); }
        _anim.SetBool(AnimationParameters.AttackState.ToString(), false);
        _aiAttack.ExitAttack();
    }
    #endregion

    #region STUNNED
    public void StunnedOnEnter()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is entering Stunned State", gameObject.name), gameObject); }

        _anim.SetTrigger(AnimationParameters.Stunned.ToString());
        _anim.SetBool(AnimationParameters.StunnedState.ToString(), true);
        _aiFOV.Off();

        _capsuleCollider.center = _capsuleStunnedCenter;
        _capsuleCollider.direction = _capsuleDirection[0];

        // play stunned dialogue
        if (EnemyManager.instance.GetPermissionToSpeak())
        {
            SoundManager.instance.PlayGuardDialogue(_dialogueSource, SoundManager.DialogueType.Stunned, _voiceNum, ref SoundManager.instance.guardStunnedDialogueArrayIndex, true);
        }
    }

    public void StunnedOnUpdate()
    {
        if (_debugAll) { Supporting.Log(string.Format("{0} is in Stunned State", gameObject.name), gameObject); }

        _aiStunned.UpdateStun();
    }

    public void StunnedOnExit()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is exiting Stunned State", gameObject.name), gameObject); }

        _anim.SetBool(AnimationParameters.StunnedState.ToString(), false);
        _aiStunned.ExitStun();

        _capsuleCollider.center = _capsuleRegCenter;
        _capsuleCollider.direction = _capsuleDirection[1];

        // play recover dialogue
        if (EnemyManager.instance.GetPermissionToSpeak())
        {
            SoundManager.instance.PlayGuardDialogue(_dialogueSource, SoundManager.DialogueType.Recovering, _voiceNum, ref SoundManager.instance.guardRecoverDialogueArrayIndex, true);
        }
    }
    #endregion

    #region SEARCH
    public void SearchOnEnter()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is entering Search State", gameObject.name), gameObject); }

        _anim.SetTrigger(AnimationParameters.Search.ToString());

        // TODO Remove this functionality from search state

        // play search Dialogue
        if (EnemyManager.instance.GetPermissionToSpeak())
        {
            SoundManager.instance.PlayGuardDialogue(_dialogueSource, SoundManager.DialogueType.Alerted, _voiceNum, ref SoundManager.instance.guardAlertedDialogueArrayIndex, false);
        }
        _aiSearch.EnterSearch(_aiFOV.seenPlayer);
        startedSearching();
        _aiFOV.On();
    }

    public void SearchOnUpdate()
    {
        if (_debugAll) { Supporting.Log(string.Format("{0} is in Search State", gameObject.name), gameObject); }

        _aiSearch.UpdateSearch();
    }

    public void SearchOnExit()
    {
        if (_debug || _debugAll) { Supporting.Log(string.Format("{0} is exiting Search State", gameObject.name), gameObject); }

        _aiSearch.ExitSearch();
    }
    #endregion

    public void TakeHit(float stunTime, bool rangedAttack = false)
    {
        if (_debugAll) { Supporting.Log(string.Format("{0} was hit", gameObject.name), gameObject); }

        enemyHit();

        _aiStunned.EnterStun(stunTime, rangedAttack);
    }

    public void Recover()
    {
        if (_debugAll) { Supporting.Log(string.Format("{0} recovered", gameObject.name), gameObject); }

        recover();
    }

    // [THISSHIT] [TH] - QUESTION - WTF?
    public void FinishRecover()
    {
        if (_debugAll) { Supporting.Log(string.Format("{0} finished recovered", gameObject.name), gameObject); }

        idle();
    }

    public void PlayerDetected(bool tellOthers)
    {
        if (_debug) { Supporting.Log(string.Format("{0} found the Player", gameObject.name), gameObject); }

        playerDetected();
        if (tellOthers)
            EnemyManager.instance.HelpChase(this);

        if (_shouldSpeak)
        {
            // playing alerted dialogue sound
            _shouldSpeak = false;
            if (EnemyManager.instance.GetPermissionToSpeak())
            {
                SoundManager.instance.PlayGuardDialogue(_dialogueSource, SoundManager.DialogueType.Chasing, _voiceNum, ref SoundManager.instance.guardChasingDialogueArrayIndex, true);

            }
            StartCoroutine(DialogueCooldown());
        }
    }

    public void PlayerLost(Vector3 position)
    {
        if (_debug) { Supporting.Log(string.Format("{0} lost the Player", gameObject.name), gameObject); }

        _lastKnownPlayerPosition = position;

        playerLost();
    }

    public void PlayerLost()
    {
        PlayerLost(target.position);
    }

    public void FinishedSearhing()
    {
        finishedSearching();
    }

    public void ResetLastPlayerPosition()
    {
        _lastKnownPlayerPosition = new Vector3();
    }

    public bool IsFacingPlayer()
    {
        RaycastHit hitInfo;

        // Raycast forwards in Actors/Environment layers
        if (Physics.Raycast(this.transform.position, this.transform.forward, out hitInfo, engageDistance, _checkFacingMask))
        {
            Debug.DrawLine(this.transform.position + Vector3.up * 0.25f, hitInfo.point + Vector3.up * 0.25f, Color.blue);
            // If something is hit, check if it is the player
            if (hitInfo.transform.gameObject == GameManager.instance.player)
            {
                return true;
            }
        }

        return false;
    }

    public void RotateTowards(Vector3 target, float rotateSpeed)
    {
        Vector3 direction = (target - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotateSpeed);
    }

    public bool ShouldDisengage()
    {
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > _disengageDistance)
            return true;
        return false;
    }

    public bool ShouldAttack()
    {
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance < _disengageDistance)
            return true;
        return false;
    }

    public bool ReadyToIdle()
    {
        if (idleTime >= 0)
            return true;
        return false;
    }

    public bool SeenPlayer()
    {
        return _aiFOV.seenPlayer;
    }

    // TODO find a way to avoid doing this every update loop
    // Returns true if agent is within 1 unit of player's last known position, false otherwise
    // Used for transitions: ChaseToSearch, AttackToChase
    public bool LastKnownPlayerPositionReached()
    {
        if (_lastKnownPlayerPosition != Vector3.zero && Vector3.Distance(transform.position, _lastKnownPlayerPosition) <= 1f)
        {
            return true;
        }

        return false;
    }

    public bool UnreachableHeight()
    {
        return _aiChase.unreachable;
    }

    // Returns true if a path from object's position to destination is a valid nav mesh path, false otherwise
    public bool CheckPathValidity(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();

        bool temp = navMeshAgent.CalculatePath(destination, path);

        //if (temp && _debug)
        //Debug.LogFormat("Path: {0}, Dest: {1}", path.corners[path.corners.Length - 1], destination);
        //Debug.Log(Vector3.Distance(path.corners[path.corners.Length - 1], destination) < 1f);

        return temp;
    }

    public void ShutDown()
    {
        if (!GameManager.instance.gameOver || _postPlayerDeath)
            return;
        _postPlayerDeath = true;
        _stateManager.SetCurrentState(State.Name.Patrol);

        Room.playerEnteredRoom -= DetermineScanningForPlayer;
        Room.playerExitedRoom -= DetermineScanningForPlayer;

        _aiFOV.Off();
    }

    private void DetermineScanningForPlayer(Room room)
    {
        if (room.roomName == _currentRoom)
        {
            _aiFOV.On();
        }
        else
        {
            _aiFOV.Off();
        }
    }

    public void SecureEnvironment()
    {
        if (!_securePoint)
            return;
        _callForHelp = false;
        PlayerLost(transform.position);
    }

    private IEnumerator CheckPathToPlayer()
    {
        while (true)
        {
            if (_stateManager.currentState.name != State.Name.Stunned && _target)
            {
                if (CheckPathValidity(target.position))
                {
                    playerReachable();
                }
                else
                {
                    playerNotReachable();
                }
            }

            yield return _waitForSeconds;
        }
    }

    // Override functions for Actor class
    public override void UpdateRoom(Room.Names oldRoom, Room.Names newRoom)
    {
        _previousRoom = oldRoom;
        _currentRoom = newRoom;
    }

    protected override void UpdateFloor()
    {
        _previousFloor = -1;
        _currentFloor = Room.DetermineActorFloor(this);
    }

    private IEnumerator DialogueCooldown()
    {
        yield return new WaitForSeconds(10);
        _shouldSpeak = true;
    }
}