using UnityEngine;

public class Critter : MonoBehaviour
{
    [SerializeField]
    float minLifeTime;
    [SerializeField]
    float maxLifeTime;
    [SerializeField]
    float minSpeed;
    [SerializeField]
    float maxSpeed;
    MorgInput morgInput;

    private void Start()
    {
        morgInput = GetComponentInChildren<MorgInput>();
        ResetAge();
    }


    private void OnEnable()
    {
        ResetAge();
    }

    float walkSpeed;
    float walkEnd;
    Vector3 walkDirection;

    void SetNewWalkTarget()
    {
        var a = Random.Range(0f, 360f);
        walkDirection = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0);
        walkSpeed = Random.Range(minSpeed, maxSpeed);
        walkEnd = Time.timeSinceLevelLoad + Random.Range(0.4f, 1.5f) / walkSpeed;
    }

    private void Update()
    {
        if (Time.timeSinceLevelLoad > despawnAt)
        {
            gameObject.SetActive(false);
            return;
        }

        if (Time.timeSinceLevelLoad > walkEnd)
        {
            SetNewWalkTarget();
        }

        transform.position += walkSpeed * walkDirection * Time.deltaTime; 
    }

    float spawnTime;

    public float Age => Time.timeSinceLevelLoad - spawnTime;

    void ResetAge()
    {
        spawnTime = Time.timeSinceLevelLoad;
    }

    Vector3 origin;
    Vector3 normal;
    Rect surfaceSize;
    float despawnAt;
    
    public void SpawnToPlane(Rect surfaceSize, Vector3 origin, Vector3 normal)
    {
        despawnAt = Time.timeSinceLevelLoad + Random.Range(minLifeTime, maxLifeTime);

        this.origin = origin;
        this.normal = normal;
        this.surfaceSize = surfaceSize;
        transform.position = origin;
        ResetAge();
        SetNewWalkTarget();
    }
}
