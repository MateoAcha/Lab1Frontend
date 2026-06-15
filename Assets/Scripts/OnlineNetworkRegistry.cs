using System.Collections.Generic;

public static class OnlineNetworkRegistry
{
    public static readonly List<EnemyController> MeleeEnemies = new List<EnemyController>();
    public static readonly List<RangedEnemyController> RangedEnemies = new List<RangedEnemyController>();
    public static readonly List<GiantEnemyController> GiantEnemies = new List<GiantEnemyController>();
    public static readonly List<EnemyProjectile> Projectiles = new List<EnemyProjectile>();
    public static readonly List<PlayerProjectile> PlayerProjectiles = new List<PlayerProjectile>();
    public static readonly List<HitBox> PlayerHitBoxes = new List<HitBox>();
    public static readonly List<PlayerMinion> PlayerMinions = new List<PlayerMinion>();
    public static readonly List<RangedAbilityProjectile> RangedAbilityProjectiles = new List<RangedAbilityProjectile>();
    public static readonly List<DroppedMaterialPickup> ItemDrops = new List<DroppedMaterialPickup>();
    public static readonly List<ExpansionBurst> Bursts = new List<ExpansionBurst>();
    public static readonly List<GravityBombProjectile> GravityBombs = new List<GravityBombProjectile>();
    public static readonly List<GravityWell> GravityWells = new List<GravityWell>();
    public static readonly List<PlayerThrownWeapon> ThrownWeapons = new List<PlayerThrownWeapon>();

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

    public static void Register(PlayerMinion minion)
    {
        if (minion != null && !PlayerMinions.Contains(minion))
            PlayerMinions.Add(minion);
    }

    public static void Unregister(PlayerMinion minion)
    {
        PlayerMinions.Remove(minion);
    }

    public static void Register(RangedAbilityProjectile proj)
    {
        if (proj != null && !RangedAbilityProjectiles.Contains(proj))
            RangedAbilityProjectiles.Add(proj);
    }

    public static void Unregister(RangedAbilityProjectile proj)
    {
        RangedAbilityProjectiles.Remove(proj);
    }

    public static void Register(DroppedMaterialPickup drop)
    {
        if (drop != null && !ItemDrops.Contains(drop))
            ItemDrops.Add(drop);
    }

    public static void Unregister(DroppedMaterialPickup drop)
    {
        ItemDrops.Remove(drop);
    }

    public static void Register(ExpansionBurst b)   { if (b != null && !Bursts.Contains(b)) Bursts.Add(b); }
    public static void Unregister(ExpansionBurst b) => Bursts.Remove(b);

    public static void Register(GravityBombProjectile b)   { if (b != null && !GravityBombs.Contains(b)) GravityBombs.Add(b); }
    public static void Unregister(GravityBombProjectile b) => GravityBombs.Remove(b);

    public static void Register(GravityWell w)   { if (w != null && !GravityWells.Contains(w)) GravityWells.Add(w); }
    public static void Unregister(GravityWell w) => GravityWells.Remove(w);

    public static void Register(PlayerThrownWeapon t)   { if (t != null && !ThrownWeapons.Contains(t)) ThrownWeapons.Add(t); }
    public static void Unregister(PlayerThrownWeapon t) => ThrownWeapons.Remove(t);
}
