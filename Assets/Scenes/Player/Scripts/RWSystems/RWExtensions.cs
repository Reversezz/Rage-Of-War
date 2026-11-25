using UnityEngine;

public static class RWExtensions
{
    private readonly static string[] AnimationsNames =
    {
        // --- АТАКА ---
        // UP
        "Warrior_READY_UP", "Warrior_ATTACK_UP", "Warrior_REBOUND_ATTACK_UP",
        // RIGHT
        "Warrior_READY_RIGHT", "Warrior_ATTACK_RIGHT", "Warrior_REBOUND_ATTACK_RIGHT",
        // DOWN
        "Warrior_READY_DOWN", "Warrior_ATTACK_DOWN", "Warrior_REBOUND_ATTACK_DOWN",
        // LEFT
        "Warrior_READY_LEFT", "Warrior_ATTACK_LEFT", "Warrior_REBOUND_ATTACK_LEFT",

        // --- ЗАЩИТА ---
        // UP
        "Warrior_BLOCK_UP", "Warrior_BLOCK_UP_REBOUND",
        // RIGHT
        "Warrior_BLOCK_RIGHT", "Warrior_BLOCK_RIGHT_REBOUND",
        // DOWN
        "Warrior_BLOCK_DOWN", "Warrior_BLOCK_DOWN_REBOUND",
        // LEFT
        "Warrior_BLOCK_LEFT", "Warrior_BLOCK_LEFT_REBOUND",
    };

    public static bool InReadyAttackAction(this BaseWarriorAgent baseWarriorAgent) => baseWarriorAgent.CurrentActionState == AgentActionStateType.Windup;
    public static bool InAttackAction(this BaseWarriorAgent baseWarriorAgent) => baseWarriorAgent.CurrentActionState == AgentActionStateType.Attack;
    public static bool InBlockAction(this BaseWarriorAgent baseWarriorAgent) => baseWarriorAgent.CurrentActionState == AgentActionStateType.Block;
    public static bool InReboundFromAttackAction(this BaseWarriorAgent baseWarriorAgent) => baseWarriorAgent.CurrentActionState == AgentActionStateType.ReboundAttack;
    public static bool InReboundFromBlockAction(this BaseWarriorAgent baseWarriorAgent) => baseWarriorAgent.CurrentActionState == AgentActionStateType.ReboundBlock;
    public static bool Inactive(this BaseWarriorAgent baseWarriorAgent) => baseWarriorAgent.CurrentActionState == AgentActionStateType.Inactive;

    public static string GetCurrentAnimationName(this Animator animator, int layerIndex)
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);

        if (animator.IsInTransition(layerIndex) && animator.GetAnimatorTransitionInfo(layerIndex).normalizedTime >= 0.5f)
            info = animator.GetNextAnimatorStateInfo(layerIndex);

        foreach (string animName in AnimationsNames)
            if (info.IsName(animName)) return animName;
        return string.Empty;
    }

    public static float GetAnimationStateProgress(this Animator animator, int layerIndex)
    {
        return animator.GetCurrentAnimatorStateInfo(layerIndex).normalizedTime;
    }

    public static bool IsCurrentAnimationNamed(this Transform root, int layerIndex, string animName)
    {
        if (root.TryGetComponent(out Animator animator))
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
            
            if (animator.IsInTransition(layerIndex) && animator.GetAnimatorTransitionInfo(layerIndex).normalizedTime >= 0.5f)
                info = animator.GetNextAnimatorStateInfo(layerIndex);

            if (info.IsName(animName))
            {
                return true;
            }
        }
        else throw new MissingComponentException($"Не удалось найти Animator у {root.name} в GetCurrentAnimationStateFromName()");        

        return false;
    }

    public static bool IsCurrentAnimationNamed(this Animator animator, int layerIndex, string animName)
    {
        if (animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(animName))
        {
            return true;
        }
        else return false;
    }

    // Возвращает все коллайдеры, попавшие в зону BoxCollider-а оружия
    public static Collider[] GetHitColliders(this Collider weaponCollider, LayerMask hitLayers)
    {
        // Берем габариты коллайдера в мировых координатах
        Vector3 center = weaponCollider.bounds.center;
        Vector3 halfExtents = weaponCollider.bounds.extents;
        Quaternion rotation = weaponCollider.transform.rotation;

        // Проверяем пересечение
        return Physics.OverlapBox(center, halfExtents, rotation, hitLayers);
    }

    public static bool IsInAnyState_Ch1(this Animator animator)
    {
        foreach (string animation in AnimationsNames)
        {
            if (animator.GetCurrentAnimatorStateInfo(1).IsName(animation)) return true;
        }

        return false;
    }

    public static GuardDirection GetRelevantBlockDirectionFromAttackDirection(this AttackDirection attackDirection)
    {
        return attackDirection switch
        {
                AttackDirection.Up => GuardDirection.Up,
                AttackDirection.Down => GuardDirection.Down,
                AttackDirection.Left => GuardDirection.Left,
                AttackDirection.Right => GuardDirection.Right,

                _ => GuardDirection.None
        };
    }

    public static bool InRange(this float f, float min, float max) => (min <= f) && (max >= f);

    // Любое движения в указанном направление
    private const MovementControlFlags ForwardMask = MovementControlFlags.MoveForward | MovementControlFlags.RunForward;
    private const MovementControlFlags BackwardMask = MovementControlFlags.MoveBackward | MovementControlFlags.RunBackward;
    private const MovementControlFlags RightMask = MovementControlFlags.MoveRight | MovementControlFlags.RunRight;
    private const MovementControlFlags LeftMask = MovementControlFlags.MoveLeft | MovementControlFlags.RunLeft;

    // Если ВООБЩЕ бежит в любую сторону
    private const MovementControlFlags RunMask = MovementControlFlags.RunForward | MovementControlFlags.RunBackward
                                                    | MovementControlFlags.RunRight | MovementControlFlags.RunLeft;

    /// <summary> Флаги -> направление: x = вправо, y = вперёд. Диагональ нормализуется, пустые флаги = стоим. </summary>
    public static Vector2 ToLocalDirection(this MovementControlFlags flags, out bool run)
    {
        run = (flags & RunMask) != 0;

        float x = ((flags & RightMask) != 0 ? 1f : 0f) - ((flags & LeftMask) != 0 ? 1f : 0f);
        float z = ((flags & ForwardMask) != 0 ? 1f : 0f) - ((flags & BackwardMask) != 0 ? 1f : 0f);

        Vector2 dir = new Vector2(x, z);
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }
}