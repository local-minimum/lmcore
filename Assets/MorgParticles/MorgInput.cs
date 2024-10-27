using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class MorgInput : MonoBehaviour
{
    [SerializeField]
    float CurrentVelocity;
    [SerializeField]
    ParticleSystem MorgBody;
    [SerializeField]
    ParticleSystem MorgTail;
    ShapeModule tailShape;
    [SerializeField]
    ParticleSystem MorgHead;
    [SerializeField]
    ParticleSystem MorgTendrilU1;
    [SerializeField]
    ParticleSystem MorgTendrilU2;
    [SerializeField]
    ParticleSystem MorgTendrilU3;
    [SerializeField]
    ParticleSystem MorgTendrilL1;
    [SerializeField]
    ParticleSystem MorgTendrilL2;
    [SerializeField]
    ParticleSystem MorgTendrilL3;
    [SerializeField]
    ParticleSystem MorgTendrilH1;
    [SerializeField]
    ParticleSystem MorgTendrilH2;
    [SerializeField]
    ParticleSystem MorgTendrilH3;

    Vector2 CurrentPos;
    Vector2 LastPos;

    List<ParticleSystem> ParticleSystems = new List<ParticleSystem>();
    List<ShapeModule> TendrilShapeModules = new List<ShapeModule>();

    void Start()
    {
        ParticleSystems = new List<ParticleSystem>()
        {
            MorgBody,
            MorgHead,
            MorgTail,
            MorgTendrilH1,
            MorgTendrilH2,
            MorgTendrilH3,
            MorgTendrilU1,
            MorgTendrilU2,
            MorgTendrilU3,
            MorgTendrilL1,
            MorgTendrilL2,
            MorgTendrilL3,

        };

        TendrilShapeModules = new List<ShapeModule>()
        {
            MorgTendrilH1.shape,
            MorgTendrilH2.shape,
            MorgTendrilH3.shape,
            MorgTendrilL1.shape,
            MorgTendrilL2.shape,
            MorgTendrilL3.shape,
            MorgTendrilU1.shape,
            MorgTendrilU2.shape,
            MorgTendrilU3.shape,
        };

        tailShape = MorgTail.shape;

        CurrentPos = transform.position;

        var seed = (uint) Random.Range(0, uint.MaxValue);
        foreach (ParticleSystem ps in ParticleSystems)
        {
            ps.Stop();
            ps.randomSeed = seed; 
            ps.Play();
        }
    }

    void Update()
    {
        LastPos = CurrentPos;
        CurrentPos = transform.position;
        CurrentVelocity = Vector3.Distance(LastPos, CurrentPos);

        if (CurrentVelocity > 0)
        {
            //tailShape.radius = 0.28f + (CurrentVelocity);
            tailShape.randomPositionAmount = 0.1f + CurrentVelocity;
            tailShape.radiusSpeed = 0.01f + (CurrentVelocity * 10f);

            for (int i = 0, l = TendrilShapeModules.Count; i < l; i++)
            {
                var shape = TendrilShapeModules[i];
                shape.randomPositionAmount = 0.002f + (CurrentVelocity * 0.1f);
                shape.radiusSpeed = 0.2f + CurrentVelocity;
                shape.arc = 20f + (CurrentVelocity * 20f);
            }
        }
    }

    public void ApplyColor(Color color)
    {
        foreach (ParticleSystem ps in ParticleSystems)
        {
            var main = ps.main;
            main.startColor = color;
        }
    }
}
