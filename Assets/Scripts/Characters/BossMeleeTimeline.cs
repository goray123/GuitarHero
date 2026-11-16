// Seconds, independent of animation clips. Only windup is accelerated in phase two.
public sealed class BossMeleeTimeline
{
    public enum Stage { Idle, Windup, Active, Recovery }
    private static readonly float[] Windups = { 0.65f, 0.75f, 0.85f };
    private static readonly float[] Actives = { 0.15f, 0.15f, 0.20f };
    private static readonly float[] Recoveries = { 0.35f, 0.35f, 0.55f };
    public int Attack { get; private set; }
    public float Elapsed { get; private set; }
    public float Windup { get; private set; }
    public float Active => Attack == 0 ? 0f : Actives[Attack - 1];
    public float Recovery => Attack == 0 ? 0f : Recoveries[Attack - 1];
    public float Total => Windup + Active + Recovery;
    public Stage CurrentStage => Attack == 0 ? Stage.Idle :
        Elapsed < Windup ? Stage.Windup : Elapsed < Windup + Active ? Stage.Active : Stage.Recovery;
    public float StageProgress => Attack == 0 ? 0f : CurrentStage == Stage.Windup ? Elapsed / Windup :
        CurrentStage == Stage.Active ? (Elapsed - Windup) / Active : (Elapsed - Windup - Active) / Recovery;

    public void Begin(int attack, float windupMultiplier = 1f)
    {
        if (attack < 1 || attack > 3) throw new System.ArgumentOutOfRangeException(nameof(attack));
        Attack = attack;
        Elapsed = 0f;
        Windup = Windups[attack - 1] * System.Math.Max(0.01f, windupMultiplier);
    }

    public bool Advance(float seconds)
    {
        if (Attack == 0) return false;
        Elapsed += System.Math.Max(0f, seconds);
        return Elapsed + 0.00001f >= Total;
    }

    public void Stop() { Attack = 0; Elapsed = 0f; Windup = 0f; }

    public static float SequenceDangerDuration(float multiplier)
    {
        float time = 0f;
        for (int i = 0; i < 3; i++) time += Windups[i] * multiplier + Actives[i] + (i < 2 ? Recoveries[i] : 0f);
        return time;
    }

    public float RemainingSequenceDanger(float nextWindupMultiplier)
    {
        if (Attack == 0) return 0f;
        float dangerEnd = Windup + Active - Elapsed;
        float nextStart = Total - Elapsed;
        for (int i = Attack; i < 3; i++)
        {
            dangerEnd = nextStart + Windups[i] * nextWindupMultiplier + Actives[i];
            nextStart = dangerEnd + Recoveries[i];
        }
        return System.Math.Max(0f, dangerEnd);
    }
}
