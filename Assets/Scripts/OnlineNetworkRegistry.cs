using System.Collections.Generic;

public static class OnlineNetworkRegistry
{
    public static readonly List<EnemyController> MeleeEnemies = new List<EnemyController>();
    public static readonly List<RangedEnemyController> RangedEnemies = new List<RangedEnemyController>();
    public static readonly List<EnemyProjectile> Projectiles = new List<EnemyProjectile>();
    public static readonly List<PlayerProjectile> PlayerProjectiles = new List<PlayerProjectile>();

    public static void Register(EnemyController enemy)
    {
        if (enemy != null && !MeleeEnemies.Contains(enemy))
            MeleeEnemies.Add(enemy);
    }

    public static void Unregister(EnemyController enemy)
    {
        MeleeEnemies.Remove(enemy);
    }

    public static void Register(RangedEnemyController enemy)
    {
        if (enemy != null && !RangedEnemies.Contains(enemy))
            RangedEnemies.Add(enemy);
    }

    public static void Unregister(RangedEnemyController enemy)
    {
        RangedEnemies.Remove(enemy);
    }

    public static void Register(EnemyProjectile projectile)
    {
        if (projectile != null && !Projectiles.Contains(projectile))
            Projectiles.Add(projectile);
    }

    public static void Unregister(EnemyProjectile projectile)
    {
        Projectiles.Remove(projectile);
    }

    public static void Register(PlayerProjectile projectile)
    {
        if (projectile != null && !PlayerProjectiles.Contains(projectile))
            PlayerProjectiles.Add(projectile);
    }

    public static void Unregister(PlayerProjectile projectile)
    {
        PlayerProjectiles.Remove(projectile);
    }
}
