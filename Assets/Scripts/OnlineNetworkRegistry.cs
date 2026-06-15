using System.Collections.Generic;

public static class OnlineNetworkRegistry
{
    public static readonly List<EnemyController> MeleeEnemies = new List<EnemyController>();
    public static readonly List<RangedEnemyController> RangedEnemies = new List<RangedEnemyController>();
    public static readonly List<GiantEnemyController> GiantEnemies = new List<GiantEnemyController>();
    public static readonly List<EnemyProjectile> Projectiles = new List<EnemyProjectile>();
    public static readonly List<PlayerProjectile> PlayerProjectiles = new List<PlayerProjectile>();
    public static readonly List<HitBox> PlayerHitBoxes = new List<HitBox>();
    public static readonly List<PlayerThrownWeapon> ThrownWeapons = new List<PlayerThrownWeapon>();
    public static readonly List<RangedAbilityProjectile> RangedAbilityProjectiles = new List<RangedAbilityProjectile>();
    public static readonly List<ExpansionBurst> ExpansionBursts = new List<ExpansionBurst>();
    public static readonly List<GravityBombProjectile> GravityBombs = new List<GravityBombProjectile>();
    public static readonly List<GravityWell> GravityWells = new List<GravityWell>();
    public static readonly List<PlayerMinion> PlayerMinions = new List<PlayerMinion>();
    public static readonly List<FireTrailSegment> FireTrails = new List<FireTrailSegment>();

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

    public static void Register(GiantEnemyController enemy)
    {
        if (enemy != null && !GiantEnemies.Contains(enemy))
            GiantEnemies.Add(enemy);
    }

    public static void Unregister(GiantEnemyController enemy)
    {
        GiantEnemies.Remove(enemy);
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

    public static void Register(HitBox hitBox)
    {
        if (hitBox != null && !PlayerHitBoxes.Contains(hitBox))
            PlayerHitBoxes.Add(hitBox);
    }

    public static void Unregister(HitBox hitBox)
    {
        PlayerHitBoxes.Remove(hitBox);
    }

    public static void Register(PlayerThrownWeapon weapon)
    {
        if (weapon != null && !ThrownWeapons.Contains(weapon))
            ThrownWeapons.Add(weapon);
    }

    public static void Unregister(PlayerThrownWeapon weapon)
    {
        ThrownWeapons.Remove(weapon);
    }

    public static void Register(RangedAbilityProjectile projectile)
    {
        if (projectile != null && !RangedAbilityProjectiles.Contains(projectile))
            RangedAbilityProjectiles.Add(projectile);
    }

    public static void Unregister(RangedAbilityProjectile projectile)
    {
        RangedAbilityProjectiles.Remove(projectile);
    }

    public static void Register(ExpansionBurst burst)
    {
        if (burst != null && !ExpansionBursts.Contains(burst))
            ExpansionBursts.Add(burst);
    }

    public static void Unregister(ExpansionBurst burst)
    {
        ExpansionBursts.Remove(burst);
    }

    public static void Register(GravityBombProjectile bomb)
    {
        if (bomb != null && !GravityBombs.Contains(bomb))
            GravityBombs.Add(bomb);
    }

    public static void Unregister(GravityBombProjectile bomb)
    {
        GravityBombs.Remove(bomb);
    }

    public static void Register(GravityWell well)
    {
        if (well != null && !GravityWells.Contains(well))
            GravityWells.Add(well);
    }

    public static void Unregister(GravityWell well)
    {
        GravityWells.Remove(well);
    }

    public static void Register(PlayerMinion minion)
    {
        if (minion != null && !PlayerMinions.Contains(minion))
            PlayerMinions.Add(minion);
    }

    public static void Unregister(PlayerMinion minion)
    {
        PlayerMinions.Remove(minion);
    }

    public static void Register(FireTrailSegment fireTrail)
    {
        if (fireTrail != null && !FireTrails.Contains(fireTrail))
            FireTrails.Add(fireTrail);
    }

    public static void Unregister(FireTrailSegment fireTrail)
    {
        FireTrails.Remove(fireTrail);
    }
}
