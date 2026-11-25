using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BaseAgent : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnTick += OnTick;
            BattleManager.Instance.OnLateTick += OnLateTick;
        }
        else
        {
            throw new Exception($"Агент {name} НЕ СМОГ подписаться: BattleManager.Instance равен null!");
        }
        
        // Добавляем агента в список тех, кто на карте (при спавне)
        _agentsList?.Add(this);
    }

    

    protected virtual void OnDisable()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnTick -= OnTick;
            BattleManager.Instance.OnLateTick -= OnLateTick;
        }

        // Удаляем агента из списка тех, кто на карте (при смерти)
        _agentsList?.Remove(this);
    }


    // - - -  ЖИЗНЬ И СМЕРТЬ  - - -
    public event Action Die;
    public event Action Dead;

    /// <summary> Проверяет, жив ли агент </summary>
    public bool IsAlive => AgentState == AgentState.Active;    

    /// <summary> Проверяет, мёртв ли агент </summary>
    public bool IsDead => AgentState == AgentState.Killed;    
    
    // Обработка агента после смерти (конец анимации ивент): отключение коллайдеров
    protected void OnDeath()
    {
        // Для отключения коллайдеров уже когда персонаж упал
        Dead?.Invoke();
    }

    


    protected virtual void Awake()
    {
        if (TryGetComponent(out Gravity agentGravity))
        {
            AgentGravity = agentGravity;
        }
        else throw new Exception($"Агент {name} НЕ СМОГ получить компонент Gravity!");

        AgentState = AgentState.Active;
        Health = 200;
    }

    // Список агентов
    private static readonly List<BaseAgent> _agentsList = new();
    public List<BaseAgent> AllAgents => _agentsList;

    public List<BaseAgent> GetActiveAgents => _agentsList.Where(agent => agent.AgentState == AgentState.Active).ToList<BaseAgent>();
    public List<BaseAgent> GetDeadAgents => _agentsList.Where(agent => agent.AgentState == AgentState.Killed).ToList<BaseAgent>();


    // Аниматор нужен базовому классу для вызова триггеров
    protected Animator Animator;


    // Базовые наследуемые свойства
    public Gravity AgentGravity { get; protected set; }    
    public AgentState AgentState { get; protected set; }

    
    public int Health { get; protected set; } 

    /// <summary>
    /// Вызывается каждый тик (Time.deltaTime)
    /// </summary>
    /// <param name="dt"></param>
    protected virtual void OnTick(float dt)
    {
        // Если мёртв, скипаем в зародыше всю логику агента
        if (IsDead) return;

        // Постоянно проверяем что у агента хп больше 0
        if (this.Health <= 0)
        {
            AgentState = AgentState.Killed;
            Animator.SetBool("IsDead", true);
            Animator.SetLayerWeight(1, 0f);

            Die?.Invoke();
            return;
        }        
    }

    /// <summary>
    /// Вызывается каждый тик (Time.deltaTime) после OnTick()
    /// </summary>
    /// <param name="dt"></param>
    protected virtual void OnLateTick(float dt) 
    { }

    protected virtual void Movement(float dt) { }

    // AI версия
    protected virtual void Movement(float dt, out float distanceToTarget) { distanceToTarget = 0f; }
}