Shader "Custom/TerrainGrassURP"
{
    Properties
    {
        // ---- Terrain colour properties (copied from the terrain material by GrassScatter) ----
        _GrassA ("Grass Light", Color) = (0.42, 0.62, 0.25, 1)
        _GrassB ("Grass Mid", Color) = (0.30, 0.50, 0.20, 1)
        _GrassC ("Grass Dark / Dry", Color) = (0.45, 0.50, 0.22, 1)
        _DirtA ("Dirt Light", Color) = (0.50, 0.38, 0.24, 1)
        _DirtB ("Dirt Dark", Color) = (0.34, 0.25, 0.16, 1)
        _WetColor ("Wet Mud", Color) = (0.22, 0.17, 0.12, 1)
        _PatchScale ("Large Patch Scale", Float) = 0.04
        _FaceVariation ("Per-Face Variation", Range(0, 0.3)) = 0.08
        _PathThreshold ("Grass/Path Threshold", Range(0, 1)) = 0.45
        _PathSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.08
        _EdgeNoise ("Edge Noise Strength", Range(0, 0.5)) = 0.15
        _CellSize ("Mesh Cell Size", Float) = 1
        _Saturation ("Saturation", Range(0, 1.5)) = 0.9
        _Bands ("Light Bands (0 = smooth)", Range(0, 8)) = 4
        _ShadowTint ("Shadow Tint", Color) = (0.55, 0.6, 0.8, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 1

        [Header(Blade Shading)]
        _LightInfluence ("Light Influence (0 = unlit)", Range(0, 1)) = 0
        _Brightness ("Brightness", Range(0.5, 2)) = 1.15
        _RootDarken ("Root Darkening", Range(0, 0.8)) = 0.15
        _TipLighten ("Tip Lightening", Range(0, 0.8)) = 0.1

        [Header(Wind)]
        _WindDirection ("Wind Direction (XZ)", Vector) = (1, 0.3, 0, 0)
        _WindSpeed ("Wind Speed", Float) = 1.6
        _WindFrequency ("Wave Frequency", Float) = 0.35
        _WindStrength ("Sway Strength", Float) = 0.06
        _WindLean ("Constant Lean", Float) = 0.03
        _GustScale ("Gust Scale", Float) = 0.08
        _GustStrength ("Gust Strength", Float) = 0.12
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GrassA, _GrassB, _GrassC;
                half4 _DirtA, _DirtB, _WetColor;
                half4 _ShadowTint;
                float _PatchScale, _FaceVariation;
                float _PathThreshold, _PathSoftness, _EdgeNoise;
                float _CellSize, _Bands, _Saturation, _AmbientStrength;
                float _RootDarken, _TipLighten;
                float _LightInfluence, _Brightness;
                float4 _WindDirection;
                float _WindSpeed, _WindFrequency, _WindStrength, _WindLean;
                float _GustScale, _GustStrength;
            CBUFFER_END

            #include "TerrainSurfaceColor.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;   // uv.y: 0 at blade base -> 1 at tip
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  albedo     : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;

                float3 rootWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 posWS  = TransformObjectToWorld(IN.positionOS.xyz);

                // ---------- wind ----------
                float2 dir = normalize(_WindDirection.xy + 1e-5);
                float2 perp = float2(-dir.y, dir.x);
                float bend = IN.uv.y * IN.uv.y;           // tips move, roots stay put
                float t = _Time.y * _WindSpeed;
                float phase = dot(rootWS.xz, dir) * _WindFrequency;
                float wave = sin(t + phase);
                float gust = valueNoise(rootWS.xz * _GustScale - dir * (_Time.y * _WindSpeed * 0.3));
                float sway = wave * _WindStrength + (gust - 0.5) * 2.0 * _GustStrength * _WindStrength + _WindLean;
                float flutter = sin(t * 2.3 + rootWS.x * 1.7 + rootWS.z * 1.3) * _WindStrength * 0.25;

                posWS.xz += (dir * sway + perp * flutter) * bend;
                posWS.y  -= abs(sway) * bend * 0.5;       // blade arcs instead of stretching

                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);

                // ---------- colour: identical to the terrain under the blade root ----------
                half3 baseCol = TerrainSurfaceAlbedo(rootWS, 0.0);
                half grad = lerp(1.0 - _RootDarken, 1.0 + _TipLighten, IN.uv.y);
                OUT.albedo = baseCol * grad;

                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 n = half3(0, 1, 0);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half ndl = saturate(dot(n, mainLight.direction));
                if (_Bands > 0.5)
                    ndl = floor(ndl * _Bands + 0.5) / _Bands;

                half atten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 lit = lerp(_ShadowTint.rgb, half3(1, 1, 1), ndl * atten);
                half3 litColor = IN.albedo * mainLight.color * lit * ndl * atten;
                litColor += IN.albedo * SampleSH(n) * _AmbientStrength;

                // unlit = just the terrain-matched albedo; blend toward lit with _LightInfluence
                half3 unlitColor = IN.albedo * _Brightness;
                half3 color = lerp(unlitColor, litColor, _LightInfluence);

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
