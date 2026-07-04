using UnityEngine;

/// <summary>
/// AUDIOVIDO — Hover Vehicle (concept art: flying traffic over the city)
/// Circles the city at a fixed height with a glowing trail look.
/// Parameters set by CitySceneBuilder for variety.
/// </summary>
public class HoverVehicle : MonoBehaviour
{
    [SerializeField] float radius = 30f;
    [SerializeField] float height = 12f;
    [SerializeField] float angularSpeed = 0.25f; // rad/s
    [SerializeField] float phase;
    [SerializeField] float bobAmount = 0.4f;
    [SerializeField] bool clockwise = true;

    void Update()
    {
        float dir = clockwise ? 1f : -1f;
        float a = Time.time * angularSpeed * dir + phase;
        Vector3 pos = new Vector3(
            Mathf.Sin(a) * radius,
            height + Mathf.Sin(Time.time * 0.7f + phase) * bobAmount,
            Mathf.Cos(a) * radius);
        transform.position = pos;
        // Face direction of travel (tangent)
        transform.rotation = Quaternion.LookRotation(
            new Vector3(Mathf.Cos(a) * dir, 0f, -Mathf.Sin(a) * dir));
    }
}
