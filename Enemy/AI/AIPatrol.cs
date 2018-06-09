using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]

public class AIPatrol : MonoBehaviour
{
    [SerializeField] private float _speed = 0.0f;
    [SerializeField] public List<BaseNode> _path = null;
    [SerializeField] private NavMeshAgent _navMeshAgent = null;
    [SerializeField] private Enemy _enemy = null;

    // dialogue stuff
    private bool _shouldSpeak = true;
    private bool _firstEntry = true;

    public int _pathIndex = 0;

    // Use this for initialization
    void Start()
    {
        if (_path.Count <= 0)
        {
            // TODO FOR BETA - Remove
            Debug.LogErrorFormat("FOR FUCK SAKES GARRETT!!! NO PATH FOR {0}. REMEMBER TO SET ONE", gameObject.name);
            // KillProgram();
        }
        else if (_path.Contains(null))
        {
            // TODO FOR BETA - Remove
            Debug.LogErrorFormat("FOR FUCK SAKES GARRETT!!! THERE IS A NULL VALUE IN AI PATROL PATH FOR {0}", gameObject.name);
            // KillProgram();
        }
        if (_speed <= 0.0f)
        {
            Debug.LogWarningFormat("AI SPEED SET TO {0}", _speed);
        }
        if (!_navMeshAgent)
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
        }
        if (!_enemy)
        {
            _enemy = GetComponent<Enemy>();
        }

        _pathIndex = 0;
        _navMeshAgent.speed = _speed;

    }

    private void OnEnable()
    {
        _navMeshAgent.SetDestination(_path[_pathIndex].transform.position);
    }

    public void EnterPatrol() // OnEnter()
    {
        _enemy.ResetLastPlayerPosition();

        BaseNode closestNode = null;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < _path.Count; ++i)
        {
            float distance = Vector3.Distance(_path[i].transform.position, transform.position);
            if (distance < closestDistance)
            {
                closestNode = _path[i];
                closestDistance = distance;
                _pathIndex = i;
            }
        }

        if (closestNode.GetName() == BaseNode.Type.Idle && _path.Count > 1)
        {
            _pathIndex++;
            if (_pathIndex >= _path.Count)
                _pathIndex = 0;
        }

        _navMeshAgent.speed = _speed;
        _navMeshAgent.SetDestination(_path[_pathIndex].transform.position);
        _navMeshAgent.autoBraking = false;
    }

    public void UpdatePatrol() // Update()
    {
        if (Vector3.Distance(transform.position, _path[_pathIndex].transform.position) <= _enemy.minDistToNode)
        {
            NextPathNode();
        }

        if (_shouldSpeak)
        {
            if (Vector3.Distance(_enemy.target.position, gameObject.transform.position) < 31)
            {
                if (_firstEntry)
                {
                    _shouldSpeak = false;
                    StartCoroutine(DialogueCooldown());
                    _firstEntry = false;
                    return;
                }

                // call dialogue
                if (EnemyManager.instance.GetPermissionToSpeak())
                {
                    SoundManager.instance.PlayGuardDialogue(_enemy.dialogueSource, SoundManager.DialogueType.Patrol, _enemy.voiceNum, ref SoundManager.instance.guardPatrolDialogueArrayIndex, false);
                }

                _shouldSpeak = false;

                // call cooldown
                StartCoroutine(DialogueCooldown());

            }
            else if (!_firstEntry)
            {
                _firstEntry = true;
            }
        }
    }

    public void ExitPatrol() { }

    private void NextPathNode()
    {
        BaseNode oldNode = _path[_pathIndex];
        _pathIndex++;

        if (_pathIndex >= _path.Count)
            _pathIndex = 0;
        if (oldNode == _path[_pathIndex])
            _enemy.idleTime = 0.0f;
        else
        {
            if (oldNode.GetName() == BaseNode.Type.Idle)
            {
                _enemy.idleTime = (oldNode as IdleNode).GetIdleTime();
            }

            _navMeshAgent.SetDestination(_path[_pathIndex].transform.position);
        }
    }

    private IEnumerator DialogueCooldown()
    {
        int randomTime = Random.Range(10, 60);
        yield return new WaitForSeconds(randomTime);
        _shouldSpeak = true;
    }

    private void KillProgram()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}