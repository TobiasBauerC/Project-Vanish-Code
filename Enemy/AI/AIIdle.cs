using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIdle : MonoBehaviour
{
    [SerializeField] private Enemy _enemy;

    private float _elapseTime = 0.0f;
    private float _targetElapseTime = 0.0f;
    private Vector3 _desiredAnlge = Vector3.zero;

    // dialogue stuff
    private bool _shouldSpeak = true;
    private bool _firstEntry = true;

    void Start()
    {
        // get Enemy reference if there is none, set the direction AI should face
        if (_enemy == null)
            _enemy = GetComponent<Enemy>();
        _desiredAnlge = transform.eulerAngles;
    }

    public void EnterIdle() // OnEnter()
    {
        _enemy.ResetLastPlayerPosition();

        // reset elaspe time, set the target time to the idle time, stop navmesh movement
        _elapseTime = 0.0f;
        _targetElapseTime = _enemy.idleTime;
        _enemy.navMeshAgent.speed = 0.0f;
    }

    public void UpdateIdle() // Update()
    {
        // idle dialogue triggers
        if (_shouldSpeak)
        {
            if (Vector3.Distance(_enemy.target.position, gameObject.transform.position) < 51)
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

        // check to see if this is a Idle only guard. If yes, return.
        if (_targetElapseTime == 0.0f)
        {
            // check to see if AI is facing where it should
            if (transform.eulerAngles != _desiredAnlge)
            {
                // lerp the y rotation
                Vector3 newAngle = new Vector3(
                    transform.eulerAngles.x,
                    Mathf.LerpAngle(transform.eulerAngles.y, _desiredAnlge.y, Time.deltaTime),
                    transform.eulerAngles.z);

                transform.eulerAngles = newAngle;
            }
            return;
        }

        // check to see if the AI is done idling
        _elapseTime += Time.deltaTime;
        if (_elapseTime >= _targetElapseTime)
        {
            _enemy.idleTime = -1.0f;
        }

    }

    public void ExitIdle()
    {
        // set Enemy.idleTime to invalid so it wont go straight back into idle
        _enemy.idleTime = -1.0f;
    }

    private IEnumerator DialogueCooldown()
    {
        int randomTime = Random.Range(7, 30);
        yield return new WaitForSeconds(randomTime);
        _shouldSpeak = true;
    }

}