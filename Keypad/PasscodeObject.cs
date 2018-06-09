using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PasscodeObject : MonoBehaviour
{
    private KeypadObject _currentKeypad; // keep reference to the keypad that the player is accessing

    [SerializeField]
    private Canvas _canvas;
    public Canvas canvas
    {
        get { return _canvas; }
    }

    [Header("Display")]
    [SerializeField]
    private Text _display;
    public Text display
    {
        get { return _display; }
    }

    [SerializeField]
    private Color _correctColor;
    [SerializeField]
    private Color _wrongColor;
    private Color _defaultColor;

    [Header("Status Nodes")]
    [SerializeField]
    private GameObject _statusNodesIdle;
    [SerializeField]
    private GameObject _statusNodesCorrect;
    [SerializeField]
    private GameObject _statusNodesWrong;

    [Header("Buttons")]
    [SerializeField]
    private Button _btn0;
    [SerializeField]
    private Button _btn1;
    [SerializeField]
    private Button _btn2;
    [SerializeField]
    private Button _btn3;
    [SerializeField]
    private Button _btn4;
    [SerializeField]
    private Button _btn5;
    [SerializeField]
    private Button _btn6;
    [SerializeField]
    private Button _btn7;
    [SerializeField]
    private Button _btn8;
    [SerializeField]
    private Button _btn9;
    [SerializeField]
    private Button _btnEnter;
    [SerializeField]
    private Button _btnBack;
    [SerializeField]
    private Button _btnExit;

    [SerializeField]
    private AudioSource _audioSource;

    private void Start()
    {
        Supporting.CheckRequiredProperty(gameObject, _canvas, "Canvas");
        Supporting.CheckRequiredProperty(gameObject, _display, "Display");
        Supporting.CheckRequiredProperty(gameObject, _statusNodesIdle, "StatusNodeIdle");
        Supporting.CheckRequiredProperty(gameObject, _statusNodesCorrect, "StatusNodeCorrect");
        Supporting.CheckRequiredProperty(gameObject, _statusNodesWrong, "StatusNodeWrong");
        Supporting.CheckRequiredProperty(gameObject, _btn0, "Button 0");
        Supporting.CheckRequiredProperty(gameObject, _btn1, "Button 1");
        Supporting.CheckRequiredProperty(gameObject, _btn2, "Button 2");
        Supporting.CheckRequiredProperty(gameObject, _btn3, "Button 3");
        Supporting.CheckRequiredProperty(gameObject, _btn4, "Button 4");
        Supporting.CheckRequiredProperty(gameObject, _btn5, "Button 5");
        Supporting.CheckRequiredProperty(gameObject, _btn6, "Button 6");
        Supporting.CheckRequiredProperty(gameObject, _btn7, "Button 7");
        Supporting.CheckRequiredProperty(gameObject, _btn8, "Button 8");
        Supporting.CheckRequiredProperty(gameObject, _btn9, "Button 9");
        Supporting.CheckRequiredProperty(gameObject, _btnEnter, "Button Enter");
        Supporting.CheckRequiredProperty(gameObject, _btnBack, "Button Back");
        Supporting.CheckRequiredProperty(gameObject, _btnExit, "Button Exit");
        Supporting.CheckRequiredProperty(gameObject, _audioSource, "AudioSource");

        _defaultColor = display.color;

        BindButtons();
    }

    private void BindButtons()
    {
        _btn0.onClick.AddListener(() => OnNumberAdd(0));
        _btn1.onClick.AddListener(() => OnNumberAdd(1));
        _btn2.onClick.AddListener(() => OnNumberAdd(2));
        _btn3.onClick.AddListener(() => OnNumberAdd(3));
        _btn4.onClick.AddListener(() => OnNumberAdd(4));
        _btn5.onClick.AddListener(() => OnNumberAdd(5));
        _btn6.onClick.AddListener(() => OnNumberAdd(6));
        _btn7.onClick.AddListener(() => OnNumberAdd(7));
        _btn8.onClick.AddListener(() => OnNumberAdd(8));
        _btn9.onClick.AddListener(() => OnNumberAdd(9));
        _btnEnter.onClick.AddListener(() => OnEnterButton());
        _btnBack.onClick.AddListener(() => OnBackButton());
        _btnExit.onClick.AddListener(() => OnExitButton());
    }

    // called when the player first interacts with the keypad in order to pass reference
    public void EnterKeypad(KeypadObject keypad)
    {
        _currentKeypad = keypad;
    }

    // removes the reference to the keypad to avoid unexpected behaviour
    public void ExitKeypad()
    {
        _currentKeypad = null;
    }

    // called when a number button is pressed. Passes that value to the player code
    public void OnNumberAdd(int pEntry)
    {
        // play Sound
        SoundManager.instance.PlaySound(SoundManager.instance.switchActivate, _audioSource, true);

        if (_currentKeypad.playerCodeIndex < 4)
            _currentKeypad.AddNumber(pEntry);
    }

    // called when the back button is pressed, removes the last added number from player's code
    public void OnBackButton()
    {
        // Play Sound
        SoundManager.instance.PlaySound(SoundManager.instance.switchActivate, _audioSource, true);

        if (_currentKeypad.playerCodeIndex > 0)
            _currentKeypad.RemoveNumber();
    }

    // called when the enter key is pressed. Calls to check is the code is correct.
    public void OnEnterButton()
    {
        // Play Sound
        SoundManager.instance.PlaySound(SoundManager.instance.switchActivate, _audioSource, true);

        _currentKeypad.ComparePasscodes();
    }

    public void OnExitButton()
    {
        // Play Sound
        SoundManager.instance.PlaySound(SoundManager.instance.switchActivate, _audioSource, true);

        _currentKeypad.Exit();
    }

    public void UpdateText(string text, KeypadObject.State state = KeypadObject.State.Idle)
    {
        if (display.text != text)
        {
            display.text = string.Format("{0}\n{1}\n{2}\n{3}", text[0], text[1], text[2], text[3]);
        }

        switch (state)
        {
            case KeypadObject.State.Correct:
                display.color = _correctColor;
                break;
            case KeypadObject.State.Wrong:
                _display.color = _wrongColor;
                break;
            case KeypadObject.State.Idle:
            default:
                _display.color = _defaultColor;
                break;
        }
    }

    public void UpdateStatusNodes(KeypadObject.State state)
    {
        switch (state)
        {
            case KeypadObject.State.Correct:
                _statusNodesIdle.SetActive(false);
                _statusNodesCorrect.SetActive(true);
                _statusNodesWrong.SetActive(false);
                break;
            case KeypadObject.State.Wrong:
                _statusNodesIdle.SetActive(false);
                _statusNodesCorrect.SetActive(false);
                _statusNodesWrong.SetActive(true);
                break;
            case KeypadObject.State.Idle:
            default:
                _statusNodesIdle.SetActive(true);
                _statusNodesCorrect.SetActive(false);
                _statusNodesWrong.SetActive(false);
                break;
        }
    }
}