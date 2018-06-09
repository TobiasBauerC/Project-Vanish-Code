using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melee : MonoBehaviour
{
    /*
	Instructions:
	1: Add this script onto the player.
	2: Enemy tag is the string that an enemy would be tagged in.
	3: Stun time is how long a hit enemy will be stunned. Default time is 5 seconds.
	4: Add an empty gameobject to the player. Add a collider that will be your hitbox. Adjest the hitbox size to be however big you want. 
		Set is trigger to true. Add the TakeDown script to the hitbox gameobject. Drag and drop that gameobject into the melee hit box field. 
	5: Finished :)
	*/

    [Header("Definitions")]
    [SerializeField] protected string _enemyTag = "Enemy";
    public static float _stunTime = 12.0f;

    [Header("Drag and Drop Variables")]
    [SerializeField] protected GameObject _meleeHitBox;
    [SerializeField] protected GameObject _meleeHitBoxCrouch;
    [SerializeField] protected CharacterBehaviour _CB; // CharacterBehaviour Script from player
    [SerializeField] protected TeleportScript _TS; // TeleportScript from player

    [HideInInspector] public bool Attacking;

    private float AttackSpeed = 0.71f;
    private float SinceLast;

    private WaitForSeconds _meleeWait = new WaitForSeconds(0.1f);
    private WaitForSeconds _meleeSWait = new WaitForSeconds(0.5f);
    private WaitForSeconds _UntilDoneWait = new WaitForSeconds(0.6f);

    Animator animator;

    private ProjectilePlayer _projectilePlayer;

    void Start()
    {
        animator = GetComponent<Animator>();

        _meleeHitBox.SetActive(false);
        _meleeHitBoxCrouch.SetActive(false);

        _projectilePlayer = GameManager.instance.player.GetComponent<ProjectilePlayer>();
    }

    void Update()
    {
        if(_projectilePlayer.HasProjectile())
        { return; }

        if (GameManager.instance.gameOver)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1) && !Attacking)
        {
            if (Time.time > SinceLast + AttackSpeed)
            {
                    StartCoroutine(Attack());

                    SinceLast = Time.time;
            }
        }
    }

    public IEnumerator Attack()
    {
        if (_CB.IsCrouching && !_TS.isActive)
        {
            Attacking = true;
            animator.SetTrigger("Melee");
            yield return _meleeSWait;
            yield return _meleeWait;
        }
        else if (!_CB.IsCrouching && !_TS.isActive)
        {
            Attacking = true;
            animator.SetTrigger("MeleeS");
            yield return _meleeSWait;
            yield return _meleeWait;
        }
        yield return _UntilDoneWait;
        Attacking = false;
    }

    protected void ActivateHitBox()
    {
        _meleeHitBox.SetActive(true);
        SoundManager.instance.PlaySound(SoundManager.instance.meleeAttack, SoundManager.instance.SoundSource, true);
    }

    protected void DeactivateHitBox()
    {
        _meleeHitBox.SetActive(false);
    }

    protected void ActivateHitBoxCrouch()
    {
        _meleeHitBoxCrouch.SetActive(true);
        SoundManager.instance.PlaySound(SoundManager.instance.meleeAttack, SoundManager.instance.SoundSource, true);
    }

    protected void DeactivateHitBoxCrouch()
    {
        _meleeHitBoxCrouch.SetActive(false);
    }
}