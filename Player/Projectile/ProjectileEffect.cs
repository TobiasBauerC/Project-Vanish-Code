using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileEffect : MonoBehaviour
{
    /** The value on the material to change's name */
    private const string TESSELLATION = "_Tessellation";

    /** Renderer of the ball */
    [SerializeField] private Renderer _renderer;

    private float _endValue;
    private float _elapsedChargeTime;

    public void OnStart(float maxChargeTime)
    {
        /** If there is no renderer, find one. Then set default value for renderer */
        if (!_renderer)
            _renderer = GetComponent<Renderer>();
        _renderer.material.SetFloat(TESSELLATION, 0.0f);

        _endValue = transform.localScale.x;
        transform.localScale = Vector3.zero;
        _elapsedChargeTime = 0.0f;
    }

    /** Updates the effect's size */
    public void UpdateSize(float deltaC, float maxChargeTime, bool finalCheck)
    {
        if (finalCheck == true && _elapsedChargeTime >= 2.0f)
            return;
        _elapsedChargeTime += deltaC;
        float value = _renderer.material.GetFloat(TESSELLATION);
        value += 0.21f / maxChargeTime * deltaC;
        if (value > 0.21f)
            value = 0.21f;
        _renderer.material.SetFloat(TESSELLATION, value);

        float newValue = transform.localScale.x;
        newValue += _endValue / maxChargeTime * deltaC;
        if (newValue > _endValue)
            newValue = _endValue;
        transform.localScale = new Vector3(newValue, newValue, newValue);
    }
}
