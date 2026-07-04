using UnityEngine;
using UnityEditor;

/// <summary>
/// AUDIOVIDO — Character Proxy Builder (art pass v1)
/// Builds a readable humanoid silhouette from primitives — torso, head,
/// arms, legs — plus glow accents (sash, visor, hair) that presence scripts
/// pulse at runtime. A big step up from lone capsules; replaced by rigged
/// Mixamo models in art pass v2 (spec §12.4).
/// </summary>
public static class CharacterProxyBuilder
{
    public struct Proxy
    {
        public GameObject root;
        public Renderer[] glowRenderers;
    }

    /// <summary>
    /// Build a humanoid proxy. <paramref name="scale"/> 1 = ~1.8m tall.
    /// <paramref name="armsRaised"/> for hype poses (PULSE/hologram).
    /// </summary>
    public static Proxy Build(string name, Vector3 position, float scale,
        Material bodyMat, Material glowMat, bool armsRaised = false)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;

        float s = scale;

        // Torso
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(root.transform);
        torso.transform.localPosition = new Vector3(0f, 1.15f * s, 0f);
        torso.transform.localScale = new Vector3(0.52f * s, 0.42f * s, 0.32f * s);
        torso.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0f, 1.72f * s, 0f);
        head.transform.localScale = Vector3.one * 0.28f * s;
        head.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // Visor (glow accent across the face)
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "GlowVisor";
        visor.transform.SetParent(root.transform);
        visor.transform.localPosition = new Vector3(0f, 1.74f * s, 0.11f * s);
        visor.transform.localScale = new Vector3(0.22f * s, 0.045f * s, 0.08f * s);
        visor.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

        // Sash (glow band across torso)
        GameObject sash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sash.name = "GlowSash";
        sash.transform.SetParent(root.transform);
        sash.transform.localPosition = new Vector3(0f, 1.22f * s, 0f);
        sash.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        sash.transform.localScale = new Vector3(0.56f * s, 0.09f * s, 0.34f * s);
        sash.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

        // Arms
        float armAngle = armsRaised ? -155f : -12f;
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.name = i == 0 ? "Arm_L" : "Arm_R";
            arm.transform.SetParent(root.transform);
            arm.transform.localPosition = new Vector3(
                side * 0.36f * s, (armsRaised ? 1.55f : 1.15f) * s, 0f);
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, side * armAngle);
            arm.transform.localScale = new Vector3(0.13f * s, 0.34f * s, 0.13f * s);
            arm.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
        }

        // Legs
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leg.name = i == 0 ? "Leg_L" : "Leg_R";
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(side * 0.14f * s, 0.42f * s, 0f);
            leg.transform.localScale = new Vector3(0.15f * s, 0.44f * s, 0.15f * s);
            leg.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
        }

        // Base glow ring
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "GlowRing";
        ring.transform.SetParent(root.transform);
        ring.transform.localPosition = new Vector3(0f, 0.02f * s, 0f);
        ring.transform.localScale = new Vector3(0.9f * s, 0.02f * s, 0.9f * s);
        ring.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

        return new Proxy
        {
            root = root,
            glowRenderers = new[]
            {
                visor.GetComponent<MeshRenderer>(),
                sash.GetComponent<MeshRenderer>(),
                ring.GetComponent<MeshRenderer>()
            }
        };
    }
}
