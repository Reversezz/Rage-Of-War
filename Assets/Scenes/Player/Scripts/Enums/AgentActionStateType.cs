public enum AgentActionStateType
{
    Inactive = -1, // Неактивный (власть у 0-го канала)

    Windup,        // Замах
    Attack,        // Удар
    Block,         // Блок
    ReboundAttack, // Отдача после удара
    ReboundBlock   // Отдача щита
}