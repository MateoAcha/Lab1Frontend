using UnityEngine;

public class Health : MonoBehaviour
{
    public float hp = 3f;
    public float maxHp;
    private bool deathHandled;

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
        if (deathHandled)
        {
            return;
        }

        hp -= damage;
        if (hp < 0f)
        {
            hp = 0f;
        }

        if (hp > maxHp)
        {
            hp = maxHp;
        }

        if (hp <= 0f)
        {
            deathHandled = true;

            if (GetComponent<PlayerController>() != null)
            {
                GameStatsTracker.RegisterPlayerDied();
            }
            else if (GetComponent<RangedEnemyController>() != null)
            {
                GameStatsTracker.RegisterRangedEnemyKilled();
            }
            else if (GetComponent<EnemyController>() != null)
            {
                GameStatsTracker.RegisterMeleeEnemyKilled();
            }

            Destroy(gameObject);
        }
    }
}
