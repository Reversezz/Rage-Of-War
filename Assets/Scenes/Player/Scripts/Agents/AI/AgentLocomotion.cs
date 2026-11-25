using UnityEngine;

/// <summary>
/// Исполнитель движения: превращает MovementControlFlags в реальное перемещение CharacterController
/// и в параметры Velocity X / Velocity Z для BlendTree.
/// <br/><br/>
/// Главная идея: агент двигается в системе координат «относительно цели»:
/// <br/>  вперёд = к цели, вправо = перпендикулярно направлению на цель.
/// <br/>  Поэтому боковой шаг — это движение по дуге вокруг врага, а не «куда-то вбок от тела».
/// <br/>
/// Скорость в м/с совпадает с точками BlendTree (шаг 1.5, бег 4, диагональ = нормализованный вектор * скорость),
/// поэтому анимация всегда соответствует реальному движению.
/// </summary>
public sealed class AgentLocomotion
{
    private static readonly int VelocityX = Animator.StringToHash("Velocity X");
    private static readonly int VelocityZ = Animator.StringToHash("Velocity Z");

    private readonly Transform _transform;
    private readonly CharacterController _controller;
    private readonly Animator _animator;

    // Настройки — Agent обновляет их каждый тик, чтобы работала правка в инспекторе во время Play
    public float MoveSpeed = 1.5f;
    public float SprintSpeed = 4f;
    public float SmoothTime = 0.12f;

    // Текущая скорость в системе «относительно цели»: x — вправо, y — вперёд (к цели). Метры в секунду.
    private Vector2 _velocity;

    public AgentLocomotion(Transform transform, CharacterController controller, Animator animator)
    {
        _transform = transform;
        _controller = controller;
        _animator = animator;
    }

    /// <param name="forwardToTarget">Горизонтальное направление на цель (нормализованное). Если цели нет — передай transform.forward.</param>
    /// <param name="verticalVelocity">Вертикальная скорость от гравитации (AgentGravity.AgentVelocity.y).</param>
    public void Tick(float dt, MovementControlFlags flags, Vector3 forwardToTarget, float verticalVelocity)
    {
        // 1. Флаги -> желаемая скорость
        Vector2 dir = flags.ToLocalDirection(out bool run);
        Vector2 desired = dir * (run ? SprintSpeed : MoveSpeed);

        // 2. Плавный разгон/торможение (не зависит от FPS)
        float t = 1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, SmoothTime));
        _velocity = Vector2.Lerp(_velocity, desired, t);

        // 3. «Относительно цели» -> мировой вектор
        Vector3 forward = forwardToTarget;
        Vector3 right = Vector3.Cross(Vector3.up, forward);   // для forward = (0,0,1) даёт (1,0,0)
        Vector3 horizontal = right * _velocity.x + forward * _velocity.y;

        _controller.Move((horizontal + Vector3.up * verticalVelocity) * dt);

        // 4. Анимация: скорость относительно ТЕЛА. Тело поворачивается к цели с задержкой (Slerp),
        //    и InverseTransformDirection сам даёт честную картинку, даже пока оно не довернулось.
        Vector3 local = _transform.InverseTransformDirection(horizontal);
        _animator.SetFloat(VelocityX, local.x);
        _animator.SetFloat(VelocityZ, local.z);
    }
}