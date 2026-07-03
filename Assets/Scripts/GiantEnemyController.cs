using System.Collections;
using UnityEngine;

public class GiantEnemyController : MonoBehaviour
{
    public float speed = 1.35f;
    public float attackRange = 14f;
    public float attackCooldown = 4f;
    public float maxHealth = 40f;
    public int touchDamage = 2;
    public int smashDamage = 2;
    public int smashCount = 6;
    public float smashSpacing = 1.05f;
    public float smashDelay = 0.13f;
    public float smashLife = 0.32f;
    public float smashSize = 1.3f;
    public float recoilSpeed = 7f;
    public float recoilTime = 0.08f;

    public bool IsAttacking     => attacking;
    public Vector2 FacingDirection => look;

    private Rigidbody2D body;
    private Transform player;
    private Vector2 look = Vector2.down;
    private float nextAttack;
    private float nextTouchDamageAt;
    private float recoilUntil;
    private bool attacking;

    private void OnEnable()
    {
        OnlineNetworkRegistry.Register(this);
    }

    private void OnDisable()
    {
        OnlineNetworkRegistry.Unregister(this);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        body.gravityScale = 0f;
        body.freezeRotation = true;

        if (GetComponent<BoxCollider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }

        IgnoreRockCollisions();

        Health health = GetComponent<Health>();
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }

        health.hp = Mathf.Max(health.hp, maxHealth);
    }

    private void Start()
    {
        player = MultiplayerState.GetNearestPlayer(transform.position);
        IgnoreRockCollisions();
    }

    private void Update()
    {
        player = MultiplayerState.GetNearestPlayer(transform.position);
        if (player == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time < recoilUntil)
        {
            return;
        }

        Vector2 to = player.position - transform.position;
        float distance = to.magnitude;
        if (to.sqrMagnitude > 0.001f)
        {
            look = to.normalized;
        }

        if (attacking)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        body.linearVelocity = look * speed;

        if (distance <= attackRange && Time.time >= nextAttack)
        {
            StartCoroutine(SmashLine(look));
            nextAttack = Time.time + Mathf.Max(0.1f, attackCooldown);
        }
    }

    public void OnHit(Vector2 hitPoint, float pushMultiplier = 1f)
    {
        Vector2 push = ((Vector2)transform.position - hitPoint).normalized;
        if (push.sqrMagnitude < 0.001f)
        {
            push = Vector2.up;
        }

        body.linearVelocity = push * (recoilSpeed * Mathf.Max(0f, pushMultiplier));
        recoilUntil = Time.time + recoilTime;
        float effectScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        BloodBurst.SpawnNetworked(transform.position, hitPoint, effectScale);
    }

    private IEnumerator SmashLine(Vector2 direction)
    {
        attacking = true;
        body.linearVelocity = Vector2.zero;
        GameAudio.PlayGiantAttackStomp();

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.down;
        }

        direction.Normalize();
        int count = Mathf.Max(1, smashCount);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = direction * (smashSpacing * (i + 1));
            SpawnSmash((Vector2)transform.position + offset);
            yield return new WaitForSeconds(Mathf.Max(0.01f, smashDelay));
        }

        attacking = false;
    }

    private void SpawnSmash(Vector2 position)
    {
        GameObject smash = new GameObject("GiantRockSmash");
        smash.transform.position = new Vector3(position.x, position.y, 0f);
        float size = Mathf.Max(0.2f, smashSize);
        smash.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer renderer = smash.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprite.Square;
        renderer.color = Color.white;
        renderer.sortingOrder = 8;

        BoxCollider2D box = smash.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        Rigidbody2D smashBody = smash.AddComponent<Rigidbody2D>();
        smashBody.bodyType = RigidbodyType2D.Kinematic;
        smashBody.gravityScale = 0f;

        HitBox hit = smash.AddComponent<HitBox>();
        hit.hitsPlayer = true;
        hit.damage = Mathf.Max(1, smashDamage);
        hit.life = Mathf.Max(0.01f, smashLife);

        smash.AddComponent<GiantShockwaveAnimator>();
    }

    private void IgnoreRockCollisions()
    {
        GameObject rocksRoot = GameObject.Find("RuntimeRocks");
        if (rocksRoot == null)
        {
            return;
        }

        Collider2D ownCollider = GetComponent<Collider2D>();
        if (ownCollider == null)
        {
            return;
        }

        Collider2D[] rockColliders = rocksRoot.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < rockColliders.Length; i++)
        {
            if (rockColliders[i] != null)
            {
                Physics2D.IgnoreCollision(ownCollider, rockColliders[i], true);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (Time.time < nextTouchDamageAt)
        {
            return;
        }

        TemporaryWall wall = other.GetComponent<TemporaryWall>();
        if (wall != null)
        {
            wall.Hit(touchDamage);
            nextTouchDamageAt = Time.time + 0.8f;
            return;
        }

        if (TryDamageAllyTarget(other))
        {
            nextTouchDamageAt = Time.time + 0.8f;
            return;
        }

        if (other.GetComponent<PlayerController>() == null)
        {
            return;
        }

        Health health = other.GetComponent<Health>();
        if (health == null)
        {
            return;
        }

        health.Hit(touchDamage);
        nextTouchDamageAt = Time.time + 0.8f;
    }

    private bool TryDamageAllyTarget(Collider2D other)
    {
        if (other.GetComponent<PlayerDecoy>() == null && other.GetComponent<PlayerMinion>() == null)
        {
            return false;
        }

        Health health = other.GetComponent<Health>();
        if (health == null)
        {
            return false;
        }

        health.Hit(touchDamage);
        return true;
    }
}
