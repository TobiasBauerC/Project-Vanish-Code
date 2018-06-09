using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : Singleton<EnemyManager>
{
    // player reference
    private CharacterBehaviour _player;

    // List of all enemies in the game
    private List<Enemy> _enemies = new List<Enemy>();
    // Add elements to the _enemies List
    public void Add(Enemy enemy)
    {
        // Should only be true when reloading a scene
        if (_enemies.Contains(null))
            _enemies.Clear();
        _enemies.Add(enemy);
    }

    private List<SearchNode> _visitedSearchNodes = new List<SearchNode>();
    public void Add(SearchNode searchNode)
    {
        _visitedSearchNodes.Add(searchNode);
    }

    // Ref to looping enemy check
    [SerializeField] private float _checkEnemiesLoopWaitTime = 1f;
    private Coroutine _checkEnemiesCoroutine;
    private WaitForSeconds _checkEnemiesWait;

    [SerializeField]
    [Tooltip("Color for Idle, Patrol and Stunned")]
    private Color _normalColor;
    public Color normalColor
    {
        get { return _normalColor; }
    }

    [SerializeField]
    [Tooltip("Color for Chase and Attack")]
    private Color _engagedColor;
    public Color engagedColor
    {
        get { return _engagedColor; }
    }

    [SerializeField]
    [Tooltip("Color for Idle, Patrol and Stunned")]
    private Color _searchColor;
    public Color searchColor
    {
        get { return _searchColor; }
    }

    public bool enemiesEngaged
    {
        get { return _enemies.Find(enemy => enemy.currentState.name == State.Name.Attack || enemy.currentState.name == State.Name.Chase); }
    }

    public bool enemiesSearching
    {
        get { return _enemies.Find(enemy => enemy.currentState.name == State.Name.Search); }
    }

    void Start()
    {
        Elevator.playerTookElevator += SecureEnvironment;
        Doors.playerCrossedDoor += SecureEnvironment;
        Room.playerEnteredRoom += ManageActiveGuards;

        _checkEnemiesWait = new WaitForSeconds(_checkEnemiesLoopWaitTime);
    }

    void Update()
    {
        if (!GameManager.instance.gameOver && !_player) { _player = GameManager.instance.player.GetComponent<CharacterBehaviour>(); }
    }

    public void InitAll()
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            _enemies[i].Init();
        }
        _checkEnemiesCoroutine = StartCoroutine(CheckEnemies());
    }

    // For guards to check the elevator/door they are assigned to
    public void SecureEnvironment(InteractableEnvironment environment)
    {
        Enemy[] guards = FindGuardsFor(environment);
        foreach (Enemy guard in guards)
        {
            guard.SecureEnvironment();
        }
    }

    // gets all guards assigned to an elevator/door
    private Enemy[] FindGuardsFor(InteractableEnvironment environment)
    {
        return _enemies.FindAll(enemy => enemy.securePoint == environment && enemy.currentRoom == _player.currentRoom && enemy.currentFloor == _player.currentFloor).ToArray();
    }

    // finds all enemies in a state, room, floor, and for an elevator/door
    public Enemy[] FindEnemies(State.Name[] states, Room.Names room, int floor, InteractableEnvironment environment)
    {
        // if there is only one element in the states array, run through everything once and return
        if (states.Length == 1)
        {
            State.Name state = states[0];
            return _enemies.FindAll(enemy => enemy.stateManager.currentState.name == state && enemy.currentRoom == room && enemy.currentFloor == floor && enemy.securePoint == environment).ToArray();
        }

        // makea  list of enemies and add to it as you find applicable enemies
        List<Enemy> relevantEnemies = new List<Enemy>();
        for (int i = 0; i < states.Length; i++)
        {
            State.Name state = states[i];
            for (int j = 0; j < _enemies.Count; j++)
            {
                Enemy enemy = _enemies[j];
                if (enemy.stateManager.currentState.name == state && enemy.currentRoom == room && enemy.currentFloor == floor && enemy.securePoint == environment)
                    relevantEnemies.Add(enemy);
            }
        }
        return relevantEnemies.ToArray();
    }

    public Enemy[] FindEnemies(State.Name[] states, Room.Names room, int floor)
    {
        // if there is only one element in the states array, run through everything once and return
        if (states.Length == 1)
        {
            State.Name state = states[0];
            return _enemies.FindAll(enemy => enemy.stateManager.currentState.name == state && enemy.currentRoom == room && enemy.currentFloor == floor).ToArray();
        }

        // makea  list of enemies and add to it as you find applicable enemies
        List<Enemy> relevantEnemies = new List<Enemy>();
        for (int i = 0; i < states.Length; i++)
        {
            State.Name state = states[i];
            for (int j = 0; j < _enemies.Count; j++)
            {
                Enemy enemy = _enemies[j];
                if (enemy.stateManager.currentState.name == state && enemy.currentRoom == room && enemy.currentFloor == floor)
                    relevantEnemies.Add(enemy);
            }
        }
        return relevantEnemies.ToArray();
    }

    // grabs the closest valid search node for an enemy
    public SearchNode GetMyNextNode(Enemy enemy, Room.Names room, int floor, InteractableEnvironment environment)
    {
        SearchNode closestNode = null;
        float closestDist = Mathf.Infinity;

        if (SearchNode.allSearchNodes == null || SearchNode.allSearchNodes.Length == 0 || SearchNode.allSearchNodes.Contains(null))
        {
            SearchNode.FindAllSearchNodes();
            return GetMyNextNode(enemy, room, floor, environment);
        }

        for (int i = 0; i < SearchNode.allSearchNodes.Length; i++)
        {
            if (SearchNode.allSearchNodes[i] == null)
            {
                continue;
            }
            if (SearchNode.allSearchNodes[i].roomName == room && SearchNode.allSearchNodes[i].floor == floor && SearchNode.allSearchNodes[i].environment == environment && SearchNode.allSearchNodes[i].IsSearchable())
            {
                float distToNode = Vector3.Distance(enemy.transform.position, SearchNode.allSearchNodes[i].transform.position);
                if (distToNode < closestDist)
                {
                    closestNode = SearchNode.allSearchNodes[i];
                    closestDist = distToNode;
                }
            }
        }

        return closestNode;
    }

    // sets all search nodes back to an unsearched mode
    public void ResetSearchNodes(Room.Names room, int floor, InteractableEnvironment environment)
    {
        SearchNode[] nodesToReset = SearchNode.allSearchNodes.Where(searchNode => searchNode.roomName == room && searchNode.floor == floor && searchNode.environment == environment && !searchNode.IsSearchable()).ToArray();
        for (int i = 0; i < nodesToReset.Length; i++)
        {
            nodesToReset[i].HardReset();
        }
        _visitedSearchNodes.Clear();
    }

    // sets all search nodes back to an unsearched mode
    public void ResetSearchNodes(Room.Names room, int floor)
    {
        SearchNode[] nodesToReset = _visitedSearchNodes.FindAll(searchNode => searchNode.roomName == room && searchNode.floor == floor).ToArray();
        for (int i = 0; i < nodesToReset.Length; i++)
        {
            nodesToReset[i].SoftReset();
            if (nodesToReset[i].IsSearchable())
                _visitedSearchNodes.Remove(nodesToReset[i]);
        }
    }

    // when the player is found, find all nearby enemies and tell them the player is found
    public void HelpChase(Enemy enemy)
    {
        // get all guards in the same room and same floor as the given enemy and that are not in an attack chase or stunned state 
        Enemy[] guardsInMyRoom = _enemies.FindAll(guard =>
            guard != enemy &&
            guard.currentRoom == enemy.currentRoom &&
            guard.currentFloor == enemy.currentFloor &&
            guard.currentState.name != State.Name.Attack &&
            guard.currentState.name != State.Name.Chase &&
            guard.currentState.name != State.Name.Stunned).ToArray();

        // tell ever guard with a direct line to the player that they are now detecting and chasing the player
        LayerMask mask = ~LayerMask.GetMask(GameManager.Layers.Actors.ToString());
        foreach (Enemy guard in guardsInMyRoom)
        {
            if (!Physics.Linecast(guard.transform.position, _player.headPosition, mask, QueryTriggerInteraction.Ignore))
            {
                guard.PlayerDetected(false);
            }
        }
    }

    /** when no one knows where the player is, the whole floor searches */
    public void HelpSearch(Enemy enemy)
    {
        ResetSearchNodes(enemy.currentRoom, enemy.currentFloor);

        /** get all guards in the same room and same floor as the given enemy and that are not in an attack, chase, stunned, or search state */
        Enemy[] guardsInMyRoom = _enemies.FindAll(guard =>
            guard != enemy &&
            guard.currentRoom == enemy.currentRoom &&
            guard.currentFloor == enemy.currentFloor &&
            guard.currentState.name != State.Name.Attack &&
            guard.currentState.name != State.Name.Chase &&
            guard.currentState.name != State.Name.Stunned &&
            guard.currentState.name != State.Name.Search).ToArray();

        foreach (Enemy guard in guardsInMyRoom)
        {
            guard.PlayerLost(guard.transform.position);
        }
    }

    // loop that checks enemies are not broken/stuck
    private IEnumerator CheckEnemies()
    {
        while (true)
        {
            yield return _checkEnemiesWait;
            CheckChaseAttackEnemies();
        }
    }

    // this will determin if the chase/attack enemies are stuck in their state
    private void CheckChaseAttackEnemies()
    {
        Enemy[] chaseAttackEnemies = _enemies.FindAll(enemy => enemy.stateManager.currentState.name == State.Name.Chase || enemy.stateManager.currentState.name == State.Name.Attack).ToArray();
        foreach (Enemy enemy in chaseAttackEnemies)
        {
            // check to see if the enemy is stuck under and elevator or on an edge
            Ray ray = new Ray(enemy.transform.position, Vector3.up);
            RaycastHit hit;
            if ((Physics.Raycast(ray, out hit) && hit.transform.CompareTag(GameManager.Tags.Elevator.ToString())) || (enemy.navMeshAgent.velocity.magnitude < 0.1f && enemy.chaseElapseTime > 1f))
            {
                enemy.PlayerLost(enemy.transform.position);
            }

            float yDifference = enemy.transform.position.y - _player.transform.position.y;
            // if the player has left the room, everyone who was chasing or attacking the player goes into search
            if ((enemy.currentRoom == _player.previousRoom && _player.currentRoom != _player.previousRoom) || (enemy.currentRoom == _player.currentRoom && enemy.currentFloor == _player.previousFloor && yDifference > 0.5f))
            {
                enemy.PlayerLost(enemy.transform.position);
            }
        }
    }

    private void ManageActiveGuards(Room playerRoom)
    {
        StartCoroutine(ActivateAndDeactivateGuards(playerRoom));
    }

    private IEnumerator ActivateAndDeactivateGuards(Room playerRoom)
    {
        Enemy[] enemiesToActivate = _enemies.FindAll(enemy => enemy.currentRoom == playerRoom.roomName ||
            playerRoom.connectedRooms.Contains(enemy.currentRoom)).ToArray();

        Enemy[] enemiesToDeactivate = _enemies.Except(enemiesToActivate).ToArray();

        int biggerArraySize = Mathf.Max(enemiesToActivate.Length, enemiesToDeactivate.Length);

        int counter = 0;
        do
        {
            if (counter < enemiesToActivate.Length)
            {
                if (!enemiesToActivate[counter].gameObject.activeSelf)
                {
                    enemiesToActivate[counter].gameObject.SetActive(true);
                }
            }

            if (counter < enemiesToDeactivate.Length)
            {
                if (enemiesToDeactivate[counter].gameObject.activeSelf)
                {
                    enemiesToDeactivate[counter].gameObject.SetActive(false);
                }
            }

            counter++;
            yield return null;

        } while (counter < biggerArraySize);
    }

    public bool GetPermissionToSpeak()
    {
        // return true only if no other enemies are speaking
        return !_enemies.Find(enemy => enemy.isSpeaking);
    }

    protected override void AdditionalDestroyTasks()
    {
        _enemies.Clear();
        Elevator.playerTookElevator -= SecureEnvironment;
        Doors.playerCrossedDoor -= SecureEnvironment;
        Room.playerEnteredRoom -= ManageActiveGuards;
    }
}