using UnityEngine;

namespace LMCore.Crawler
{
    public delegate void ProjectileCollisionEvent(Projectile projectile, GameObject victim);
    public delegate void ProjectileRecycleEvent(Projectile projectile);

    public class Projectile : MonoBehaviour
    {
        public static event ProjectileCollisionEvent OnCollide;
        public event ProjectileRecycleEvent OnRecycle;

        private Direction _flightDirection = Direction.None;
        public Direction FlightDirection { 
            get => _flightDirection;
            set
            {
                if (_flightDirection == Direction.None && value != Direction.None)
                {
                    phaseState = Time.timeSinceLevelLoad;
                    phase = Phase.Flying;
                }
                _flightDirection = value;
            }
        }

        Transform shooter;
        public void Shoot(Transform shooter, Direction flightDirection)
        {
            this.shooter = shooter;
            transform.position = shooter.position;
            FlightDirection = flightDirection;
            phase = Phase.Flying;
            phaseState = Time.timeSinceLevelLoad;
        }

        [SerializeField, Tooltip("Units/s")]
        private float speed;

        private enum Phase { Inactive, Flying, Colliding };
        Phase phase = Phase.Inactive;

        float phaseState;

        [SerializeField]
        private float gracePeriod = 0.25f;
        [SerializeField]
        private float recycleAfter = 0.4f;
        [SerializeField]
        private int _damage = 3;
        public int Damage => _damage;

        public override string ToString() =>
            $"Projectile '{name}' flying {FlightDirection}";

        private string PrefixLogMessage(string message) =>
            $"{this}: {message}";

        private void OnEnable()
        {
            phaseState = Time.timeSinceLevelLoad;
            if (phase == Phase.Colliding)
            {
                phase = Phase.Inactive;
            }
        }

        void Update()
        {
            if (phase == Phase.Inactive) return;

            if (phase == Phase.Flying)
            {
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (FlightDirection == Direction.None)
                    {
                        rb.isKinematic = true;
                    }
                    else
                    {
                        if (rb.isKinematic)
                        {
                            rb.isKinematic = false;
                        }
                        rb.velocity = (Vector3)FlightDirection.AsLookVector3D() * speed;
                    }
                }
            } else if (phase == Phase.Colliding)
            {
                if (Time.timeSinceLevelLoad - phaseState > recycleAfter)
                {
                    Recycle();
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollission(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            HandleCollission(collision); 
        }

        void HandleCollission(Collision collision)
        {
            if (phase == Phase.Colliding) return; 

            if (Time.timeSinceLevelLoad - phaseState < gracePeriod)
            {
                return;
            }

            phase = Phase.Colliding;
            Debug.Log(PrefixLogMessage($"Hit {collision.gameObject}"));

            OnCollide?.Invoke(this, collision.gameObject);
            FlightDirection = Direction.None;
        }

        void Recycle()
        {
            FlightDirection = Direction.None;
            phase = Phase.Inactive;
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }
            OnRecycle?.Invoke(this);
            transform.position = shooter.position;
        }
    }
}
