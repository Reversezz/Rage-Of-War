/// <summary> Итог проверки удара на hit-кадре. </summary>
public enum SwingHitResult
{
    None,
    Obstacle,      // удар по препятствию — отскок
    Shield,        // удар отбит валидным блоком — отскок
    AgentBody      // попадание по телу — отскок
}