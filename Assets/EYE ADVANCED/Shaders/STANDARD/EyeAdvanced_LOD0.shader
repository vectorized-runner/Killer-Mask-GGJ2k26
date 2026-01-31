Shader "EyeAdvanced/EyeAdvanced_URP"
{
    Properties
    {
        _MainTex ("Sclera Texture", 2D) = "white" {}
        _IrisColorTex ("Iris Color", 2D) = "white" {}
        _IrisTex ("Iris Mask", 2D) = "white" {}

        _EyeBump ("Eye Normal", 2D) = "bump" {}
        _CorneaBump ("Cornea Normal", 2D) = "bump" {}
        _IrisBump ("Iris Normal", 2D) = "bump" {}

        _scleraColor ("Sclera Color", Color) = (1,1,1,1)
        _irisColor ("Iris Color", Color) = (1,1,1,1)
        _illumColor ("Glow", Color) = (0,0,0,0)
        _limbalColor ("Limbal Color", Color) = (0,0,0,0)

        _irisSize ("Iris Size", Range(1.5,5)) = 2
        _scleraSize ("Sclera Size", Range(0.8,2.2)) = 1
        _pupilSize ("Pupil", Range(0,1)) = 0.3
        _parallax ("Parallax", Range(0,0.05)) = 0.02

        _smoothness ("Smoothness", Range(0,1)) = 0.75
        _specular ("Specular", Range(0,1)) = 0.5
        _brightShift ("Brightness", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_IrisColorTex);  SAMPLER(sampler_IrisColorTex);
            TEXTURE2D(_IrisTex);       SAMPLER(sampler_IrisTex);
            TEXTURE2D(_EyeBump);       SAMPLER(sampler_EyeBump);
            TEXTURE2D(_CorneaBump);    SAMPLER(sampler_CorneaBump);
            TEXTURE2D(_IrisBump);      SAMPLER(sampler_IrisBump);

            float4 _scleraColor;
            float4 _irisColor;
            float4 _illumColor;
            float4 _limbalColor;

            float _irisSize;
            float _scleraSize;
            float _pupilSize;
            float _parallax;
            float _smoothness;
            float _specular;
            float _brightShift;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(v.positionOS.xyz));
                o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDirWS);
                float3 normalWS = normalize(i.normalWS);

                float irisMask = SAMPLE_TEXTURE2D(_IrisTex, sampler_IrisTex, i.uv).b;

                // UV scaling
                float2 scleraUV = (i.uv * _scleraSize) - ((_scleraSize - 1) * 0.5);
                float2 irisUV   = (i.uv * _irisSize)   - ((_irisSize   - 1) * 0.5);

                // Parallax
                float parallaxMask = SAMPLE_TEXTURE2D(_IrisTex, sampler_IrisTex, irisUV).g;
                float2 parallaxOffset = viewDir.xy * (_parallax * parallaxMask);
                irisUV -= parallaxOffset;

                // Textures
                float3 scleraCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scleraUV).rgb * _scleraColor.rgb;
                float3 irisCol   = SAMPLE_TEXTURE2D(_IrisColorTex, sampler_IrisColorTex, irisUV).rgb * _irisColor.rgb;

                float3 albedo = lerp(scleraCol, irisCol, irisMask);
                albedo *= _brightShift;

                // Normals
                float3 nEye    = UnpackNormal(SAMPLE_TEXTURE2D(_EyeBump, sampler_EyeBump, scleraUV));
                float3 nCornea = UnpackNormal(SAMPLE_TEXTURE2D(_CorneaBump, sampler_CorneaBump, irisUV));
                normalWS = normalize(TransformTangentToWorld(lerp(nEye, nCornea, irisMask),
                                  half3x3(i.tangentWS.xyz,
                                          cross(i.normalWS, i.tangentWS.xyz) * i.tangentWS.w,
                                          i.normalWS)));

                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 lighting = albedo * mainLight.color * NdotL;

                // Specular (simple)
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normalWS, halfDir)), 64) * _specular;

                // Emission
                float3 emission = albedo * _illumColor.rgb * _illumColor.a * irisMask;

                return half4(lighting + spec + emission, 1);
            }
            ENDHLSL
        }
    }
}
