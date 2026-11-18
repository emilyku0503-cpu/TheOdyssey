// 로우폴리 바다: 버텍스 파도(각진 면) + 섬/바위 주변에 밀려왔다 빠지는 거품.
// 메시는 잘게 쪼갠 평면이어야 출렁입니다 (Assets/Shader/OceanWaveGrid.asset).
// 해안 거품은 URP 에셋의 Depth Texture 가 켜져 있어야 보입니다.
Shader "TheOdyssey/LowPolyOcean"
{
    Properties
    {
        [Header(Color)]
        _ShallowColor ("Shallow Color", Color) = (0.20, 0.55, 0.65, 0.75)
        _DeepColor ("Deep Color", Color) = (0.04, 0.22, 0.38, 1)
        _DepthMaxDistance ("Depth Fade Distance", Float) = 2.5

        [Header(Waves)]
        _WaveHeight ("Wave Height", Float) = 0.22
        _WaveLength ("Wave Length", Float) = 9
        _WaveSpeed ("Wave Speed", Float) = 1.2
        _WaveDirection ("Main Direction (XZ)", Vector) = (1, 0, 0.35, 0)
        _Choppiness ("Choppiness", Range(0, 1)) = 0.5

        [Header(Shore Foam)]
        _FoamColor ("Foam Color", Color) = (0.95, 0.97, 1, 1)
        _FoamDistance ("Foam Width", Float) = 0.9
        _FoamWashSpeed ("Wash In-Out Speed", Float) = 0.9
        _FoamWashAmount ("Wash In-Out Amount", Range(0, 1)) = 0.6
        _CrestFoam ("Wave Crest Foam", Range(0, 1)) = 0.35

        [Header(Lighting)]
        _Specular ("Sun Glint", Range(0, 2)) = 0.6
        _Gloss ("Glint Sharpness", Float) = 48
        _FacetContrast ("Facet Contrast (low-poly look)", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "Ocean"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float _DepthMaxDistance;
                float _WaveHeight;
                float _WaveLength;
                float _WaveSpeed;
                float4 _WaveDirection;
                float _Choppiness;
                half4 _FoamColor;
                float _FoamDistance;
                float _FoamWashSpeed;
                float _FoamWashAmount;
                float _CrestFoam;
                float _Specular;
                float _Gloss;
                float _FacetContrast;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
                float  waveHeight : TEXCOORD2; // -1..1, for crest foam
                float  fogFactor  : TEXCOORD3;
            };

            // one directional sine wave; returns height in -1..1 (sharpened by choppiness)
            float Wave(float2 p, float2 dir, float lengthMul, float speedMul, float t)
            {
                float k = 6.28318 / max(0.01, _WaveLength * lengthMul);
                float s = sin(dot(normalize(dir), p) * k + t * _WaveSpeed * speedMul);
                // choppiness: pointier crests, flatter troughs
                float c = 1.0 - abs(cos(dot(normalize(dir), p) * k + t * _WaveSpeed * speedMul));
                return lerp(s, s * 0.6 + c * 0.8 - 0.4, _Choppiness);
            }

            float WaveSum(float2 p, float t)
            {
                float2 d = _WaveDirection.xz;
                if (dot(d, d) < 0.0001) d = float2(1, 0);
                float2 d2 = float2(d.y, -d.x) * 0.6 + d * 0.8;   // a second, crossing direction
                float2 d3 = float2(-d.y, d.x) * 0.5 + d;
                float h = Wave(p, d, 1.0, 1.0, t) * 0.55
                        + Wave(p, d2, 0.63, 1.35, t) * 0.3
                        + Wave(p, d3, 0.37, 1.8, t) * 0.15;
                return h; // roughly -1..1
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float h = WaveSum(ws.xz, _Time.y);
                ws.y += h * _WaveHeight;
                o.positionWS = ws;
                o.waveHeight = h;
                o.positionCS = TransformWorldToHClip(ws);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // cheap value noise for an irregular foam edge
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // flat (faceted) normal from screen-space derivatives -> low-poly look
                float3 n = normalize(cross(ddy(i.positionWS), ddx(i.positionWS)));
                if (n.y < 0) n = -n;

                // water depth below the surface (needs the camera depth texture)
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float surfEye = i.screenPos.w;
                float depth = max(0, sceneEye - surfEye);

                float deep = saturate(depth / max(0.01, _DepthMaxDistance));
                half4 col = lerp(_ShallowColor, _DeepColor, deep);

                // shore foam that washes in and out like waves hitting the beach
                float t = _Time.y;
                float wash = 1.0 - _FoamWashAmount * (0.5 + 0.5 * sin(t * _FoamWashSpeed + Noise(i.positionWS.xz * 0.15) * 6.28));
                float edgeNoise = Noise(i.positionWS.xz * 1.3 + t * 0.3) * 0.5 + 0.5;
                float foamLine = _FoamDistance * wash * edgeNoise;
                float shoreFoam = 1.0 - smoothstep(foamLine * 0.6, foamLine, depth);

                // a little white on wave crests
                float crest = smoothstep(0.35, 0.8, i.waveHeight) * _CrestFoam;

                float foam = saturate(shoreFoam + crest);

                // lighting: main light lambert + ambient + sun glint, with shadows
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float ndl = saturate(dot(n, mainLight.direction));
                float atten = lerp(0.55, 1.0, mainLight.shadowAttenuation);
                half3 ambient = SampleSH(n);
                half3 lit = col.rgb * (ambient + mainLight.color * ndl * atten);
                // fixed-direction facet shading so the waves read even in dark/foggy scenes
                float facet = 0.55 + 0.9 * saturate(dot(n, normalize(float3(0.45, 0.75, 0.35))) - 0.55) * 2.2;
                lit *= lerp(1.0, facet, _FacetContrast);

                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 hdir = normalize(mainLight.direction + v);
                lit += mainLight.color * pow(saturate(dot(n, hdir)), _Gloss) * _Specular * atten;

                half3 foamLit = _FoamColor.rgb * (ambient + mainLight.color * atten);
                half3 finalRgb = lerp(lit, foamLit, foam);
                half alpha = saturate(lerp(col.a, 1.0, foam));

                finalRgb = MixFog(finalRgb, i.fogFactor);
                return half4(finalRgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
