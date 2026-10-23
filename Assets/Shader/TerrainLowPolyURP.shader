Shader "Custom/TerrainLowPolyURP"
{
    Properties
    {
        [Header(Grass)]
        _GrassA ("Grass Light", Color) = (0.42, 0.62, 0.25, 1)
        _GrassB ("Grass Mid", Color) = (0.30, 0.50, 0.20, 1)
        _GrassC ("Grass Dark / Dry", Color) = (0.45, 0.50, 0.22, 1)
        _PatchScale ("Large Patch Scale", Float) = 0.04
        _FaceVariation ("Per-Face Variation", Range(0, 0.3)) = 0.08

        [Header(Path)]
        _DirtA ("Dirt Light", Color) = (0.50, 0.38, 0.24, 1)
        _DirtB ("Dirt Dark", Color) = (0.34, 0.25, 0.16, 1)
        _WetColor ("Wet Mud (river centre)", Color) = (0.22, 0.17, 0.12, 1)
        _PathThreshold ("Grass/Path Threshold", Range(0, 1)) = 0.45
        _PathSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.08
        _EdgeNoise ("Edge Noise Strength", Range(0, 0.5)) = 0.15

        [Header(Low Poly Look)]
        _CellSize ("Mesh Cell Size (mapSize / resolution)", Float) = 1
        _Bands ("Light Bands (0 = smooth)", Range(0, 8)) = 4
        _ShadowTint ("Shadow Tint", Color) = (0.55, 0.6, 0.8, 1)
        _Saturation ("Saturation", Range(0, 1.5)) = 0.9
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // One CBUFFER shared by all passes keeps the shader SRP Batcher compatible.
        CBUFFER_START(UnityPerMaterial)
            half4 _GrassA, _GrassB, _GrassC;
            half4 _DirtA, _DirtB, _WetColor;
            half4 _ShadowTint;
            float _PatchScale, _FaceVariation;
            float _PathThreshold, _PathSoftness, _EdgeNoise;
            float _CellSize, _Bands, _Saturation, _AmbientStrength;
        CBUFFER_END
        ENDHLSL

        // ------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;   // r = path influence
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float  influence  : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
            };

            // ---------- helpers ----------
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                return valueNoise(p) * 0.6 + valueNoise(p * 2.07) * 0.3 + valueNoise(p * 4.3) * 0.1;
            }

            half BandLight(half ndl)
            {
                if (_Bands > 0.5)
                    ndl = floor(ndl * _Bands + 0.5) / _Bands;
                return ndl;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.influence = IN.color.r;
                OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wp = IN.positionWS;

                // Flat (faceted) normal from screen-space derivatives
                float3 n = normalize(cross(ddx(wp), ddy(wp)));
                if (n.y < 0) n = -n;

                // Grid cell + which triangle of the cell (matches TerrainGenerator index order)
                float2 cellUV = wp.xz / max(_CellSize, 0.0001);
                float2 cell = floor(cellUV);
                float2 f = frac(cellUV);
                float tri = step(1.0, f.x + f.y);
                float faceRand = hash21(cell + tri * 17.31) - 0.5;

                // Large soft colour patches
                float patch  = fbm(wp.xz * _PatchScale);
                float patch2 = fbm(wp.xz * _PatchScale * 2.3 + 40.0);

                half3 grass = lerp(_GrassB.rgb, _GrassA.rgb, smoothstep(0.35, 0.65, patch));
                grass = lerp(grass, _GrassC.rgb, smoothstep(0.55, 0.75, patch2) * 0.7);

                float dirtMix = saturate(hash21(cell * 1.7 + tri * 9.1) * 0.6 + fbm(wp.xz * 0.2) * 0.6);
                half3 dirt = lerp(_DirtB.rgb, _DirtA.rgb, dirtMix);

                float influence = IN.influence;
                float edgeJitter = (fbm(wp.xz * 0.35) - 0.5) * _EdgeNoise;
                float pathMask = smoothstep(_PathThreshold - _PathSoftness,
                                            _PathThreshold + _PathSoftness,
                                            influence + edgeJitter);

                dirt = lerp(dirt, _WetColor.rgb, smoothstep(0.75, 1.0, influence) * 0.8);

                half3 albedo = lerp(grass, dirt, pathMask);
                albedo *= 1.0 + faceRand * 2.0 * _FaceVariation;

                half lum = dot(albedo, half3(0.299, 0.587, 0.114));
                albedo = lerp(lum.xxx, albedo, _Saturation);

                // ---------- lighting ----------
                float4 shadowCoord = TransformWorldToShadowCoord(wp);
                Light mainLight = GetMainLight(shadowCoord);

                half ndl = BandLight(saturate(dot(n, mainLight.direction)));
                half atten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 lit = lerp(_ShadowTint.rgb, half3(1, 1, 1), ndl * atten);
                half3 color = albedo * mainLight.color * lit * ndl * atten;

                // ambient (spherical harmonics, using the flat normal)
                color += albedo * SampleSH(n) * _AmbientStrength;

                #ifdef _ADDITIONAL_LIGHTS
                uint count = GetAdditionalLightsCount();
                for (uint i = 0u; i < count; ++i)
                {
                    Light l = GetAdditionalLight(i, wp);
                    half a = BandLight(saturate(dot(n, l.direction)))
                             * l.distanceAttenuation * l.shadowAttenuation;
                    color += albedo * l.color * a;
                }
                #endif

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // ------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half DepthFrag(Varyings IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack Off
}
