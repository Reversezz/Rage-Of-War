using UnityEngine;

public struct AttackCollisionData
{
    public BaseWarriorAgent Attacker;
    public BaseWarriorAgent Victim; // Ссылка на врага

    public Collider HitCollider; // Во что попали (объект, коллайдер)
    public Vector3 HitPoint; // Точка столкновения в мире
    public Vector3 HitNormal; // Направление удара

    public Weapon Weapon;
    public AttackDirection AttackDirection;
    public DamageInfo DamageInfo; // Информация об нанесённом уроне


    public AttackCollisionData(BaseWarriorAgent attacker, BaseWarriorAgent victim, Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal, Weapon weapon, AttackDirection attackDirection)
    {
        Attacker = attacker;
        Victim = victim;
        HitCollider = hitCollider;
        HitPoint = hitPoint;
        HitNormal = hitNormal;
        Weapon = weapon;
        AttackDirection = attackDirection;
        DamageInfo = new DamageInfo(weapon);
    }
}