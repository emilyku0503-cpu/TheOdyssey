Shader "Custom/InkPaperMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Video Texture", 2D) = "white" {}
        _PaperTex ("Paper Texture", 2D) = "white" {}
        _Threshold ("White Threshold", Range(0,1)) = 0.9
        _Softness ("Softness", Range(0.001,0.5)) = 0.05
        _InkColor ("Ink Tint", Color) = (0,0,0,1)
        _Saturation ("Saturation", Range(0,1)) = 1
        _PaperTiling ("Paper Tiling", Vector) = (1,1,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _PaperTex;
            float4 _MainTex_ST;
            float4 _PaperTex_ST;
            float _Threshold;
            float _Softness;
            fixed4 _InkColor;
            float _Saturation;
            float4 _PaperTiling;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 videoCol = tex2D(_MainTex, i.uv);

                float luminance = dot(
                    videoCol.rgb,
                    float3(0.299, 0.587, 0.114)
                );

                // White becomes transparent
                float alpha = 1.0 - smoothstep(
                    _Threshold - _Softness,
                    _Threshold,
                    luminance
                );

                float inkMask = 1.0 - luminance;

                float2 paperUV = i.uv * _PaperTiling.xy;
                fixed4 paperCol = tex2D(_PaperTex, paperUV);

                fixed3 finalRgb = paperCol.rgb * _InkColor.rgb;

                // Saturation adjustment
                float gray = dot(
                    finalRgb,
                    float3(0.299, 0.587, 0.114)
                );

                finalRgb = lerp(
                    float3(gray, gray, gray),
                    finalRgb,
                    _Saturation
                );

                return fixed4(finalRgb, alpha * inkMask);
            }
            ENDCG
        }
    }
}