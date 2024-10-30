using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CritterSpawner : MonoBehaviour
{
    [SerializeField]
    Critter Prefab;

    [SerializeField, Range(0, 300)]
    int maxCritters = 100;

    [SerializeField]
    Transform Pool;

    List<Critter> Spawned = new List<Critter>();

    [SerializeField]
    float minSpawnInterval = 0.5f;

    [SerializeField]
    float maxSpawnInterval = 2f;

    [SerializeField]
    Camera cam;

    [SerializeField]
    float maxSpawnFromCamDistance = 10f;

    [SerializeField]
    LayerMask CollisionMask;

    void Start()
    {
        if (Pool == null)
        {
            Pool = transform;
        }        
    }

    Critter GetCritter()
    {
        if (maxCritters == 0) return null;

        var critter = Spawned.FirstOrDefault(c => !c.gameObject.activeSelf);
        if (critter != null) {
            critter.gameObject.SetActive(true);
            return critter;
        }

        if (Spawned.Count < maxCritters)
        {
            critter = Instantiate(Prefab, Pool);
            Spawned.Add(critter);
            return critter;
        }

        critter = Spawned.OrderBy(c => c.Age).Last();
        return critter;
    }

    float nextSpawn;

    Ray GetRandomCamRay()
    {
        var a1 = Random.Range(0f, 20f);
        var a2 = Random.Range(0f, 360f);
        var magnitude = Mathf.Abs(Mathf.Sin(a1 * Mathf.Deg2Rad));
        var dRight = Mathf.Sin(a2 * Mathf.Deg2Rad) * magnitude;
        var dUp = Mathf.Cos(a2 * Mathf.Deg2Rad) * magnitude;
        var direction = cam.transform.forward * (1 - magnitude) + 
            dRight * cam.transform.right + 
            dUp * cam.transform.up;
        
        return new Ray(cam.transform.position, direction);
    }

    Queue<Ray> recentRays = new Queue<Ray>();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        foreach (var ray in recentRays)
        {
            Gizmos.DrawLine(ray.origin, ray.GetPoint(maxSpawnFromCamDistance));
        }
    }

    void Update()
    {
        if (Time.timeSinceLevelLoad > nextSpawn)
        {
            var ray = GetRandomCamRay();
            recentRays.Enqueue(ray);
            if (recentRays.Count > 10) recentRays.Dequeue();

            Debug.Log($"Critter spawn {ray.direction}");
            if (Physics.Raycast(ray, out var hitInfo, maxSpawnFromCamDistance, CollisionMask))
            {
                var critter = GetCritter();
                // Probably better use navmeshes here or such thing
                // not the right normal
                critter.SpawnToPlane(new Rect(-1, -1, 2, 2), hitInfo.point, hitInfo.normal);
                nextSpawn = Time.timeSinceLevelLoad + Random.Range(minSpawnInterval, maxSpawnInterval);
            }
        }
    }
}
