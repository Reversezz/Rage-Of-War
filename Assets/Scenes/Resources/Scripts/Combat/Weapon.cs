using System;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponType WeaponType;
    public WeaponClass WeaponClass;

    [Header("Настройки удара")]
    [Tooltip("Слои, по которым бьет оружие (Agent, Weapon, Obstacle)")]
    public LayerMask HitLayers;

    // Типы урона в зависимости от типа оружия
    public DamageType DamageType => WeaponType switch
    {
        WeaponType.OneHanded => DamageType.Cut,
        WeaponType.TwoHanded => DamageType.Cut,
        WeaponType.Polearm => DamageType.Pierce,
        WeaponType.WoodShield => DamageType.Blunt,
        _ => DamageType.Invalid,
    };

    public int WeaponDamage => WeaponClass switch
    {
        WeaponClass.Sword => 40,
        WeaponClass.Axe => 50,
        _ => 0,
    };

    public event Action<AttackCollisionData, bool> OnWeaponHit;
    public event Action<Weapon> OnShieldBlocked;
    public event Action<Collider> OnObstacleHit;


    private bool _hasHitThisSwing = false;

    // Первый Animation Event удара
    public void StartSwing()
    {
        _hasHitThisSwing = false;
    }


    // Animation Event на кадре удара
    public SwingHitResult CheckHit()
    {
        if (WeaponClass == WeaponClass.Shield) return SwingHitResult.None;
        if (_hasHitThisSwing) return SwingHitResult.None;
        if (!TryGetComponent(out Collider weaponCollider)) return SwingHitResult.None;

        Collider[] hitColliders = weaponCollider.GetHitColliders(HitLayers);

        // Порядок в массиве Unity не гарантирует ничего, поэтому сначала собираем, потом выбираем по приоритету
        Collider shield = null, agent = null, obstacle = null;

        foreach (var hit in hitColliders)
        {
            if (hit.transform.root == transform.root) continue; // себя игнорируем

            if      (hit.CompareTag("Shield"))   { if (shield == null) shield = hit; }
            else if (hit.CompareTag("Agent"))    { if (agent == null) agent = hit; }
            else if (hit.CompareTag("Obstacle")) { if (obstacle == null) obstacle = hit; }
        }

        // Щит работает, только если блок РЕАЛЬНО поднят
        if (shield != null)
        {
            _hasHitThisSwing = true;

            // Если попали по щиту и агент действительно в блоке
            if (shield.TryGetComponent(out Weapon enemyShield))
            {
                // Если противник был в блоке 
                if (WasInBlock(shield))
                {
                    if (IsValidBlockDirection(shield))
                    {
                        enemyShield.ReceiveBlock(this);
                        return SwingHitResult.Shield;
                    }
                    else
                    {                        
                        // Незначительный урон из-за неправильно поставленного блока
                        OnWeaponHit?.Invoke(CreateData(shield), true);
                        
                        enemyShield.ReceiveBlock(this);
                        return SwingHitResult.Shield;
                    }
                }
                // Если противник не был в блоке, но мы всё равно попали по щиту.
                // Но визуально большая часть анимации явно задела агента,
                // так что полное отсутствие урона недопустимо
                else
                {
                    OnWeaponHit?.Invoke(CreateData(shield), false);
                    return SwingHitResult.AgentBody;
                }                
            }

            // OnWeaponHit для заблокированного удара больше не вызывается:
            // иначе будущий TakeDamage в обработчике нанесёт урон сквозь блок.            
        }

        // Попал в тело, наносится урон
        if (agent != null)
        {                
            _hasHitThisSwing = true;
            OnWeaponHit?.Invoke(CreateData(agent), false);
            return SwingHitResult.AgentBody;
        }

        // Попал в препятствие
        if (obstacle != null)
        {
            _hasHitThisSwing = true;
            OnObstacleHit?.Invoke(obstacle);
            return SwingHitResult.Obstacle;
        }

        return SwingHitResult.None;
    }

    private bool WasInBlock(Collider shieldCollider)
    {
        var victim = shieldCollider.transform.root.GetComponent<BaseWarriorAgent>();
        var attacker = transform.root.GetComponent<BaseWarriorAgent>();
        if (victim == null || attacker == null) return false;

        return victim.CurrentActionState == AgentActionStateType.Block;
    }

    private bool IsValidBlockDirection(Collider shieldCollider)
    {
        var victim = shieldCollider.transform.root.GetComponent<BaseWarriorAgent>();
        var attacker = transform.root.GetComponent<BaseWarriorAgent>();
        if (victim == null || attacker == null) return false;

        return victim.GuardDirection == attacker.AttackDirection.GetRelevantBlockDirectionFromAttackDirection();
    }

    private AttackCollisionData CreateData(Collider hit)
    {
        return new AttackCollisionData(
            attacker: this.transform.root.GetComponent<BaseWarriorAgent>(),
            victim: hit.transform.root.GetComponent<BaseWarriorAgent>(),
            hitCollider: hit,
            hitPoint: hit.ClosestPoint(transform.position),
            hitNormal: (transform.position - hit.transform.position).normalized,
            attackDirection: ((BaseWarriorAgent)transform.root).AttackDirection,
            weapon: this
        );
    }

    // Вызывается атакующим мечом
    private void ReceiveBlock(Weapon attackerWeapon)
    {
        OnShieldBlocked?.Invoke(attackerWeapon);
    }
}