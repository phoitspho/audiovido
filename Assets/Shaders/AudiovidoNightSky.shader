Shader "AUDIOVIDO/NightSky"
{
    // Deep-space night sky: subtle gradient, twinkling star field, a soft galaxy
    // band, and a low silver moon with halo + horizon glow. No sun (night vibes).
    Properties
    {
        _ZenithColor  ("Zenith",       Color) = (0.02,0.02,0.06,1)
        _HorizonColor ("Horizon",      Color) = (0.02,0.03,0.09,1)
        _GroundColor  ("Below Horizon",Color) = (0.004,0.004,0.010,1)
        _GalaxyColor  ("Galaxy Band",  Color) = (0.26,0.20,0.46,1)

        _MoonDir   ("Moon Direction", Vector) = (0.15,0.14,1,0)
        _MoonColor ("Moon Color",     Color)  = (0.86,0.90,1.0,1)
        _MoonSize  ("Moon Size",      Range(0.001,0.2)) = 0.032
        _MoonGlow  ("Moon Glow",      Range(1,300)) = 60

        _StarDensity  ("Star Density",   Float) = 120
        _StarSparsity ("Star Sparsity",  Range(0.5,0.999)) = 0.90
        _StarRadius   ("Star Radius",    Float) = 0.00006
        _StarBright   ("Star Brightness",Float) = 1.6
        _Twinkle      ("Twinkle Speed",  Float) = 3.0
        _Exposure     ("Exposure",       Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            float4 _ZenithColor, _HorizonColor, _GroundColor, _GalaxyColor, _MoonColor, _MoonDir;
            float _MoonSize, _MoonGlow, _StarDensity, _StarSparsity, _StarRadius, _StarBright, _Twinkle, _Exposure;

            float hash13(float3 p3)
            {
                p3 = frac(p3 * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float starField(float3 dir)
            {
                float3 p = dir * _StarDensity;
                float3 bId = floor(p);
                float acc = 0.0;
                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    float3 id = bId + float3(x, y, z);
                    float h = hash13(id);
                    if (h > _StarSparsity)
                    {
                        float3 off = float3(hash13(id + 1.3), hash13(id + 2.7), hash13(id + 4.1));
                        float3 sc = normalize(id + off);
                        float ang = 1.0 - dot(dir, sc);
                        float core = smoothstep(_StarRadius, 0.0, ang);
                        float glow = 0.05 * smoothstep(_StarRadius * 3.5, 0.0, ang);
                        float tw = 0.55 + 0.45 * sin(_Time.y * _Twinkle + h * 40.0);
                        acc += (core + glow) * (0.5 + 0.7 * h) * tw;
                    }
                }
                return acc;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float h = dir.y;

                // Base vertical gradient
                float3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(saturate(h), 0.45));
                sky = lerp(sky, _GroundColor.rgb, saturate(-h * 3.0));

                // Soft galaxy band along a tilted great circle
                float3 bandN = normalize(float3(0.6, 0.5, -0.2));
                float band = exp(-pow(dot(dir, bandN) * 3.5, 2.0));
                float bandNoise = 0.5 + 0.5 * hash13(floor(dir * 40.0));
                sky += _GalaxyColor.rgb * band * (0.06 + 0.12 * bandNoise) * saturate(h + 0.2);

                // Stars (fade out below the horizon)
                float starMask = saturate(h * 4.0);
                sky += starField(dir) * _StarBright * starMask;

                // Silver moon (disk + halo + faint crater mottling)
                float3 md = normalize(_MoonDir.xyz);
                float mdot = saturate(dot(dir, md));
                float disk = smoothstep(1.0 - _MoonSize, 1.0 - _MoonSize * 0.5, mdot);
                float crater = 0.85 + 0.15 * hash13(dir * 90.0);
                float halo = pow(mdot, _MoonGlow);
                sky = lerp(sky, _MoonColor.rgb * crater, disk);
                sky += _MoonColor.rgb * halo * 0.14;

                // Subtle moon glow hugging the horizon
                float2 dh = normalize(float2(dir.x, dir.z) + 1e-5);
                float2 mh = normalize(float2(md.x, md.z) + 1e-5);
                float hg = exp(-abs(h) * 7.0) * pow(saturate(dot(dh, mh)), 4.0);
                sky += _MoonColor.rgb * hg * 0.12;

                sky *= _Exposure;
                return fixed4(sky, 1.0);
            }
            ENDCG
        }
    }
}
