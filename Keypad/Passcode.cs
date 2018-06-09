using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Passcode
{
    private int[] _passcode = new int[4]; // stores the pre made code
    public int[] code { get { return _passcode; } }

    public Passcode()
    {
        // create random code with no regard to code order or structure
        for (int i = 0; i < _passcode.Length; ++i)
        {
            _passcode[i] = (int) Random.Range(0, 10);
        }
    }
}