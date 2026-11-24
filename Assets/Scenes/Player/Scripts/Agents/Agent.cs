using System;
using System.Reflection.Metadata.Ecma335;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class Agent : BaseWarriorAgent
{
    [Header("Компоненты")]
    public CharacterController controller;

    [Header("Цель")]
    public BaseWarriorAgent TargetAgent;

    [Header("Настройки Агента")]
    public float MoveSpeed = 1.5f;
    public float SprintSpeed = 4f;
    public float JumpHeight = 1.5f;
    public float SpeedChangeRate = 10f;
    public float AnimSmoothTime = 0.12f;

    private Vector3 _smoothedMoveWorld;
    private Vector3 _smoothedMove;
    private float _currentSpeed;
    private float _targetSpeed;

    [Header("Настройки ИИ")]
    [Tooltip("Расстояние между агентами в стычке")]
    public float DistanceBetweenTheAgents = 1.66f;
    [Tooltip("Радиус, в котором агент замахивается (Ready)")]
    public float AwarenessDistance = 2.52f;
    [Tooltip("Радиус, в котором агент точно достанет до врага (Attack)")]
    public float MinAttackDistance = 1.58f;

    [Header("Реакция и защита")]
    [Tooltip("Талант агента (0..60), немного улучшает реакцию")]
    [SerializeField, Range(0, 60)] private int _talentBonus = 0;
    [Tooltip("Сколько секунд угрозы нет, прежде чем опустить блок. Защита от «моргания» состояний аниматора")]
    [SerializeField] private float _blockReleaseDelay = 0.12f;
    [SerializeField] private bool _debugDefense;

    [Header("Команда")]
    [SerializeField] private Material _team;

    [Header("Снаряжение")]
    [SerializeField] private Equipment _equipment;

    public override Team Team => (Team)_team;
    public override Equipment Equipment => _equipment;

    private RayCast _rayCast;

    protected override void Awake()
    {
        _equipment = GetComponent<Equipment>();
        base.Awake();

        Die += OnDie;

        // Характеристики нужно создавать явно: до этого свойство было нулевой структурой
        CognitiveAbilities = new CognitiveAbilities(_talentBonus);
        _attackCooldown = RollAttackCooldown();

        _rayCast = GetComponent<RayCast>();
        _rayCast.OnRaycastHit += Agent_OnRaycastHit;
    }

    protected override void OnDie()
    {
        Debug.Log($"{name}: попал в OnDie() в Agent");

        base.OnDie();
        if (TargetAgent != null) TargetAgent = null;        
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_rayCast != null) _rayCast.OnRaycastHit -= Agent_OnRaycastHit;
    }

    // При попадании луча во что-либо. Если агент что-то увидел
    private void Agent_OnRaycastHit(RaycastHit obj)
    {
        if (!obj.transform.root.TryGetComponent(out BaseWarriorAgent agent)) return;
        if (agent == this || agent.IsDead || this.Team == agent.Team) return;

        if (TargetAgent == null)
        {
            TargetAgent = agent;
            return;
        }

        // Новый враг ближе — переключаемся (sqrMagnitude дешевле magnitude)
        float current = (TargetAgent.Position - Position).sqrMagnitude;
        float other = (agent.Position - Position).sqrMagnitude;

        if (other < current) TargetAgent = agent;
    }

    private static readonly AttackDirection[] AttackDirections = { AttackDirection.Up, AttackDirection.Right, AttackDirection.Down, AttackDirection.Left };
    private static readonly GuardDirection[] GuardDirections = { GuardDirection.Up, GuardDirection.Right, GuardDirection.Down, GuardDirection.Left };

    private AttackDirection _validAttackDirection = AttackDirection.None;
    private GuardDirection _validBlockDirection = GuardDirection.None;

    protected override void OnTick(float dt)
    {
        base.OnTick(dt);

        AgentGravity.AgentGravityUpdate(dt);

        Movement(dt, out float distance);

        if (TargetAgent == null) return;

        if (TargetAgent.IsDead)
        {
            LoseTarget();
            return;
        }

        UpdateMovementFlags(distance);
        UpdateDefense(dt, distance);
        UpdateOffense(dt, distance);
    }

    private void LoseTarget()
    {
        TargetAgent = null;
        EndDefense();                           // опустить блок, сбросить решения защиты
        StartAttackCooldown();
        if (IsReadyTriggered) ReleaseAttack();  // не стоять вечно в замахе (или добавить в аниматор триггер отмены замаха)
    }

    // =====================================================================
    //  ДВИЖЕНИЕ
    // =====================================================================

    private void UpdateMovementFlags(float distance)
    {
        if (distance >= 7f)                            MovementControlFlags |= MovementControlFlags.RunForward;   // далеко
        else if (distance > DistanceBetweenTheAgents)  MovementControlFlags |= MovementControlFlags.MoveForward;  // идём
        else if (distance >= MinAttackDistance)        MovementControlFlags |= MovementControlFlags.Idle;         // оптимально
        else                                           MovementControlFlags |= MovementControlFlags.MoveBackward; // слишком близко
    }

    protected override void Movement(float dt, out float distance)
    {
        if (TargetAgent == null)
        {
            distance = 0f;
            MoveAgentLogic(dt, Vector3.zero, false); // чтобы гравитация реально двигала контроллер
            return;
        }

        Vector3 toTarget = TargetAgent.transform.position - transform.position;
        toTarget.y = 0f;

        distance = toTarget.magnitude;
        Vector3 direction = distance > 0.0001f ? toTarget / distance : Vector3.zero;

        if (toTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, dt * 10f);
        }

        Vector3 desiredMove = Vector3.zero;
        bool isSprint = false;

        if (MovementControlFlags.HasFlag(MovementControlFlags.RunForward))
        {
            desiredMove = direction;
            isSprint = true;
            MovementControlFlags &= ~MovementControlFlags.RunForward;
        }
        else if (MovementControlFlags.HasFlag(MovementControlFlags.MoveForward))
        {
            desiredMove = direction;
            MovementControlFlags &= ~MovementControlFlags.MoveForward;
        }
        else if (MovementControlFlags.HasFlag(MovementControlFlags.MoveBackward))
        {
            desiredMove = -direction;
            MovementControlFlags &= ~MovementControlFlags.MoveBackward;
        }

        MovementControlFlags &= ~MovementControlFlags.Idle;

        MoveAgentLogic(dt, desiredMove, isSprint);
    }

    private void MoveAgentLogic(float dt, Vector3 desiredWorldMove, bool isSprint)
    {
        _smoothedMoveWorld = Vector3.Lerp(_smoothedMoveWorld, desiredWorldMove, dt / AnimSmoothTime);

        _targetSpeed = _smoothedMoveWorld.magnitude * (isSprint ? SprintSpeed : MoveSpeed);
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, dt * SpeedChangeRate);

        Vector3 horizontalMove = _smoothedMoveWorld * _currentSpeed;
        controller.Move((horizontalMove + Vector3.up * AgentGravity.AgentVelocity.y) * dt);

        Vector3 localMove = transform.InverseTransformDirection(_smoothedMoveWorld);

        Animator.SetFloat("Velocity X", localMove.x * _currentSpeed);
        Animator.SetFloat("Velocity Z", localMove.z * _currentSpeed);
    }

    // =====================================================================
    //  ЗАЩИТА: «заметил -> подумал -> решил -> (возможно) ошибся»
    // =====================================================================

    private enum DefenseDecision
    {
        None,            // ещё не решил (думает или не заметил)
        BlockCorrect,    // блок в правильную сторону
        BlockWrong,      // перепутал направление
        IgnoreAndAttack, // решил, что успеет ударить первым
        TooLate,         // уже замахнулся/бьёт — блок невозможен
        GaveUp           // поставил блок, но не выдержал и опустил его
    }

    private DefenseDecision _decision = DefenseDecision.None;
    private AttackDirection _observedEnemyDir = AttackDirection.None;
    private float _reactionTimer;
    private bool _readsWindup;
    private float _blockHeldTime;
    private float _blockPatience;
    private float _noThreatTime;

    private bool IsDefending => _decision == DefenseDecision.BlockCorrect || _decision == DefenseDecision.BlockWrong;

    private void UpdateDefense(float dt, float distance)
    {
        AttackDirection enemyDir = TargetAgent.AttackDirection;
        bool threat = enemyDir != AttackDirection.None && distance <= AwarenessDistance;

        // --- Угрозы нет: сбрасываем состояние с небольшой задержкой ---
        if (!threat)
        {
            _noThreatTime += dt;
            if (_noThreatTime >= _blockReleaseDelay) EndDefense();
            return;
        }

        _noThreatTime = 0f;

        // --- Новая атака врага (или повторная после паузы) ---
        if (enemyDir != _observedEnemyDir) BeginObservation(enemyDir);

        bool enemyCommitted = TargetAgent.CurrentActionState == AgentActionStateType.Attack;

        if (_decision == DefenseDecision.None)
        {
            // Не читает замах — «просыпается» только когда удар уже пошёл
            if (!_readsWindup && !enemyCommitted) return;

            _reactionTimer -= dt;
            if (_reactionTimer > 0f) return;

            MakeDecision(enemyDir);
        }
        else if (IsDefending)
        {
            _blockHeldTime += dt;

            // Нетерпеливый агент опускает щит, пока враг ещё в замахе
            if (!enemyCommitted && _blockHeldTime >= _blockPatience)
            {
                UnBlock();
                _decision = DefenseDecision.GaveUp;
                Log("потерял терпение, опустил блок");
            }
        }
    }

    private void BeginObservation(AttackDirection enemyDir)
    {
        _observedEnemyDir = enemyDir;
        _decision = DefenseDecision.None;
        _reactionTimer = RollReactionTime();
        _readsWindup = RollReadsWindup();
        _blockHeldTime = 0f;

        Log($"заметил атаку {enemyDir}: реакция {_reactionTimer:F2}с, читает замах: {_readsWindup}");
    }

    private void MakeDecision(AttackDirection enemyDir)
    {
        // Сам уже в ударе — блок поставить физически нельзя, обмен ударами
        if (this.InAttackAction())
        {
            _decision = DefenseDecision.TooLate;
            Log("решение: слишком поздно (сам в атаке)");
            return;
        }

        if (Random.value < CounterGambleChance)
        {
            _decision = DefenseDecision.IgnoreAndAttack;
            Log("решение: рискнуть и ударить первым");
            return;
        }

        GuardDirection guard = enemyDir.GetRelevantBlockDirectionFromAttackDirection();

        if (Random.value < DirectionMisreadChance)
        {
            guard = RandomOtherGuard(guard);
            _decision = DefenseDecision.BlockWrong;
        }
        else
        {
            _decision = DefenseDecision.BlockCorrect;
        }

        _blockHeldTime = 0f;
        _blockPatience = RollBlockPatience();
        TryGetDirectionToBlock(guard);

        Log($"решение: {_decision}, блок {guard}, терпение {_blockPatience:F2}с");
    }

    private void EndDefense()
    {
        _observedEnemyDir = AttackDirection.None;
        _decision = DefenseDecision.None;
        _reactionTimer = 0f;
        _blockHeldTime = 0f;
        UnBlock(); // внутри уже есть проверка IsBlockOn
    }

    private static GuardDirection RandomOtherGuard(GuardDirection correct)
    {
        GuardDirection result;
        do { result = GuardDirections[Random.Range(0, GuardDirections.Length)]; }
        while (result == correct);
        return result;
    }

    private void TryGetDirectionToBlock(GuardDirection dir)
    {
        _validBlockDirection = dir;

        // Уже в блоке, но смотрит не туда — переключаем
        if (this.InBlockAction() || this.InReboundFromBlockAction())
        {
            if (GuardDirection != dir)
            {
                UnBlock();
                HoldBlock(dir);
            }
        }
        else
        {
            HoldBlock(dir);
        }
    }

    /// <summary> Читает ли агент направление уже по замаху (иначе — только когда удар пошёл). </summary>
    public bool RollReadsWindup() => Random.value < CognitiveAbilities.Perception / 100f;

    /// <summary>
    /// Сколько секунд агент готов держать блок, пока враг ещё в замахе.
    /// Опытные (высокое хладнокровие) держат до конца атаки — Infinity.
    /// </summary>
    public float RollBlockPatience()
    {
        if (CognitiveAbilities.Composure >= 0.7f) return float.PositiveInfinity;
        return Mathf.Lerp(3.2f, 6.2f, CognitiveAbilities.Composure / 0.7f) * Random.Range(0.8f, 1.2f);
    }

    /// <summary>
    /// Время (сек), через которое агент ОТРЕАГИРУЕТ на замечённую угрозу. <br/>
    /// Реакция задаёт среднее, координация — разброс. <br/>
    /// Реальный потолок: длительность Attack-анимации до hit-кадра минус время перехода в Block.
    /// </summary>
    public float RollReactionTime()
    {
        float baseTime = Mathf.Lerp(0.55f, 0.12f, CognitiveAbilities.Reaction / 100f);
        float jitter = Mathf.Lerp(0.30f, 0.08f, CognitiveAbilities.Coordination / 100f);
        return Mathf.Max(0.05f, baseTime * Random.Range(1f - jitter, 1f + jitter));
    }

    /// <summary> Шанс перепутать направление и поставить блок не туда. </summary>
    public float DirectionMisreadChance => Mathf.Lerp(0.20f, 0.02f, CognitiveAbilities.Perception / 100f);

    /// <summary> Шанс решить «успею ударить первым» вместо блока. </summary>
    public float CounterGambleChance => Mathf.Lerp(0.35f, 0.03f, CognitiveAbilities.Composure);

    private void Log(string message)
    {
        if (_debugDefense) Debug.Log($"[{name}] {message}");
    }

    // =====================================================================
    //  АТАКА
    // =====================================================================

    private enum AttackDecision
    {
        Cooldown,   // «выдыхает»: после удара или блока новых замахов нет
        Evaluating, // кд прошёл, ждёт удобного момента
        Windup      // замах начат, ждёт дистанции для удара
                    // сюда позже: WindupChange (смена направления в замахе), GaveUp (снять замах ради блока)
    }

    private AttackDecision _attackDecision = AttackDecision.Cooldown;
    private float _attackCooldown;      // сколько ещё выдерживать паузу
    private float _evaluateTime;        // сколько уже ждёт удобного момента
    private float _maxWaitForOpening;   // потом ударит и без «окна»

    private bool IsAttackBusy => this.InReadyAttackAction() || this.InAttackAction() || this.InReboundFromAttackAction();

    private void UpdateOffense(float dt, float distance)
    {
        // Защищается — замах уже сброшен в HoldBlock. После блока: короткая пауза, потом оценка момента
        if (IsDefending)
        {
            if (_attackDecision != AttackDecision.Cooldown) StartAttackCooldown(0.3f);
            return;
        }

        // «Успею ударить первым» — идёт мимо кд и мимо оценки момента
        bool rushed = _decision == DefenseDecision.IgnoreAndAttack;
        if (rushed && _attackDecision != AttackDecision.Windup && !IsAttackBusy && distance <= AwarenessDistance)
        {
            if (TryGetDirectionToReadyAttack(ChooseAttackDirection()))
                _attackDecision = AttackDecision.Windup;
        }

        switch (_attackDecision)
        {
            case AttackDecision.Cooldown:
                // Кд тикает только когда агент не в замахе, ударе или отскоке
                if (!IsAttackBusy) _attackCooldown -= dt;
                if (_attackCooldown <= 0f) BeginEvaluating();
                break;

            case AttackDecision.Evaluating:
                {
                    _evaluateTime += dt;
                    if (distance > AwarenessDistance || IsAttackBusy) break;

                    AgentActionStateType enemy = TargetAgent.CurrentActionState;

                    bool enemyAttacking = enemy == AgentActionStateType.Windup || enemy == AgentActionStateType.Attack;

                    bool opening = enemy == AgentActionStateType.Inactive
                                || enemy == AgentActionStateType.ReboundAttack  // его удар отбит — лучший момент для контратаки
                                || enemy == AgentActionStateType.ReboundBlock;

                    // Враг атакует — это дело защиты. Враг в блоке — ждём, пока не кончится терпение
                    if (!enemyAttacking && (opening || _evaluateTime >= _maxWaitForOpening))
                    {
                        if (TryGetDirectionToReadyAttack(ChooseAttackDirection()))
                        {
                            _attackDecision = AttackDecision.Windup;
                            Log($"атака: замах {_validAttackDirection} (ждал {_evaluateTime:F2}с)");
                        }
                    }
                    break;
                }

            case AttackDecision.Windup:
                if (!IsReadyTriggered)
                {
                    // Замах сорван (блок или отскок)
                    StartAttackCooldown();
                    break;
                }

                if (this.InReadyAttackAction() && (rushed || distance <= DistanceBetweenTheAgents))
                {
                    ReleaseAttack();
                    StartAttackCooldown();
                }
                break;
        }
    }

    private void BeginEvaluating()
    {
        _attackDecision = AttackDecision.Evaluating;
        _evaluateTime = 0f;
        _maxWaitForOpening = RollMaxWaitForOpening();
    }

    private void StartAttackCooldown(float scale = 1f)
    {
        _attackDecision = AttackDecision.Cooldown;
        _attackCooldown = RollAttackCooldown() * scale;
    }

    /// <summary> Умный агент не бьёт туда, где у врага стоит блок (шанс зависит от восприятия). </summary>
    private AttackDirection ChooseAttackDirection()
    {
        GuardDirection enemyGuard = TargetAgent.GuardDirection;

        bool avoidGuard = enemyGuard != GuardDirection.None && Random.value < CognitiveAbilities.Perception / 100f;

        int start = Random.Range(0, AttackDirections.Length);
        for (int i = 0; i < AttackDirections.Length; i++)
        {
            AttackDirection dir = AttackDirections[(start + i) % AttackDirections.Length];
            if (!avoidGuard || dir.GetRelevantBlockDirectionFromAttackDirection() != enemyGuard)
                return dir;
        }
        return AttackDirections[start];
    }

    /// <summary> Пауза между атаками: хладнокровный выбирает момент дольше, торопыга бьёт чаще. </summary>
    private float RollAttackCooldown() => Mathf.Lerp(0.4f, 1.6f, CognitiveAbilities.Composure) * Random.Range(0.7f, 1.3f);

    /// <summary> Сколько агент готов ждать «окна», прежде чем ударить без него. </summary>
    private float RollMaxWaitForOpening() => Mathf.Lerp(0.6f, 2.5f, CognitiveAbilities.Composure) * Random.Range(0.8f, 1.2f);

    /// <summary>
    /// Возвращает true, только если замах реально начался (иначе AttackDecision не меняем).
    /// Защита от багов: новый триггер при уже нажатом, повторный замах, уже выбранная директория.
    /// </summary>
    private bool TryGetDirectionToReadyAttack(AttackDirection direction)
    {
        if (IsReadyTriggered) return false;
        if (CurrentActionState == AgentActionStateType.Windup) return false;
        if (AttackDirection != AttackDirection.None) return false;

        _validAttackDirection = direction;
        ReadyAttack(direction);
        return IsReadyTriggered; // ReadyAttack ставит флаг, только если триггер валиден
    }
}
