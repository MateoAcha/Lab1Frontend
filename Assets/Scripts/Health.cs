using UnityEngine;

public class Health : MonoBehaviour
{
    public int hp = 3;
    public int maxHp;

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
        hp -= damage;
        if (hp < 0)
        {
            hp = 0;
        }

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}
