Shader "Zipper/VisionCone"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 0.16)
        _Range ("Range", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Range;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float fade : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldView : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float distanceFromApex = length(v.vertex.xyz);
                o.fade = saturate(1.0 - distanceFromApex / max(0.001, _Range));

                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldView = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 侧面看明显、顺着锥轴看几乎透明，避免玩家游进锥体时整屏发白
                float facing = saturate(abs(dot(normalize(i.worldNormal), normalize(i.worldView))));
                float alpha = _Color.a * (0.2 + 0.8 * i.fade) * facing * facing;

                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
