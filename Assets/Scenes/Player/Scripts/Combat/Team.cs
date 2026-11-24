using UnityEngine;

public class Team
{
    public Color TeamColor { get; }

    public Team(Material material)
    {
        TeamColor = material.color;
    }

    public static explicit operator Team(Material material) => new Team(material);


    // Сравнение по цвету материала на воине
    public static bool operator ==(Team a, Team b) => a?.TeamColor == b?.TeamColor;
    public static bool operator !=(Team a, Team b) => !(a == b);
    

    public override bool Equals(object obj) => obj is Team other && this == other;
    public override int GetHashCode() => TeamColor.GetHashCode();
    
}