using UnityEngine;

public struct DamageInfo
{
    public DamageInfo(Weapon weapon)
    {
        WeaponClass = weapon.WeaponClass;
        DamageType = weapon.DamageType;
        WeaponDamage = weapon.WeaponDamage;
    }

    public WeaponClass WeaponClass; // Sword, Axe
    public DamageType DamageType; // Cut, Pierce, Blunt    
    public int WeaponDamage; // Количество урона
}