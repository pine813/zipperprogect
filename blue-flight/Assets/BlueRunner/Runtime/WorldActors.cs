using UnityEngine;

namespace BlueRunner
{
    public sealed class CoinActor : MonoBehaviour
    {
        bool collected;
        void Update()
        {
            Transform coin = transform.GetChild(1);
            coin.localPosition = new Vector3(0, Mathf.Sin(Time.time * 3 + transform.position.x) * .07f, 0);
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || GetComponentInParent<GameRoot>().State != RunState.Playing || !other.TryGetComponent<PlayerController>(out _)) return;
            collected = true;
            GetComponentInParent<GameRoot>().Collect(transform.position);
            gameObject.SetActive(false);
        }
    }

    public sealed class EnemyActor : MonoBehaviour
    {
        public float left, right, speed = 1.35f;
        Rigidbody2D body;
        int direction = 1;
        bool defeated;
        public void Defeat() { if (defeated) return; defeated = true; GetComponentInParent<GameRoot>().EnemyDefeated(transform.position); gameObject.SetActive(false); }
        void Awake() { body = GetComponent<Rigidbody2D>(); }
        void FixedUpdate()
        {
            if (GetComponentInParent<GameRoot>().State != RunState.Playing) return;
            if (body.position.x >= right) direction = -1;
            if (body.position.x <= left) direction = 1;
            body.MovePosition(body.position + Vector2.right * (direction * speed * Time.fixedDeltaTime));
            Transform visual = transform.GetChild(0);
            visual.localScale = new Vector3(.95f * (1 + Mathf.Sin(Time.time * 7) * .045f), .76f * (1 - Mathf.Sin(Time.time * 7) * .06f), 1);
        }
        void OnTriggerEnter2D(Collider2D other) { Touch(other); }
        void OnTriggerStay2D(Collider2D other) { Touch(other); }
        void Touch(Collider2D other)
        {
            if (defeated || GetComponentInParent<GameRoot>().State != RunState.Playing || !other.TryGetComponent<PlayerController>(out var player)) return;
            if (player.Dashing)
            {
                defeated = true;

                GetComponentInParent<GameRoot>().EnemyDefeated(transform.position);
                gameObject.SetActive(false);
            }
            else GetComponentInParent<GameRoot>().Hurt(false, transform.position);
        }
    }

    public sealed class SpikeActor : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other) { Touch(other); }
        void OnTriggerStay2D(Collider2D other) { Touch(other); }
        void Touch(Collider2D other)
        {
            if (other.TryGetComponent<PlayerController>(out var player) && !player.Invulnerable)
                GetComponentInParent<GameRoot>().Hurt(false, transform.position);
        }
    }

    public sealed class CheckpointActor : MonoBehaviour
    {
        public Vector2 spawnPoint;
        bool activated;
        void OnTriggerEnter2D(Collider2D other)
        {
            if (activated || !other.TryGetComponent<PlayerController>(out _) || GetComponentInParent<GameRoot>().State != RunState.Playing) return;
            activated = true;
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>()) sprite.color = Art.Mint;
            GetComponentInParent<GameRoot>().SetCheckpoint(spawnPoint);
        }
    }

    public sealed class ExitActor : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<PlayerController>(out _)) GetComponentInParent<GameRoot>().Win();
        }
    }

    [DefaultExecutionOrder(100)]
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform target;
        Vector3 velocity;
        float shake;
        Vector3 Desired()
        {
            float half = GetComponent<Camera>().orthographicSize * GetComponent<Camera>().aspect;
            return new Vector3(Mathf.Clamp(target.position.x + 4f, half - 1.5f, 126 - half), 0f, -10);
        }
        public void Snap() { if (target != null) transform.position = Desired(); velocity = Vector3.zero; }
        public void Shake(float duration) { shake = duration; }
        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, .10f, 100, Time.deltaTime);
            if (shake > 0) {
                shake -= Time.deltaTime;
                transform.position += new Vector3(Mathf.Sin(Time.time * 83), Mathf.Cos(Time.time * 67), 0) * shake * .3f;
            }
        }
    }

    [DefaultExecutionOrder(200)]
    public sealed class Parallax : MonoBehaviour
    {
        public float factor;
        Vector3 origin;
        void Start() { origin = transform.position; }
        void LateUpdate()
        {
            Vector3 cameraPosition = GetComponentInParent<GameRoot>().CameraRig.transform.position;
            transform.position = origin + new Vector3((cameraPosition.x - 7) * factor, cameraPosition.y * factor * .3f, 0);
        }
    }

    public sealed class Drift : MonoBehaviour
    {
        public float phase;
        Vector3 origin;
        SpriteRenderer sprite;
        void Start() { origin = transform.position; sprite = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            float t = Time.time * .6f + phase;
            transform.position = origin + new Vector3(Mathf.Sin(t) * .2f, Mathf.Cos(t * 1.3f) * .15f, 0);
            Color c = sprite.color; c.a = .45f + Mathf.Sin(t * 3) * .3f; sprite.color = c;
        }
    }

    public sealed class Effects : MonoBehaviour
    {
        Vector2 velocity;
        float remaining = .5f;
        SpriteRenderer sprite;
        Color color;
        public static void Burst(Vector2 at, Color color, int count)
        {
            for (int i = 0; i < count; i++) {
                var sr = Art.Sprite("Spark", Art.Disc, null, at, Vector2.one * Random.Range(.035f, .10f), color, 30);
                var effect = sr.gameObject.AddComponent<Effects>();
                effect.sprite = sr; effect.color = color;
                effect.velocity = Random.insideUnitCircle * 2.8f + Vector2.up * 1.1f;
            }
        }
        void Update()
        {
            remaining -= Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= Mathf.Exp(-Time.deltaTime * 2);
            color.a = Mathf.Clamp01(remaining * 2);
            sprite.color = color;
            if (remaining <= 0) Destroy(gameObject);
        }
    }
}
