using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public BaseWarriorAgent agent;
    private void Start()
    {
        if (agent == null) agent = GetComponent<BaseWarriorAgent>();
    }


    private void OnFootstep()
    {
        AudioClip clip = SoundLibrary.GetRandom(SoundLibrary.Footsteps);
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, agent.Position, Random.Range(0.3f, 0.5f));
    }

    private void OnDeathScream()
    {
        AudioClip clip = SoundLibrary.GetRandom(SoundLibrary.DeathScreams);
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, agent.Position, volume: 0.3f);
    }

    private void OnFallToGround()
    {
        AudioClip clip = SoundLibrary.GetRandom(SoundLibrary.FallsToGround);
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, agent.Position, volume: 0.76f);
    }
}