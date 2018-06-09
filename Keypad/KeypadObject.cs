#pragma warning disable 0108

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class KeypadObject : Trigger
{
    public enum State { Idle, Correct, Wrong }

    [Tooltip("The text in world space that shows what the passcode is.")]
    [SerializeField]
    private Text _worldCodeText;
    [SerializeField] private UnityEvent _onPasscodeAccept;

    private Passcode _passcode; // object that makes and stores a code.
    private int _playerCodeIndex = 0; // index of where you are in the player's passcode array.
    public int playerCodeIndex { get { return _playerCodeIndex; } }
    private int[] _playerCode = new int[4]; // the code the player has input

    private bool _inRange = false; // are you close enough to the keypad to use it?
    private WaitForSeconds _waitToReset;
    private GameObject _player; // ref to the player in order to shut him down
    private CharacterBehaviour _characterBehaviour;
    private TeleportScript _teleportScript;
    private CharacterControllerScheme _characterControllerScheme;

    [SerializeField]
    private Renderer _holoDisplay;
    [SerializeField]
    private Color _passColor;

    void Start()
    {
        base.Start();

        // initialise and set default vaiables
        _waitToReset = new WaitForSeconds(1.0f);
        _passcode = new Passcode();
        _worldCodeText.text = string.Format("{0}\t{1}\t{2}\t{3}", _passcode.code[0], _passcode.code[1], _passcode.code[2], _passcode.code[3]);

        // update the on screen text
        UpdateText();
        Canvas_Manager.Keypad.UpdateStatusNodes(State.Idle);
    }

    void Update()
    {
        //check if the player if the player is in range and in the keypad to disable components on the player
        if (Canvas_Manager.Keypad.canvas.enabled && _inRange)
            DisablePlayer(true);

        // check to see if player has intereacted with keypad
        if (Input.GetKeyDown(KeyCode.F) && _inRange)
        {
            // if the player is not in the keypad, enter it, else, kick the player out of it
            if (!Canvas_Manager.Keypad.canvas.enabled)
            {
                // tell the camer the player is not in playmode, pass this reference to the keypad canvas, enable the canvas on screen

                Canvas_Manager.Keypad.EnterKeypad(this);
                Canvas_Manager.Keypad.canvas.enabled = true;
                Canvas_Manager.EnableUICursor(true);
            }
            else
            {
                KickPlayerOut();
            }
        }
    }

    // remove player from canvas
    private void KickPlayerOut()
    {
        if (Canvas_Manager.Keypad.canvas.enabled)
        {
            // remove ref from keypad canvas, player not in keypad bool, enable the player, tell camera in game mode, lock cursor, disable canvas
            Canvas_Manager.Keypad.ExitKeypad();
            DisablePlayer(false);

            Canvas_Manager.EnableUICursor(false);

            Canvas_Manager.Keypad.canvas.enabled = false;
        }
    }

    public void Exit()
    {
        KickPlayerOut();
    }

    // adds number to the player's code and updates the on screen text
    public void AddNumber(int numToAdd)
    {
        _playerCode[_playerCodeIndex] = numToAdd;
        ++_playerCodeIndex;
        UpdateText();
    }

    // removed last number added and updates on screen text
    public void RemoveNumber()
    {
        _playerCodeIndex--;
        _playerCode[_playerCodeIndex] = 0;
        UpdateText();
    }

    // compares codes if the player has entered 4 numbers
    public void ComparePasscodes()
    {
        if (_playerCodeIndex < 4)
        {
            Result(false);
            return;
        }

        for (int i = 0; i < _playerCode.Length; ++i)
        {
            if (i == (_playerCode.Length - 1) && _playerCode[i] == _passcode.code[i])
            {
                Result(true);
            }
            else if (_playerCode[i] != _passcode.code[i])
            {
                Result(false);
                break;
            }
        }
    }

    // starts the code accepted of not accepted 
    void Result(bool pass)
    {
        if (pass)
        {
            // play sound
            SoundManager.instance.PlaySound(SoundManager.instance.keypadRightAnswer, gameObject.GetComponent<AudioSource>(), true);

            Canvas_Manager.Keypad.UpdateStatusNodes(State.Correct);
            Canvas_Manager.Keypad.UpdateText(Canvas_Manager.Keypad.display.text, State.Correct);

            _onPasscodeAccept.Invoke();
            ChangeDisplayColor();
            StartCoroutine(Reset_playerCode(pass));
        }
        else
        {
            // play sound
            SoundManager.instance.PlaySound(SoundManager.instance.keypadWrongAnswer, gameObject.GetComponent<AudioSource>(), true);

            Canvas_Manager.Keypad.UpdateStatusNodes(State.Wrong);
            Canvas_Manager.Keypad.UpdateText(Canvas_Manager.Keypad.display.text, State.Wrong);

            StartCoroutine(Reset_playerCode(pass));
        }
    }

    // updates the on screen text of the code
    void UpdateText()
    {
        // make an array of strings to properly sort out code from dash
        string[] playerPassword = new string[4];
        // for every number input on the pad, print a number, else print a dash
        for (int i = 0; i < playerPassword.Length; ++i)
        {
            if (_playerCodeIndex == 0 || i >= _playerCodeIndex)
                playerPassword[i] = "-";
            else
                playerPassword[i] = _playerCode[i].ToString();
        }

        Canvas_Manager.Keypad.UpdateText(string.Format("{0}{1}{2}{3}",
            playerPassword[0],
            playerPassword[1],
            playerPassword[2],
            playerPassword[3]));
    }

    // disables or enables the player based on entered bool
    void DisablePlayer(bool pSetDisable)
    {
        /** Tell teleport script you are in a keypad before the script is called to be disabled */
        if (!_teleportScript)
            _teleportScript = _player.GetComponent<TeleportScript>();
        _teleportScript.isInKeypad = pSetDisable;

        foreach (MonoBehaviour script in _player.GetComponents<MonoBehaviour>())
        {
            script.enabled = !pSetDisable;
        }

        _characterBehaviour = _player.GetComponent<CharacterBehaviour>();
        _characterControllerScheme = _player.GetComponent<CharacterControllerScheme>();

        if (pSetDisable)
        {
            _characterBehaviour.SetPhysicsColider(false);
            _characterControllerScheme.StopMovement();
        }
        if (!pSetDisable)
        {
            _characterBehaviour.SetPhysicsColider(true);
        }
    }

    void OnTriggerEnter(Collider coll)
    {
        base.OnTriggerEnter(coll);

        if (coll.CompareTag(GameManager.Tags.Player.ToString()))
        {
            if (!_player)
            {
                _player = coll.gameObject;
            }
            _inRange = true;
        }
    }

    void OnTriggerExit(Collider coll)
    {
        base.OnTriggerExit(coll);

        _inRange = false;
    }

    IEnumerator Reset_playerCode(bool kickPlayerOut)
    {
        yield return _waitToReset;

        Canvas_Manager.Keypad.UpdateStatusNodes(State.Idle);

        if (kickPlayerOut)
            KickPlayerOut();

        for (int i = 0; i < _playerCode.Length; ++i)
        {
            _playerCode[i] = 0;
        }

        _playerCodeIndex = 0;

        UpdateText();
    }

    private void ChangeDisplayColor()
    {
        _holoDisplay.material.SetColor("_Switch3Color", _passColor);
    }
}