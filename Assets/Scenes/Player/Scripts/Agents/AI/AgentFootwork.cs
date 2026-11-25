using UnityEngine;

/// <summary>
/// «Работа ног»: решает, когда и в какую сторону агент делает боковой шаг вокруг врага.
/// Ничего не двигает сам — только выдаёт флаг Strafe (Idle / MoveLeft / MoveRight),
/// а Agent добавляет к нему подход/отход по дистанции.
/// </summary>
public sealed class AgentFootwork
{
    /// <summary>
    /// true: враг держит блок слева -> агент идёт влево (в свою левую сторону = правый бок врага).
    /// Если визуально получилось наоборот (в твоей анимации Left/Right зеркальны) — поставь false.
    /// </summary>
    //public bool StrafeTowardGuardSide = true;

    /// <summary> Что сейчас делают ноги: Idle, MoveLeft или MoveRight. </summary>
    public MovementControlFlags Strafe { get; private set; } = MovementControlFlags.Idle;

    private const float MinHoldBeforeReact = 0.35f; // не дёргаться, если враг «моргает» блоком

    private float _timeLeft;        // сколько ещё держим текущее решение
    private float _sinceDecision;   // сколько прошло с момента решения
    private GuardDirection _lastSeenGuard = GuardDirection.None;
    private MovementControlFlags _lastSide;

    public AgentFootwork()
    {
        _lastSide = Random.value < 0.5f ? MovementControlFlags.MoveLeft : MovementControlFlags.MoveRight;
    }

    public void Reset()
    {
        Strafe = MovementControlFlags.Idle;
        _timeLeft = 0f;
        _sinceDecision = 0f;
        _lastSeenGuard = GuardDirection.None;
    }

    /// <param name="canStrafe">Можно ли сейчас кружить (враг рядом, сами не в ударе).</param>
    public void Tick(float dt, CognitiveAbilities abilities, GuardDirection enemyGuard, bool canStrafe)
    {
        if (!canStrafe)
        {
            Reset();
            return;
        }

        _timeLeft -= dt;
        _sinceDecision += dt;

        bool guardChanged = enemyGuard != _lastSeenGuard;
        _lastSeenGuard = enemyGuard;

        // Держим решение, пока не вышло время. Исключение: враг сменил блок (но не чаще раза в MinHoldBeforeReact)
        bool reconsider = _timeLeft <= 0f || (guardChanged && _sinceDecision >= MinHoldBeforeReact);
        if (!reconsider) return;

        Decide(abilities, enemyGuard);
    }

    // Вердикт
    private void Decide(CognitiveAbilities abilities, GuardDirection enemyGuard)
    {
        // Восприятие: заметит ли агент, куда именно враг поставил блок
        bool readsGuard = Random.value < abilities.Perception / 100f;

        // 1. Блок сбоку — идём в открытый бок
        if (readsGuard && (enemyGuard == GuardDirection.Left || enemyGuard == GuardDirection.Right))
        {
            bool toLeft = (enemyGuard == GuardDirection.Left); // == StrafeTowardGuardSide
            SetStrafe(toLeft ? MovementControlFlags.MoveLeft : MovementControlFlags.MoveRight, Random.Range(0.8f, 1.8f));
            return;
        }

        // 2. Иначе — свободное кружение или короткая пауза. Скоординированный агент двигается чаще
        float strafeChance = Mathf.Lerp(0.25f, 0.7f, abilities.Coordination / 100f);

        if (Random.value < strafeChance)
        {
            // Чаще продолжает в ту же сторону — получается именно кружение, а не дёрганье туда-сюда
            MovementControlFlags side = Random.value < 0.65f ? _lastSide : Opposite(_lastSide);
            SetStrafe(side, Random.Range(0.7f, 1.8f));
        }
        else
        {
            SetStrafe(MovementControlFlags.Idle, Random.Range(0.4f, 1.0f));
        }
    }

    private void SetStrafe(MovementControlFlags side, float duration)
    {
        Strafe = side;
        _timeLeft = duration;
        _sinceDecision = 0f;
        if (side != MovementControlFlags.Idle) _lastSide = side;
    }

    private static MovementControlFlags Opposite(MovementControlFlags side) =>
        side == MovementControlFlags.MoveLeft ? MovementControlFlags.MoveRight : MovementControlFlags.MoveLeft;
}