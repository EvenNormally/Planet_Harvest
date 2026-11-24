Shader "Planet Harvest/Earth Terrain"
{
    Properties
    {
        _Layer ("Layer (soil, stone, iron)", Float) = 0
        _Ground ("Base", Color) = (0.54, 0.30, 0.17, 1)
        _Mark ("Pattern", Color) = (0.72, 0.45, 0.24, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Ground, _Mark;
                float _Layer;
            CBUFFER_END
            struct Attributes { float4 p : POSITION; float3 n : NORMAL; float4 color : COLOR; };
            struct Varyings
            {
                float4 clip : SV_POSITION;
                float3 p : TEXCOORD0;
                half3 tint : TEXCOORD1; // grass, burial, directional light
            };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.clip = TransformObjectToHClip(v.p.xyz);
                o.p = v.p.xyz;
                o.tint.xy = v.color.rg;
                o.tint.z = saturate(dot(TransformObjectToWorldNormal(v.n), GetMainLight().direction));
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float3 p = i.p;
                // Planet-space coordinates continue across every chunk and freshly dug face.
                float wave = sin(dot(p, float3(2.4, 1.2, 1.7))) + 0.45 * sin(dot(p, float3(-3.8, 2.8, 2.1)));
                float grain = sin(5.3 * p.x + 3.4 * p.z) * sin(4.1 * p.y - 2.5 * p.z + wave);
                half3 color;
                if (_Layer < 0.5)
                {
                    color = lerp(_Ground.rgb, _Mark.rgb, step(0.62, grain) * 0.8);
                    half3 grass = lerp(half3(0.095, 0.32, 0.09), half3(0.19, 0.45, 0.105), step(0.20, wave));
                    color = lerp(color, grass, i.tint.x);
                }
                else if (_Layer < 1.5)
                {
                    float stratum = frac(length(p) * 1.65 + 0.22 * wave);
                    color = lerp(_Ground.rgb, _Mark.rgb, step(0.84, stratum) * 0.72);
                }
                else
                {
                    float vein = abs(sin(dot(p, float3(1.1, 3.8, -2.2)) + wave));
                    color = lerp(_Ground.rgb, _Mark.rgb, step(0.39, vein));
                    color += half3(0.22, 0.12, 0.035) * step(0.75, grain);
                }
                half light = lerp(0.70, 1.05, smoothstep(0.14, 0.7, i.tint.z));
                return half4(color * light * lerp(1.0, 0.66, i.tint.y), 1);
            }
            ENDHLSL
        }
    }
}
