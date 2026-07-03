using UnityEngine;

public class Health : MonoBehaviour
{
    public float hp = 3f;
    public float maxHp;
    public float playerHitImmunitySeconds = 0.55f;
    private bool deathHandled;
    private float playerDamageImmuneUntil;

    private void Start()
    {
        if (maxHp <= 0)
        {
            maxHp = hp;
        }

        if (hp > maxHp)
        {
            hp = maxHp;
        }

        if (GetComponent<HealthBar>() == null)
        {
            gameObject.AddComponent<HealthBar>();
        }
    }

    public void Hit(int damage)
    {
        Hit((float)damage);
    }

    public void Hit(float damage)
    {
        bool isDamage = damage > 0f;
        PlayerController pc = GetComponent<PlayerController>();
        PlayerReviveState reviveState = GetComponent<PlayerReviveState>();
        if (isDamage && reviveState != null && reviveState.IsDowned)
        {
            return;
        }

        if (isDamage && pc != null && Time.time < playerDamageImmuneUntil)
        {
            return;
        }

        if (deathHandled)
        {
            return;
        }

        float previousHp = hp;
        hp -= damage;
        if (hp < 0f)
        {
            hp = 0f;
        }

        if (hp > maxHp)
        {
            hp = maxHp;
        }

        if (isDamage && pc != null && hp < previousHp - 0.001f)
        {
            playerDamageImmuneUntil = Time.time + Mathf.Max(0f, playerHitImmunitySeconds);
            PlayPlayerHitFeedback();
        }

        if (hp <= 0f)
        {
            deathHandled = true;

            if (pc != null)
            {
                if (MultiplayerState.IsMultiplayer || MultiplayerState.IsOnline)
                {
                    if (reviveState == null)
                        reviveState = gameObject.AddComponent<PlayerReviveState>();
                    reviveState.Down();
                    MultiplayerState.RegisterPlayerDeath(pc);
                    return;
                }

                MultiplayerState.RegisterPlayerDeath(pc);
            }
            else if (GetComponent<RangedEnemyController>() != null)
            {
                GameStatsTracker.RegisterRangedEnemyKilled();
            }
            else if (GetComponent<GiantEnemyController>() != null)
            {
                GameStatsTracker.RegisterGiantEnemyKilled();
                DroppedMaterialPickup.SpawnForCurrentMap(transform.position);
            }
            else if (GetComponent<EnemyController>() != null)
            {
                GameStatsTracker.RegisterMeleeEnemyKilled();
            }

            Destroy(gameObject);
        }
    }

    public void ReviveWithHealth(float revivedHp)
    {
        maxHp = Mathf.Max(maxHp, 1f);
        hp = Mathf.Clamp(revivedHp, 1f, maxHp);
        deathHandled = false;
    }

    public void SetHealthSilently(float value, float maxValue)
    {
        maxHp = Mathf.Max(maxValue, 0.01f);
        hp = Mathf.Clamp(value, 0f, maxHp);
        if (hp > 0f)
            deathHandled = false;
    }

    public void PlayPlayerHitFeedback()
    {
        PlayerHitFeedback feedback = GetComponent<PlayerHitFeedback>();
        if (feedback == null)
            feedback = gameObject.AddComponent<PlayerHitFeedback>();
        feedback.Play();
    }
}
