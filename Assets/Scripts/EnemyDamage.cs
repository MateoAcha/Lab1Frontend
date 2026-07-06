using UnityEngine;

public static class EnemyDamage
{
    public const float Multiplier = 5f;

    public static float Amount(float baseDamage)
    {
        return Mathf.Max(0f, baseDamage) * Multiplier;
    }

    public static int AmountInt(int baseDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(Amount(baseDamage)));
    }
}
