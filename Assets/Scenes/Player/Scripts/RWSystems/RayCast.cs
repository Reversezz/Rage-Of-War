using System;
using UnityEngine;

public class RayCast : MonoBehaviour
{
    public static RayCast Instance;
    public RayCast() => Instance = this;


    public Transform RayCastRoot;

    public event Action<RaycastHit> OnRaycastHit;

    public float ViewAngle = 120f;
    public float ViewDistance = 25f;
    public int RayCount = 9;
    public LayerMask ObtacleMask;

    private void Update()
    {
        if (((BaseWarriorAgent)transform.root).IsDead) return;

        float startAngle = -ViewAngle / 2f; // начинаем с угла слева (-60 градус от центра)
        float angleStep = ViewAngle / (RayCount - 1); // 120 / 5 = 24 (каждые 24 градуса — луч)

        for (int i = 0; i < RayCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);

            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 rayDirection = rotation * RayCastRoot.forward;

            Ray ray = new Ray(RayCastRoot.position, rayDirection);

            if (Physics.Raycast(ray, out RaycastHit hit, ViewDistance, ObtacleMask))
            {
                // если это не оружие или щит агента
                if (!hit.transform.TryGetComponent(out Weapon weapon)) 
                {
                    Debug.DrawRay(ray.origin, rayDirection * hit.distance, Color.red);
                    OnRaycastHit?.Invoke(hit);
                }
                else if (hit.transform.TryGetComponent(out BaseWarriorAgent baseWarriorAgent))
                {
                    if (!baseWarriorAgent.IsDead)
                    {
                        Debug.DrawRay(ray.origin, rayDirection * hit.distance, Color.green);
                        OnRaycastHit?.Invoke(hit);
                    }
                }
            }
            else
            {
                Debug.DrawRay(ray.origin, rayDirection * hit.distance, Color.green);  
            }
                          
        }
    }
}