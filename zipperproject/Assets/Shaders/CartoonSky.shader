Shader "Zipper/CartoonSky"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.35, 0.66, 0.95)
        _HorizonColor ("Horizon Color", Color) = (0.86, 0.94, 1.00)
        _BottomColor ("Bottom Color", Color) = (0.78, 0.90, 0.94)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _BottomColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.direction = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float height = normalize(i.direction).y;

                fixed3 topBlend = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(height), 0.55));
                fixed3 bottomBlend = lerp(_HorizonColor.rgb, _BottomColor.rgb, pow(saturate(-height), 0.7));
                fixed3 color = height >= 0.0 ? topBlend : bottomBlend;

                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
