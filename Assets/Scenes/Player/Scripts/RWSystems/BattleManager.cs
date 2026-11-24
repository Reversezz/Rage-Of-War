using System;
using UnityEngine;

public class BattleManager : MonoBehaviour
{

    public static BattleManager Instance;
    public BattleManager() => Instance = this;



    public event Action<float> OnTick;
    
    private void Update()
    {
        OnTick?.Invoke(Time.deltaTime);
    }

    public event Action<float> OnLateTick;

    private void LateUpdate()
    {
        OnLateTick?.Invoke(Time.deltaTime);
    }
}