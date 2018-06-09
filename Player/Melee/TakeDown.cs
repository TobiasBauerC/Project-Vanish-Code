using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakeDown : MonoBehaviour
{ 
    void OnTriggerEnter(Collider c)
    {
        if (c.gameObject.tag == CharacterStringLibrary.CharCons.Enemy.ToString())
        {
            Debug.Log("Hit Enemy");
            c.gameObject.GetComponent<Enemy>().TakeHit(Melee._stunTime);
        }
    }
}
