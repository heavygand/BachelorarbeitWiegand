Shader "Car/Car Paint"
{
    Properties
    {
        _Color("Main Color (RGB)", Color) = (1,1,1,1)
        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _Shininess("Shininess", Range (0.03, 1)) = 0.078125
        _ReflectColor("Reflection Color (RGB) RefStrength (A)", Color) = (1,1,1,0.5)
        _MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
        _Cube("Reflection Cubemap", Cube) = "_Skybox" { TexGen CubeReflect }
        _FresnelPower("Fresnel Power", Range(0.05,5.0)) = 0.75
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf BlinnPhong
        #pragma target 3.0

        sampler2D   _MainTex;
        samplerCUBE _Cube;
        float4      _Color;
        float4      _ReflectColor;
        float       _Shininess;
        float       _FresnelPower;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldRefl;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutput OUT)
        {
            half4 tex = tex2D(_MainTex, IN.uv_MainTex);
            half4 col = tex * _Color;

            OUT.Albedo   = col.rgb;
            OUT.Gloss    = tex.a;
            OUT.Specular = _Shininess;

            half4 reflCol  = texCUBE(_Cube, IN.worldRefl);

            float bias    = 0.20373;
            float facing  = saturate(1 - max(dot(normalize(IN.viewDir.xyz), normalize(OUT.Normal)), 0));
            float fresnel = max(bias + (1 - bias) * pow(facing, _FresnelPower), 0);

            reflCol *= _ReflectColor.a * fresnel;

            OUT.Emission = reflCol.rgb * _ReflectColor.rgb;
            OUT.Alpha    = _Color.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
