using UnityEngine;

public class Health : MonoBehaviour
{
    public float hp = 3f;
    public float maxHp;

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
            Destroy(gameObject);
        }
    }
}
