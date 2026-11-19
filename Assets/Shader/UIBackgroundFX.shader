// 게임오버 배경 그림(한 장)의 일부 영역만 살아 움직이게 하는 UI 셰이더.
//  - 찢어진 돛: 매달린 위쪽은 고정, 아래 끝으로 갈수록 바람에 펄럭임
//  - 바다의 하얀 물결선: 천천히 좌우로 일렁이고, 흰 선 위로 반짝임이 흐름
// 영역은 UV(0~1, 왼쪽 아래가 0,0) 사각형으로 지정. 머티리얼 인스펙터에서 조절 가능.
Shader "TheOdyssey/UI/BackgroundFX"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Sail 1  xMin yMin xMax yMax  (attached at the top))]
        _Sail1 ("Sail 1 Rect", Vector) = (0.808, 0.345, 0.888, 0.492)
        [Header(Sail 2)]
        _Sail2 ("Sail 2 Rect", Vector) = (0.893, 0.340, 0.958, 0.430)
        _SailAmp ("Sail Flutter Strength", Range(0, 0.01)) = 0.0042
        _SailSpeed ("Sail Flutter Speed", Range(0, 10)) = 3.2
        _SailDarkKeep ("Keep Dark Wood Still (luma)", Range(0, 1)) = 0.45

        [Header(Sea  xMin yMin xMax yMax)]
        _Sea ("Sea Rect", Vector) = (0.05, 0.215, 0.72, 0.37)
        _WaveAmp ("Wave Sway Strength", Range(0, 0.01)) = 0.0032
        _WaveSpeed ("Wave Sway Speed", Range(0, 5)) = 0.9
        _Shimmer ("Shimmer Strength", Range(0, 1)) = 0.35
        _ShimmerSpeed ("Shimmer Speed", Range(0, 5)) = 1.3

        [HideInInspector] _FxTime ("Time (driven by script)", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 worldPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _Sail1, _Sail2, _Sea;
            float _SailAmp, _SailSpeed, _SailDarkKeep, _WaveAmp, _WaveSpeed, _Shimmer, _ShimmerSpeed, _FxTime;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // soft 0..1 mask for a rect (xMin,yMin,xMax,yMax) with feathered edges
            float RectMask (float2 uv, float4 r, float feather)
            {
                float2 a = smoothstep(r.xy, r.xy + feather, uv);
                float2 b = 1 - smoothstep(r.zw - feather, r.zw, uv);
                return a.x * a.y * b.x * b.y;
            }
            // judge colours as they look in the PNG (the project renders in Linear space)
            float3 ToSRGB (float3 c)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return c;
                #else
                return LinearToGammaSpace(c);
                #endif
            }
            float Luma (float3 c) { return dot(ToSRGB(c), float3(0.299, 0.587, 0.114)); }

            float2 SailOffset (float2 uv, float4 r, float phase, float t)
            {
                float m = RectMask(uv, r, 0.008);
                // 0 where the cloth hangs from (top), 1 at the free, torn bottom edge
                float hang = saturate((r.w - uv.y) / max(r.w - r.y, 1e-4));
                hang = hang * hang;
                float w = sin(t * _SailSpeed + uv.y * 70 + uv.x * 25 + phase)
                        + 0.5 * sin(t * _SailSpeed * 1.9 + uv.y * 130 + phase * 2.3);
                return float2(w, 0.35 * sin(t * _SailSpeed * 1.3 + uv.x * 90 + phase)) * _SailAmp * hang * m;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _FxTime;
                float2 uv = i.uv;
                fixed4 orig = tex2D(_MainTex, uv);

                // ---- sails ----
                float2 sailOff = SailOffset(uv, _Sail1, 0.0, t) + SailOffset(uv, _Sail2, 1.7, t);
                // ---- sea: rows sway sideways like slow swells ----
                float seaM = RectMask(uv, _Sea, 0.02);
                float sway = sin(t * _WaveSpeed + uv.y * 55) * 0.7 + sin(t * _WaveSpeed * 0.63 + uv.y * 23 + 1.3) * 0.3;
                // swells also lift and drop a little, travelling across the sea
                float heave = sin(t * _WaveSpeed * 1.25 + uv.x * 18 + uv.y * 30) * 0.45;
                float2 seaOff = float2(sway, heave) * _WaveAmp * seaM;

                float2 uv2 = uv + sailOff + seaOff;
                fixed4 col = tex2D(_MainTex, uv2);

                // keep the dark mast / yard / ropes perfectly still: only move light (cloth/sky) pixels
                float sailWeight = saturate(length(sailOff) * 4000);
                float keep = smoothstep(_SailDarkKeep - 0.08, _SailDarkKeep + 0.02, min(Luma(orig.rgb), Luma(col.rgb)));
                col = lerp(orig, col, lerp(1, keep, sailWeight));

                // ---- shimmer travelling along the white wave lines ----
                float3 s = ToSRGB(col.rgb);
                float l = dot(s, float3(0.299, 0.587, 0.114));
                float mx = max(s.r, max(s.g, s.b)), mn = min(s.r, min(s.g, s.b));
                float sat = (mx - mn) / max(mx, 1e-4);
                // measured in the PNG: wave lines luma ~0.86 / sat ~0.33, sea ~0.57, wet sand ~0.81 / 0.42, bright sand ~0.91
                float waveLine = smoothstep(0.80, 0.84, l) * (1 - smoothstep(0.885, 0.90, l)) * (1 - smoothstep(0.38, 0.41, sat)) * seaM;
                float glint = pow(saturate(sin(t * _ShimmerSpeed - uv.x * 38 + uv.y * 60) * 0.5 + 0.5), 6);
                col.rgb += waveLine * glint * _Shimmer;

                col = (col + _TextureSampleAdd) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
