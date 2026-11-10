Shader "Zipper/OceanSurface"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.42, 0.86, 0.88, 0.72)
        _DeepColor ("Deep Color", Color) = (0.10, 0.48, 0.72, 0.90)
        _WaveAmplitude ("Wave Amplitude", Float) = 0.16
        _WaveFrequency ("Wave Frequency", Float) = 0.35
        _WaveSpeed ("Wave Speed", Float) = 1.1
        _FresnelPower ("Fresnel Power", Float) = 3.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "ForceNoShadowCasting" = "True"
        }
        LOD 200

        Pass
        {
            Name "SURFACE"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _ShallowColor;
            fixed4 _DeepColor;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _FresnelPower;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                float t = _Time.y * _WaveSpeed;
                float wave = sin(world.x * _WaveFrequency + t) * cos(world.z * _WaveFrequency * 0.8 + t * 0.7);
                world.y += wave * _WaveAmplitude;

                o.pos = mul(UNITY_MATRIX_VP, world);
                o.worldPos = world.xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, float3(0.0, 1.0, 0.0)));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fresnel = pow(1.0 - saturate(dot(viewDir, i.worldNormal)), _FresnelPower);

                // 岸线附近最浅（通透），远离岸线（海心与远海）更深
                float radius = length(i.worldPos.xz);
                float shore = 1.0 - saturate(abs(radius - 44.0) / 26.0);

                fixed4 color = lerp(_DeepColor, _ShallowColor, shore);
                color.rgb += fresnel * 0.05;
                color.a = lerp(0.80, 0.95, fresnel);
                return color;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
