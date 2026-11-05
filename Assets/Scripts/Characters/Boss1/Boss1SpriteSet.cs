using UnityEngine;

public enum Boss1Pose { Idle, Walk, LowSweep, MidPunch, OverheadSlam, BoosterDash, RearMissile, MissileBarrage, Hit, Death, MissileFlight, MissileExplosion }

[CreateAssetMenu(menuName = "Guitar Hero/Boss 1 Sprite Set")]
public sealed class Boss1SpriteSet : ScriptableObject
{
    public Sprite[] idle;
    public Sprite[] walk;
    public Sprite[] lowSweep;
    public Sprite[] midPunch;
    public Sprite[] overheadSlam;
    public Sprite[] boosterDash;
    public Sprite[] rearMissile;
    public Sprite[] missileBarrage;
    public Sprite[] hit;
    public Sprite[] death;
    public Sprite[] missileFlight;
    public Sprite[] missileExplosion;

    public Sprite[] Frames(Boss1Pose pose)
    {
        switch (pose)
        {
            case Boss1Pose.Walk: return walk;
            case Boss1Pose.LowSweep: return lowSweep;
            case Boss1Pose.MidPunch: return midPunch;
            case Boss1Pose.OverheadSlam: return overheadSlam;
            case Boss1Pose.BoosterDash: return boosterDash;
            case Boss1Pose.RearMissile: return rearMissile;
            case Boss1Pose.MissileBarrage: return missileBarrage;
            case Boss1Pose.Hit: return hit;
            case Boss1Pose.Death: return death;
            case Boss1Pose.MissileFlight: return missileFlight;
            case Boss1Pose.MissileExplosion: return missileExplosion;
            default: return idle;
        }
    }

    public Sprite Sample(Boss1Pose pose, int first, int last, float progress)
    {
        Sprite[] frames = Frames(pose);
        if (frames == null || frames.Length == 0) return null;
        first = Mathf.Clamp(first, 0, frames.Length - 1);
        last = Mathf.Clamp(last, first, frames.Length - 1);
        int index = Mathf.Min(last, first + Mathf.FloorToInt(Mathf.Clamp01(progress) * (last - first + 1)));
        return frames[index];
    }
}

