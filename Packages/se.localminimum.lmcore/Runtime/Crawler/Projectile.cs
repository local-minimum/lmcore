using UnityEngine;

namespace LMCore.Crawler
{
    public delegate void ProjectileCollisionEvent(Projectile projectile, GameObject victim);
    public delegate void ProjectileRecycleEvent(Projectile projectile);

    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        public static event ProjectileCollisionEvent OnCollide;
        public event ProjectileRecycleEvent OnRecycle;

        private Vector3 _flightDirection;
        public Vector3 FlightDirection
        {
            get => _flightDirection;
            set
            {
                if (_flightDirection == Vector3.zero && value != Vector3.zero)
                {
                    PhaseStart = Time.timeSinceLevelLoad;
                    phase = Phase.Flying;
                }
                _flightDirection = value;
            }
        }

        Transform shooter;
        public void Shoot(Transform shooter, Direction flightDirection)
        {
            this.shooter = shooter;
            Position = shooter.position;
            FlightDirection = flightDirection.AsLookVector3D();
            phase = Phase.Flying;
            PhaseStart = Time.timeSinceLevelLoad;
        }

        public void Shoot(Transform shooter, Vector3 flightDirection)
        {
            this.shooter = shooter;
            Position = shooter.position;
            FlightDirection = flightDirection;
            phase = Phase.Flying;
            PhaseStart = Time.timeSinceLevelLoad;
        }

        [SerializeField, Tooltip("Units/s")]
        private float speed;

        private enum Phase { Inactive, Flying, Colliding };
        Phase phase = Phase.Inactive;

        float _PhaseStart;
        float PhaseStart
        {
            set { _PhaseStart = value; }
        }
        float PhaseDuration => Paused ? _PauseStart - _PhaseStart : Time.timeSinceLevelLoad - _PhaseStart;

        [SerializeField]
        private float gracePeriod = 0.25f;
        [SerializeField]
        private float recycleAfter = 0.4f;
        [SerializeField]
        private int _damage = 3;
        public int Damage => _damage;

        public override string ToString() =>
            $"<Projectile '{name}' flying {FlightDirection}>";

        private string PrefixLogMessage(string message) =>
            $"{this}: {message}";

        bool _Paused;
        float _PauseStart;
        public bool Paused
        {
            get => _Paused;
            set
            {
                if (!Paused && value)
                {
                    _PauseStart = Time.timeSinceLevelLoad;
                    StopRigidbody();
                    var ps = GetComponentInChildren<ParticleSystem>();
                    ps?.Pause();
                }
                else if (Paused && !value)
                {
                    _PhaseStart += Time.timeSinceLevelLoad - _PauseStart;
                    var ps = GetComponentInChildren<ParticleSystem>();
                    ps?.Play();
                }

                _Paused = value;
            }
        }

        private void OnEnable()
        {
            PhaseStart = Time.timeSinceLevelLoad;
            if (phase == Phase.Colliding)
            {
                phase = Phase.Inactive;
            }
        }

        Rigidbody _rb;
        Rigidbody rb
        {
            get
            {
                if (_rb == null)
                {
                    _rb = GetComponent<Rigidbody>();
                }
                return _rb;
            }
        }

        public Vector3 Position
        {
            get => rb.position;
            set
            {
                rb.position = value;
            }
        }

        void Update()
        {
            if (Paused) return;

            if (phase == Phase.Inactive) return;

            if (phase == Phase.Flying)
            {
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (FlightDirection == Vector3.zero)
                    {
                        rb.isKinematic = true;
                    }
                    else
                    {
                        if (rb.isKinematic)
                        {
                            rb.isKinematic = false;
                        }
                        rb.linearVelocity = FlightDirection * speed;
                    }
                }
            }
            else if (phase == Phase.Colliding)
            {
                if (PhaseDuration > recycleAfter)
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

            if (PhaseDuration < gracePeriod)
            {
                return;
            }

            phase = Phase.Colliding;
            Debug.Log(PrefixLogMessage($"Hit {collision.gameObject}"));

            OnCollide?.Invoke(this, collision.gameObject);
            FlightDirection = Vector3.zero;
        }

        void StopRigidbody()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
        }

        void Recycle()
        {
            FlightDirection = Vector3.zero;
            phase = Phase.Inactive;
            StopRigidbody();
            OnRecycle?.Invoke(this);
            Position = shooter.position;
        }
    }
}
