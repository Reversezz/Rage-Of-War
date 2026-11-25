using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class Gravity : MonoBehaviour
{  
    [Header("Character Controller")]
    public CharacterController controller;


    // —ила гравитации
    public const float GravityValue = -9.81f;

    public Vector3 AgentVelocity;    
    public bool isGrounded;
    

    /// <summary> ќбновление гравитации каждый тик </summary>
    public void AgentGravityUpdate(float dt)
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            // Ќебольша€ скорость вниз, чтобы удерживать персонажа устойчиво на земле
            if (AgentVelocity.y < -2f)
                AgentVelocity.y = -2f;
        }

        // ѕримен€ем гравитацию
        AgentVelocity.y += GravityValue * dt;
    }
}