using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class XRayOrb : MonoBehaviour
{
    [SerializeField] private XRay _xray;

    void Start()
    {
        if (!_xray)
            _xray = GetComponentInParent<XRay>();
    }

    private void OnTriggerEnter(Collider c)
    {
        if (c.gameObject.tag == "Enemy")
        {
            _xray.AddXrayObject(c.gameObject);
        }
    }
}
