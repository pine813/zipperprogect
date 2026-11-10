Shader "Zipper/SoftSphere"
{
    Properties
    {
        _Color ("Color", Color) = (0.9, 0.97, 1.0, 0.55)
        _RimPower ("Rim Power", Float) = 2.0
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
            float _RimPower;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldView : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldView = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float facing = saturate(dot(normalize(i.worldNormal), normalize(i.worldView)));
                float rim = pow(1.0 - facing, _RimPower);

                float alpha = _Color.a * (0.22 + 0.78 * rim);
                fixed3 color = _Color.rgb + rim * 0.25;

                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
