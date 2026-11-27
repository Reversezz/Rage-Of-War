using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerAgent : BaseWarriorAgent
{
    [Header("Компоненты")]
    public CharacterController controller;

    [Header("Снаряжение")]
    [SerializeField] private Equipment _equipment;

    [Header("Команда")]
    [SerializeField] private Material _team;

    [Header("UI")]
    [SerializeField] private UIPlayerHealthBar _healthBar;

    private InputAction _lookAction;
    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;
    private InputAction _attackAction;
    private InputAction _blockAction;
    private InputAction _characterOverview;

    private bool isCharacterOverviewActive;
    private bool isReturningFromCharacterOverview;

    [Header("Camera")]
    public Transform playerCameraTarget;
    [Range(0f, 0.4f)] public float MouseSensitivity = 0.1f;
    public float TopClamp = 30f;
    public float BottomClamp = -20f;

    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    [Header("Настройки")]
    public float MoveSpeed = 1.5f;
    public float SprintSpeed = 4f;
    public float JumpHeight = 1.5f;
    public float SpeedChangeRate = 10f;
    public float AnimSmoothTime = 0.12f;

    private Vector3 _smoothedMove;
    private float _currentSpeed;
    private float _targetSpeed;

    public override Equipment Equipment => _equipment;
    public override Team Team => (Team)_team;


    protected override void Awake()
    {
        Die += OnDie;
        HealthChanged += OnHealthChanged;

        base.Awake(); // Тут же получаем Gravity и Animator

        _attackAction = InputSystem.actions.FindAction("Attack");
        _blockAction = InputSystem.actions.FindAction("Block");

        _moveAction = InputSystem.actions.FindAction("Move");
        _sprintAction = InputSystem.actions.FindAction("Sprint");
        _lookAction = InputSystem.actions.FindAction("Look");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _characterOverview = InputSystem.actions.FindAction("CharacterOverview");
    }

    private void OnHealthChanged(float health)
    {
        if (IsAlive) _healthBar.SetHealth(health);
        else _healthBar.SetHealth(0f); // чтобы обнулить полосу жизни
    }

    protected override void OnDie()
    {
        base.OnDie();
        controller = null;
    }

    protected override void Start()
    {
        base.Start(); // Подписки на оружие из BaseWarriorAgent
        _cinemachineTargetYaw = playerCameraTarget.eulerAngles.y;
    }

    private AttackDirection _validAttackDirection = AttackDirection.None;
    protected override void OnTick(float dt)
    {
        if (this.IsDead) return;
        base.OnTick(dt);

        AgentGravity.AgentGravityUpdate(dt);

        Movement(dt);
        Jump(); 

        // СБРОС ФЛАГА: только если мы ничего не нажимаем И мы в пустом состоянии
        if (!_attackAction.IsPressed() && !_blockAction.IsPressed() && this.Inactive())
        {
            IsReadyTriggered = false;
        }

        // ЛОГИКА АТАКИ
        if (_attackAction.IsPressed() && !_blockAction.IsPressed())
        {
            if (!IsReadyTriggered && !this.InAttackAction() && !this.InReboundFromAttackAction())
            {
                CameraDirection camDir = GetDirectionFromMouse();

                if (camDir != CameraDirection.Invalid)
                {
                    _validAttackDirection = GetAttackDirectionFromMouse(camDir);
                }

                if (_validAttackDirection != AttackDirection.None)
                {
                    ReadyAttack(_validAttackDirection);
                }
            }
        }

        // ОТПУСКАНИЕ АТАКИ (вместо события canceled)
        if (_attackAction.WasReleasedThisFrame())
        {
            ReleaseAttack();
        }

        // ЛОГИКА БЛОКА
        if (_blockAction.IsPressed() && !_attackAction.IsPressed())
        {
            if (!this.InAttackAction() && !this.InReboundFromBlockAction())
            {
                GuardDirection dir = GetBlockDirectionFromMouse(GetDirectionFromMouse());
                HoldBlock(dir);
            }
        }

        // ОТПУСКАНИЕ БЛОКА (вместо события canceled)
        if (_blockAction.WasReleasedThisFrame())
        {
            UnBlock();
        }
    }


    protected override void Movement(float dt)
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);
        move = Vector3.ClampMagnitude(move, 1f);

        _smoothedMove = Vector3.Lerp(_smoothedMove, move, dt / AnimSmoothTime);
        Vector3 worldMove = transform.forward * move.z + transform.right * move.x;

        _targetSpeed = worldMove.magnitude * (_sprintAction.IsPressed() ? SprintSpeed : MoveSpeed);
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, dt * SpeedChangeRate);

        Vector3 horizontalMove = worldMove * _currentSpeed;
        controller?.Move((horizontalMove + Vector3.up * AgentGravity.AgentVelocity.y) * dt);

        Animator.SetFloat("Velocity X", _smoothedMove.x * _currentSpeed);
        Animator.SetFloat("Velocity Z", _smoothedMove.z * _currentSpeed);
    }



    protected override void OnLateTick(float dt)
    {
        if (!this.IsDead) CameraRotation(dt);
    }

    private void CameraRotation(float dt)
    {
        Vector2 look = _lookAction.ReadValue<Vector2>();
        if (!isReturningFromCharacterOverview && look.sqrMagnitude >= 0.01f)
        {
            _cinemachineTargetYaw += look.x * MouseSensitivity;
            _cinemachineTargetPitch += look.y * MouseSensitivity * 0.5f;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
        CameraViewLogic(dt);
    }

    private void CameraViewLogic(float dt)
    {
        if (_characterOverview.IsPressed())
        {
            isCharacterOverviewActive = true;
            isReturningFromCharacterOverview = false;
            playerCameraTarget.rotation = Quaternion.Euler(-_cinemachineTargetPitch, _cinemachineTargetYaw, 0f);
        }
        else
        {
            if (isCharacterOverviewActive == true)
            {
                playerCameraTarget.rotation = Quaternion.Lerp(playerCameraTarget.rotation, transform.rotation, dt * 6f);
                if (Quaternion.Angle(playerCameraTarget.rotation, transform.rotation) < 1f)
                {
                    _cinemachineTargetYaw = transform.eulerAngles.y;
                    _cinemachineTargetPitch = 0f;
                    isCharacterOverviewActive = false;
                }
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, _cinemachineTargetYaw, 0f);
                playerCameraTarget.rotation = Quaternion.Euler(-_cinemachineTargetPitch, _cinemachineTargetYaw, 0f);
            }
        }
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }


    public enum CameraDirection
    {
        Invalid = -1,

        Left, Right, Up, Down
    }

    private CameraDirection GetDirectionFromMouse()
    {
        Vector2 look = _lookAction.ReadValue<Vector2>();
        float threshold = 0.2f;

        // Общая сила движения мыши достаточная?
        if (look.sqrMagnitude >= threshold * threshold)
        {
            // Определяем доминирующую ось и смотрим её знак
            if (Mathf.Abs(look.x) > Mathf.Abs(look.y))
            {
                return look.x > 0 ? CameraDirection.Right : CameraDirection.Left;
            }
            else
            {
                return look.y > 0 ? CameraDirection.Up : CameraDirection.Down;
            }
        }

        return CameraDirection.Invalid;
    }

    private AttackDirection GetAttackDirectionFromMouse(CameraDirection direction)
    {
        return direction switch
        {
            CameraDirection.Left => AttackDirection.Left,
            CameraDirection.Right => AttackDirection.Right,
            CameraDirection.Up => AttackDirection.Up,
            CameraDirection.Down => AttackDirection.Down,
            _ => AttackDirection.None,
        };
    }

    private GuardDirection GetBlockDirectionFromMouse(CameraDirection direction)
    {
        return direction switch
        {
            CameraDirection.Left => GuardDirection.Left,
            CameraDirection.Right => GuardDirection.Right,
            CameraDirection.Up => GuardDirection.Up,
            CameraDirection.Down => GuardDirection.Down,
            _ => GuardDirection.None,
        };
    }


    private void Jump()
    {
        if (AgentGravity.isGrounded && _jumpAction.WasPressedThisFrame())
        {
            Animator.SetBool("Jump", true);
            AgentGravity.AgentVelocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity.GravityValue);
        }
    }
}