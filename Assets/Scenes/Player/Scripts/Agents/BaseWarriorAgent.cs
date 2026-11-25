using System;
using UnityEngine;

public abstract class BaseWarriorAgent : BaseAgent
{
    protected virtual void OnDie()
    {
        Debug.Log($"{name}: попал в OnDie() в BaseWarriorAgent");

        if (CurrentAnimName != string.Empty)  CurrentAnimName = string.Empty;
        if (IsReadyTriggered != false)        IsReadyTriggered = false;
        if (IsBlockOn != false)               IsBlockOn = false;
        if (triggerReadyName != null)         triggerReadyName = null;
    }

    private void OnDead()
    {
        // Отключаем зрение агента (на игроке не сработает, потому что на нём нет RayCast)

        if (TryGetComponent(out RayCast r)) r.enabled = false;

        // Отключение коллайдеров оружия и щита агента
        SetColliderEnabled(Equipment.Weapon, value: false);
        SetColliderEnabled(Equipment.Shield, value: false);

        if (TryGetComponent(out Animator a)) a.enabled = false;

        // Отключаем коллайдер агента
        if (TryGetComponent(out CharacterController cc)) cc.enabled = false;
        
        // Отключение самого себя
        //this.enabled = false;

        void SetColliderEnabled(Weapon weapon, bool value)
        {
            if (weapon != null && weapon.TryGetComponent(out Collider collider))
            {
                collider.enabled = value;
            }
        }
    }


    private string[] ReadyTriggers = { "ReadyAttackUP", "ReadyAttackRIGHT", "ReadyAttackDOWN", "ReadyAttackLEFT" };

    public abstract Team Team { get; }
    public abstract Equipment Equipment { get; }

    /// <summary> Если агент — игрок </summary>
    public bool IsPlayer
    {
        get
        {
            if (this.TryGetComponent(out PlayerAgent player))
            {
                if (player.AgentState == AgentState.Active)
                    return true;
            }
            return false;
        }
    }

    /// <summary> Если агент — бот </summary>
    public bool IsAI
    {
        get
        {
            if (this.TryGetComponent(out Agent ai))
            {
                if (ai.AgentState == AgentState.Active)
                    return true;
            }
            return false;
        }
    }


    public static explicit operator BaseWarriorAgent(Transform AgentRoot)
    {
        if (AgentRoot.TryGetComponent(out BaseWarriorAgent agent)) 
            return agent;
        
        throw new MissingComponentException($"Не удалось найти компонент агента у {AgentRoot.name} в explicit operator BaseWarriorAgent()");
    }

    public MovementControlFlags MovementControlFlags { get; set; }

    public AgentActionStateType CurrentActionState
    {
        get
        {
            bool readyAction = CurrentAnimName.Contains("READY");
            bool attackAction = CurrentAnimName.Contains("ATTACK") && !CurrentAnimName.Contains("REBOUND");
            bool blockAction = CurrentAnimName.Contains("BLOCK") && !CurrentAnimName.Contains("REBOUND");
            bool reboundAttackAction = CurrentAnimName.Contains("REBOUND_ATTACK");
            bool reboundBlockAction = CurrentAnimName.Contains("BLOCK") && CurrentAnimName.Contains("REBOUND");

            if (readyAction)              return AgentActionStateType.Windup;
            else if (attackAction)        return AgentActionStateType.Attack;
            else if (blockAction)         return AgentActionStateType.Block;
            else if (reboundAttackAction) return AgentActionStateType.ReboundAttack;
            else if (reboundBlockAction)  return AgentActionStateType.ReboundBlock;
            else                          return AgentActionStateType.Inactive;
        }
    }

    public AttackDirection AttackDirection
    {
        get
        {
            if (CurrentActionState == AgentActionStateType.Windup || CurrentActionState == AgentActionStateType.Attack)
            {
                if (CurrentAnimName.Contains("UP"))         return AttackDirection.Up;
                else if (CurrentAnimName.Contains("DOWN"))  return AttackDirection.Down;
                else if (CurrentAnimName.Contains("LEFT"))  return AttackDirection.Left;
                else if (CurrentAnimName.Contains("RIGHT")) return AttackDirection.Right;

                else return AttackDirection.None;
            }
            else return AttackDirection.None;
        }
    }

    public GuardDirection GuardDirection
    {
        get
        {
            if (CurrentActionState == AgentActionStateType.Block)
            {
                if (CurrentAnimName.Contains("UP"))         return GuardDirection.Up;
                else if (CurrentAnimName.Contains("DOWN"))  return GuardDirection.Down;
                else if (CurrentAnimName.Contains("LEFT"))  return GuardDirection.Left;
                else if (CurrentAnimName.Contains("RIGHT")) return GuardDirection.Right;

                else return GuardDirection.None;
            }
            else return GuardDirection.None;
        }
    }

    // Рабочие поля
    public Vector3 Position;

    // Флаг, чтобы агент знали, можно ли начинать новый замах
    protected bool IsReadyTriggered = false;

    private Equipment BW_Equipment;
    private bool _isGotEquipmentComponent;


    protected override void Awake()
    {
        base.Awake();
        Dead += OnDead;

        Animator = GetComponent<Animator>();
        _isGotEquipmentComponent = TryGetComponent(out BW_Equipment);
    }

    protected virtual void Start()
    {
        if (_isGotEquipmentComponent)
        {
            if (BW_Equipment.Weapon != null)
            {
                BW_Equipment.Weapon.OnWeaponHit += Weapon_OnWeaponHit;
                BW_Equipment.Weapon.OnObstacleHit += Weapon_OnObstacleHit;
            }
            else throw new Exception($"У агента {BW_Equipment.transform.root.name}: Weapon == null");

            if (BW_Equipment.Shield != null) BW_Equipment.Shield.OnShieldBlocked += Shield_OnShieldBlocked;
            else throw new Exception($"У агента {BW_Equipment.transform.root.name}: Shield == null");
        }
    }

    protected string CurrentAnimName = string.Empty;
    protected override void OnTick(float dt)
    {
        base.OnTick(dt);

        Position = transform.position;
        CurrentAnimName = Animator.GetCurrentAnimationName(1);
    }


    // --- ОБРАБОТЧИКИ СОБЫТИЙ ОРУЖИЯ ---

    private void Weapon_OnWeaponHit(AttackCollisionData attackCollisionData, bool IsNoValidBlockDamage)
    {
        if (attackCollisionData.Victim != null)
        {
            var victim = attackCollisionData.Victim;

            float damage = !IsNoValidBlockDamage ? attackCollisionData.DamageInfo.WeaponDamage 
                                                 : attackCollisionData.DamageInfo.WeaponDamage * 0.25f;

            victim.Health -= (int)damage;

            Debug.Log($"{victim}: У меня осталось {victim.Health}");
        }
    }

    private void Weapon_OnObstacleHit(Collider collider)
    {
        if (collider.transform.root != null) Debug.Log($"Удар по препятствию: {collider.name}");
    }


    // --- ДЕЙСТВИЯ (ОБЕРТКИ ДЛЯ ТРИГГЕРОВ) ---

    private bool IsBlockOn;
    public void HoldBlock(GuardDirection direction)
    {
        foreach (var t in ReadyTriggers) Animator.ResetTrigger(t);

        if (!IsBlockOn)
        {
            string triggerName = direction switch
            {
                GuardDirection.Up => "BlockUP",
                GuardDirection.Right => "BlockRIGHT",
                GuardDirection.Down => "BlockDOWN",
                GuardDirection.Left => "BlockLEFT",
                _ => null
            };

            if (triggerName != null)
            {
                IsReadyTriggered = false; // Если мы прервали замах блоком
                triggerReadyName = null; // очищаем замах, если прервали блоком
                Animator.ResetTrigger("UnBlock"); // Очищаем очередь от отмены блока

                Animator.SetTrigger(triggerName);
                IsBlockOn = true;
            }
        }
    }

    private void Shield_OnShieldBlocked(Weapon attackerWeapon)
    {
        string reboundTrigger = GuardDirection switch
        {
            GuardDirection.Up => "BlockUpRebound",
            GuardDirection.Right => "BlockRightRebound",
            GuardDirection.Down => "BlockDownRebound",
            GuardDirection.Left => "BlockLeftRebound",
            _ => null
        };

        if (reboundTrigger != null)
        {
            Animator.SetTrigger(reboundTrigger);
        }
    }

    public void UnBlock() 
    {
        if (IsBlockOn)
        {
            Animator.SetTrigger("UnBlock");
            IsBlockOn = false;
        }            
    }

    string triggerReadyName = null;
    public void ReadyAttack(AttackDirection direction)
    {
        foreach (var t in ReadyTriggers) Animator.ResetTrigger(t);

        triggerReadyName = direction switch
        {
            AttackDirection.Up => "ReadyAttackUP",
            AttackDirection.Right => "ReadyAttackRIGHT",
            AttackDirection.Down => "ReadyAttackDOWN",
            AttackDirection.Left => "ReadyAttackLEFT",
            _ => null
        };

        if (triggerReadyName != null)
        {
            Animator.SetTrigger(triggerReadyName);
            IsReadyTriggered = true;
        }
    }

    public void ReleaseAttack()
    {
        if (triggerReadyName == null) return; // Если замаха не было — выходим

        string triggerAttackName = triggerReadyName switch
        {
            "ReadyAttackUP" => "AttackUP",
            "ReadyAttackRIGHT" => "AttackRIGHT",
            "ReadyAttackDOWN" => "AttackDOWN",
            "ReadyAttackLEFT" => "AttackLEFT",
            _ => null
        };

        if (triggerAttackName != null)
        {
            Animator.SetTrigger(triggerAttackName);
        }

        triggerReadyName = null; // Очищаем
        IsReadyTriggered = false;
    }


    // --- АНИМАЦИОННЫЕ ИВЕНТЫ ---

    public void OnAttackStartSwing()
    {
        if (IsDead) return;

        Equipment.Weapon.StartSwing();
    }

    public void OnAttackHitFrame()
    {
        if (IsDead) return;

        if (Equipment.Weapon.CheckHit() != SwingHitResult.None)
        {
            string reboundTrigger = AttackDirection switch
            {
                AttackDirection.Up => "ReboundAttackUP",
                AttackDirection.Right => "ReboundAttackRIGHT",
                AttackDirection.Down => "ReboundAttackDOWN",
                AttackDirection.Left => "ReboundAttackLEFT",
                _ => null
            };

            if (reboundTrigger != null)
            {
                Animator.SetTrigger(reboundTrigger);
                IsReadyTriggered = false;
                triggerReadyName = null; // Очищаем при отскоке
            }
        }
    }
}