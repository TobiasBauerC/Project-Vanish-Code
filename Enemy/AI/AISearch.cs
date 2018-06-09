using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AIController))]
[RequireComponent(typeof(NavMeshAgent))]

public class AISearch : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The maximum speed the agent has while in Search State.")]
    private float _searchSpeed = 0f;

    private Enemy _enemy = null;
    private float _searchTimeElapsed = 0f;
    private SearchNode _currentSearchNode;

    private void Start()
    {
        if (!_enemy)
        {
            _enemy = GetComponent<Enemy>();
        }

        // Default values (If no value was put into the inspector)
        if (_searchSpeed == 0f)
        {
            _searchSpeed = 5f;
        }
    }

    public void EnterSearch(bool playerSeen = false)
    {
        _enemy.ResetLastPlayerPosition();
        _enemy.navMeshAgent.autoBraking = true;
        _searchTimeElapsed = 0f;
        _enemy.navMeshAgent.speed = _searchSpeed;

        if (EnemyManager.instance.FindEnemies(new State.Name[] { State.Name.Attack, State.Name.Chase }, _enemy.currentRoom, _enemy.currentFloor).Length == 0 && _enemy.callForHelp)
            EnemyManager.instance.HelpSearch(_enemy);

        _enemy.callForHelp = true;
        SetNextNode();
    }

    public void UpdateSearch()
    {
        if (CheckDestinationReached())
        {
            SetNextNode();
        }
    }

    public void ExitSearch()
    {
        if (EnemyManager.instance.FindEnemies(new State.Name[] { _enemy.stateManager.currentState.name }, _enemy.currentRoom, _enemy.currentFloor, _enemy.securePoint).Length == 1)
            EnemyManager.instance.ResetSearchNodes(_enemy.currentRoom, _enemy.currentFloor, _enemy.securePoint);
    }

    // Returns true if distance to destination is less than 1 unit away, false otherwise
    private bool CheckDestinationReached()
    {
        if (_enemy.navMeshAgent.destination == null /*!_enemy.navMeshAgent.hasPath*/ )
        {
            //Debug.Log("[AI][Search State] No current destination. Searching nearby.");
            return true;
        }

        float dist = Vector3.Distance(this.transform.position, _enemy.navMeshAgent.destination);
        if (dist <= 1.1f)
        {
            //Debug.Log("[AI][Search State] Destination reached. Searching nearby.");
            return true;
        }

        return false;
    }

    private void SetNextNode(SearchNode currentNode = null)
    {
        if (_currentSearchNode != null)
            _currentSearchNode.Cleared();
        _currentSearchNode = EnemyManager.instance.GetMyNextNode(_enemy, _enemy.currentRoom, _enemy.currentFloor, _enemy.securePoint);
        if (_currentSearchNode != null)
        {
            _enemy.navMeshAgent.SetDestination(_currentSearchNode.transform.position);
            _currentSearchNode.StartSearch();
            EnemyManager.instance.Add(_currentSearchNode);
        }
        else
            _enemy.FinishedSearhing();
    }
}