using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIFieldOfView : MonoBehaviour
{
    [SerializeField] private float _standingViewRadius;
    [Range(0.0f, 360.0f)]
    [SerializeField]
    private float _viewAngle;
    [SerializeField] private float _viewHeight;
    [SerializeField] private float _runningNearDetection = 10.0f;
    [SerializeField] private Enemy _enemy;

    [SerializeField] private LayerMask _targetMask;
    [SerializeField] private LayerMask _obstacleMask;

    private CharacterBehaviour _player;
    private Coroutine _fovLoop = null;

    public float standingViewRadius { get { return _standingViewRadius; } }
    public float viewAngle { get { return _viewAngle; } }
    public float viewHeight { get { return _viewHeight; } }

    // TODO Revise name to avoid confusion and further confution.
    /// <summary>
    /// Determin if the player is currently in the list of viewed objects.
    /// </summary>
    /// <returns>Returns true if the player is in the viewed objects list, false otherwise.</returns>
    public bool seenPlayer
    {
        get
        {
            return _visibleTargets.Contains(_enemy.target);
        }
    }

    //[HideInInspector]
    public List<Transform> _visibleTargets = new List<Transform>();

    private void OnEnable()
    {
        On();
    }

    void Start()
    {
        if (!_enemy)
            _enemy = GetComponent<Enemy>();
        StartCoroutine(GetPlayerRef());
    }

    IEnumerator GetPlayerRef()
    {
        do
        {
            if (_enemy)
            {
                if (_enemy.target)
                {
                    _player = _enemy.target.GetComponent<CharacterBehaviour>();
                }
            }
            yield return null;
        }
        while (!_player);
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            if (_enemy.debug)
            {
                Supporting.Log(string.Format("Scanning for Targets", _enemy.name));
            }
            FindVisibleTargets(_standingViewRadius);
        }
    }

    void FindVisibleTargets(float viewRadius)
    {
        Vector3 bottom = new Vector3(transform.position.x, transform.position.y - _viewHeight, transform.position.z);
        Vector3 top = new Vector3(transform.position.x, transform.position.y + _viewHeight, transform.position.z);
        Collider[] targetsInViewRadius = Physics.OverlapCapsule(bottom, top, viewRadius, _targetMask);

        // find each target that falls withing the angle range
        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform targetTransform = targetsInViewRadius[i].transform;
            if (!targetTransform.GetComponent<CapsuleCollider>())
                continue;
            CapsuleCollider targetCollider = targetTransform.GetComponent<CapsuleCollider>();
            Vector3 target = targetTransform.GetComponent<CapsuleCollider>().bounds.center;
            target.y += targetCollider.height / 2.0f;
            Vector3 dirToTarget = (target - transform.position).normalized;
            CharacterBehaviour player = targetTransform.GetComponent<CharacterBehaviour>();
            float distToTarget = Vector3.Distance(transform.position, target);

            if (Vector3.Angle(transform.forward, dirToTarget) < _viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target);

                Vector3 targetFlatPos = new Vector3(target.x, transform.position.y, target.z);
                float horizontalDstToTarget = Vector3.Distance(transform.position, targetFlatPos);
                float checkHeight = ((horizontalDstToTarget / viewRadius) * _viewHeight) + 1.0f;
                float heightDif = transform.position.y - target.y;

                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, _obstacleMask) &&
                    !_visibleTargets.Contains(_enemy.target) &&
                    (heightDif <= checkHeight || heightDif >= -checkHeight))
                {
                    FoundPlayer(targetTransform);
                }
                else if (_visibleTargets.Contains(_enemy.target) &&
                    Physics.Raycast(transform.position, dirToTarget, dstToTarget, _obstacleMask) ||
                    (heightDif > checkHeight || heightDif < -checkHeight))
                {
                    LostPlayer();
                }
            }
            else if (player && distToTarget <= _runningNearDetection)
            {
                bool playerCrouching = player.IsCrouching;
                bool playerMoving = player.IsMoving;
                bool playerAlreadySeen = _visibleTargets.Contains(_enemy.target);
                if (!playerCrouching && playerMoving && !playerAlreadySeen)
                {
                    FoundPlayer(targetTransform);
                }
            }
            else if (_visibleTargets.Contains(_enemy.target))
            {
                LostPlayer();
            }
        }
    }

    private void FoundPlayer(Transform foundTarget)
    {
        _visibleTargets.Add(foundTarget);
        _enemy.PlayerDetected(true);
    }

    private void LostPlayer()
    {
        _visibleTargets.Clear();
        _enemy.PlayerLost();
    }

    // return the position of the line based on the given angle
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0.0f, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    public Vector3 DirFromAngleVert(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(0.0f, Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    /// <summary>
    /// Starts the field of view check loop.
    /// </summary>
    public void On()
    {
        if (GameManager.instance.gameOver || _fovLoop != null)
            return;
        _visibleTargets.Clear();
        _fovLoop = StartCoroutine(FindTargetsWithDelay(0.2f));
    }

    /// <summary>
    /// Stops the field of view check loop while keeping reference to anything it had prefiously seen.
    /// </summary>
    public void Off()
    {
        if (_fovLoop != null)
        {
            StopCoroutine(_fovLoop);
            _fovLoop = null;

            if (seenPlayer)
            {
                _enemy.PlayerLost();
            }
        }
    }

    private void OnDisable()
    {
        Off();
    }
}