using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class Gravity : MonoBehaviour
{  
    [Header("Character Controller")]
    public CharacterController controller;


    // Сила гравитации
    public const float GravityValue = -9.81f;

    public Vector3 AgentVelocity;    
    public bool isGrounded;
    

    /// <summary> Обновление гравитации каждый тик </summary>
    public void AgentGravityUpdate(float dt)
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            // Небольшая скорость вниз, чтобы удерживать персонажа устойчиво на земле
            if (AgentVelocity.y < -2f)
                AgentVelocity.y = -2f;
        }

        // Применяем гравитацию
        AgentVelocity.y += GravityValue * dt;
    }
}