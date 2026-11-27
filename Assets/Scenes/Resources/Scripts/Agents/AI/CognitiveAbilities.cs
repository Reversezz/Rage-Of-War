using UnityEngine;

/// <summary>
/// Нейронные характеристики агента.
/// Из них считаются «человеческие» параметры боя: время реакции, шанс ошибиться в направлении,
/// склонность рискнуть и контратаковать вместо блока, терпение при удержании блока.
/// </summary>
public readonly struct CognitiveAbilities
{
    /// <summary> Восприятие </summary>
    public int Perception { get; }

    /// <summary> Хладнокровие (выдержка и контроль решений) </summary>
    public float Composure { get; }

    /// <summary> Координация </summary>
    public int Coordination { get; }

    /// <summary> Скорость обработки информации </summary>
    public int InfoProcessingSpeed { get; }

    /// <summary> Скорость передачи сигнала в мышцы </summary>
    public int RateToSignalTransmissionInMuscles { get; }

    /// <summary> Реакция (0..100) </summary>
    public int Reaction { get; }


    // ВАЖНО: параметра по умолчанию нет намеренно.
    // `new AgentCharacteristics()` для структуры создаёт нулевой экземпляр и НЕ вызывает этот конструктор.
    public CognitiveAbilities(int talentBonus = 0)
    {
        float talent = Mathf.Clamp(talentBonus, 0, 60) / 1.2f;

        int perception = Random.Range(30, 90);
        int coordination = Random.Range(40, 80);        
        int infoSpeed = Random.Range(30, 80);
        float composure = (coordination + infoSpeed) / 200f;
        int signalRate = Random.Range(20, 40) + (int)(infoSpeed * 0.33f) + (int)(perception * 0.52f);

        Perception = perception;        
        Coordination = coordination;
        Composure = composure;
        InfoProcessingSpeed = infoSpeed;
        RateToSignalTransmissionInMuscles = signalRate;

        Reaction = Mathf.Clamp((int)((perception + coordination + infoSpeed + signalRate) / 4f + talent * 0.5f), 0, 100);
    }    
}