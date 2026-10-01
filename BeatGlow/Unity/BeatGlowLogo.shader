// Intended per-pixel blend for a future asset bundle built with Unity 2022.3.33f1.
// Not loaded at runtime. Beat Saber single-pass instanced rendering needs the game's
// stereo macros, and no matching editor is available to compile this safely.
// LogoMaterialFactory uses Custom/UINoGlow instead, which is unlit, transparent,
// and excluded from the bloom pre-pass. Vertex colors on the white mesh apply the
// equalizer fill that this fragment would do with _Level.
Shader "BeatGlow/Logo"
{
    Properties
    {
        _MainTex ("Art", 2D) = "white" {}
        _MaskTex ("White mask", 2D) = "black" {}
        _Level ("Equalizer level", Range(0, 1)) = 0
        _ColorLow ("Low color", Color) = (0.15, 0.75, 1, 1)
        _ColorHigh ("High color", Color) = (1, 0.2, 0.35, 1)
        _Dim ("Dim above the level", Range(0, 1)) = 0.12
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off
        Fog { Mode Off }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float _Level;
            fixed4 _ColorLow;
            fixed4 _ColorHigh;
            float _Dim;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 art = tex2D(_MainTex, i.uv);
                fixed mask = tex2D(_MaskTex, i.uv).a;
                fixed3 music = lerp(_ColorLow.rgb, _ColorHigh.rgb, saturate(_Level));
                float coverage = saturate((_Level - i.uv.y) * 32.0);
                fixed3 tint = music * lerp(_Dim, 1.0, coverage);
                fixed3 rgb = lerp(art.rgb, tint, mask);
                return fixed4(rgb, art.a);
            }
            ENDCG
        }
    }
}
