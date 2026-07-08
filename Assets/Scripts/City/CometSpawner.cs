using System.Collections;
using UnityEngine;

/// <summary>
/// AUDIOVIDO — Comet Spawner
/// Occasionally streaks a glowing comet across the night sky. Comets are built
/// procedurally at runtime (bright HDR head for bloom + a fading trail) and fly
/// in a straight line before self-destructing. Purely cosmetic; City scene only.
/// </summary>
public class CometSpawner : MonoBehaviour
{
    [SerializeField] float minInterval = 5f;
    [SerializeField] float maxInterval = 12f;
    [SerializeField] float speed       = 70f;
    [SerializeField] float spawnRadius = 190f;
    [SerializeField] Color cometColor  = new Color(2.6f, 2.9f, 3.4f); // HDR → blooms

    Material _headMat, _trailMat;

    void Awake()
    {
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Color");
        _headMat = new Material(unlit);
        _headMat.SetColor("_BaseColor", cometColor);
        _headMat.SetColor("_Color", cometColor);

        Shader sprite = Shader.Find("Sprites/Default"); // uses vertex color → trail fade
        _trailMat = sprite != null ? new Material(sprite) : new Material(unlit);
        _trailMat.color = Color.white;
    }

    void OnEnable() => StartCoroutine(Loop());

    IEnumerator Loop()
    {
        yield return new WaitForSeconds(Random.Range(1.5f, 3.5f));
        while (true)
        {
            SpawnComet();
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }

    void SpawnComet()
    {
        float side = Random.value < 0.5f ? -1f : 1f;
        Vector3 start = new Vector3(side * spawnRadius, Random.Range(70f, 135f), Random.Range(-30f, 170f));
        Vector3 end   = new Vector3(-side * spawnRadius, Random.Range(45f, 95f), Random.Range(-30f, 170f));
        Vector3 vel   = (end - start).normalized * speed;

        var go = new GameObject("Comet");
        go.transform.position = start;

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(go.transform, false);
        head.transform.localScale = Vector3.one * 1.3f;
        Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().sharedMaterial = _headMat;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time            = 1.7f;
        trail.startWidth      = 1.2f;
        trail.endWidth        = 0f;
        trail.material        = _trailMat;
        trail.numCapVertices  = 4;
        trail.alignment       = LineAlignment.View;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.8f, 1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = grad;

        go.AddComponent<CometMover>().Init(vel, spawnRadius * 2.5f);
    }
}

/// <summary>Moves a comet in a straight line and destroys it once it's flown past.</summary>
public class CometMover : MonoBehaviour
{
    Vector3 _vel, _origin;
    float _maxDist;

    public void Init(Vector3 vel, float maxDist)
    {
        _vel = vel; _maxDist = maxDist; _origin = transform.position;
    }

    void Update()
    {
        transform.position += _vel * Time.deltaTime;
        if ((transform.position - _origin).sqrMagnitude > _maxDist * _maxDist)
            Destroy(gameObject);
    }
}
