Shader "Planet Harvest/Earth Sky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float3 _LocalUp, _SunDirection;
            struct Output { float4 pos : SV_POSITION; float3 ray : TEXCOORD0; };
            Output Vert(float4 p : POSITION)
            {
                Output o;
                o.pos = TransformObjectToHClip(p.xyz);
                o.ray = p.xyz;
                return o;
            }
            float hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            float noise(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                f = f*f*(3-2*f);
                float4 a = float4(hash(i), hash(i+float3(0,1,0)), hash(i+float3(0,0,1)), hash(i+float3(0,1,1)));
                float4 b = float4(hash(i+float3(1,0,0)),hash(i+float3(1,1,0)),hash(i+float3(1,0,1)),hash(i+1));
                float4 x = lerp(a,b,f.x);
                return lerp(lerp(x.x,x.y,f.y),lerp(x.z,x.w,f.y),f.z);
            }
            half4 Frag(Output i) : SV_Target
            {
                float3 d = normalize(i.ray);
                float h = saturate(dot(d, _LocalUp));
                float3 sky = lerp(float3(.82,.70,.93),float3(.20,.16,.66),sqrt(h));
                float sun = saturate(dot(d, _SunDirection));
                sky += float3(.55,.38,.15) * pow(sun,80);
                sky = lerp(sky,float3(1.8,1.45,.88),smoothstep(.9984,.9989,sun));
                float3 p = d * 5.4 / (.58 + .55*h) + _LocalUp*h*2.8 + float3(_Time.y*.017,0,0);
                float cloud = noise(p)*.52 + noise(p*2.1)*.27 + noise(p*4.4)*.14 + noise(p*9)*.07;
                float cover = smoothstep(.51,.565,cloud)*smoothstep(.01,.16,h);
                return half4(lerp(sky,lerp(float3(.68,.60,.83),float3(1,.94,.91),smoothstep(.52,.66,cloud)),cover),1);
            }
            ENDHLSL
        }
    }
}
