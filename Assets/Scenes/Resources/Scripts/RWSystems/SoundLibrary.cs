using Unity.VectorGraphics;
using UnityEngine;

public class SoundLibrary
{
    // Получаем все звуки из папок, где путь к каждой из них указан параметром path в LoadAll
    public static AudioClip[] Footsteps => Resources.LoadAll<AudioClip>("Sounds/Footsteps");
    public static AudioClip[] DeathScreams => Resources.LoadAll<AudioClip>("Sounds/DeathScreams");
    public static AudioClip[] FallsToGround => Resources.LoadAll<AudioClip>("Sounds/FallToGround");
    
    public static AudioClip[] SwordSlashes => Resources.LoadAll<AudioClip>("Sounds/SwordSlashes");
    public static AudioClip[] ShieldBlocks => Resources.LoadAll<AudioClip>("Sounds/ShieldBlock");

    /// <summary> Возвращает рандомный звук из выбранного массива звуков </summary>
    public static AudioClip GetRandom(AudioClip[] clips)
    {
        if (clips != null)
        {
            if (clips.Length == 1) return clips[0];
            else if (clips.Length > 1) return clips[Random.Range(0, clips.Length)];
        }
        return null;
    }         
}
