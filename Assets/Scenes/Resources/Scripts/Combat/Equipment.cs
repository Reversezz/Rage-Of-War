using System;
using UnityEngine;

public class Equipment : MonoBehaviour
{
    [Header("Держатели для оружия на руках")]
    public Transform RIGHT_HAND_COMBAT;
    public Transform LEFT_HAND_COMBAT;


    public Weapon Weapon
    {
        get
        {
            if (RIGHT_HAND_COMBAT.childCount > 0)
            {
                return RIGHT_HAND_COMBAT.GetComponentInChildren<Weapon>();
            }
            
            return null;
        }        
    }

    public Weapon Shield
    {
        get
        {
            if (LEFT_HAND_COMBAT.childCount > 0)
            {
                return LEFT_HAND_COMBAT.GetComponentInChildren<Weapon>();
            }

            return null;
        }
    }

    private void Awake()
    {
        if (RIGHT_HAND_COMBAT == null)
        {
            throw new Exception("Weapon IS NULL");
        }

        if (LEFT_HAND_COMBAT == null)
        {
            throw new Exception("Shield IS NULL");
        }
    }

    // в будущем броня
}