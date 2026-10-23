#ifndef TERRAIN_SURFACE_COLOR_INCLUDED
#define TERRAIN_SURFACE_COLOR_INCLUDED

// Shared by the terrain shader and the grass shader so both always produce the
// exact same colour at a given world position.
//
// The including shader must declare these in its UnityPerMaterial CBUFFER:
//   half4 _GrassA, _GrassB, _GrassC, _DirtA, _DirtB, _WetColor;
//   float _PatchScale, _FaceVariation, _PathThreshold, _PathSoftness,
//         _EdgeNoise, _CellSize, _Saturation;

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

half3 TerrainSurfaceAlbedo(float3 wp, float influence)
{
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

    float edgeJitter = (fbm(wp.xz * 0.35) - 0.5) * _EdgeNoise;
    float pathMask = smoothstep(_PathThreshold - _PathSoftness,
                                _PathThreshold + _PathSoftness,
                                influence + edgeJitter);

    dirt = lerp(dirt, _WetColor.rgb, smoothstep(0.75, 1.0, influence) * 0.8);

    half3 albedo = lerp(grass, dirt, pathMask);
    albedo *= 1.0 + faceRand * 2.0 * _FaceVariation;

    half lum = dot(albedo, half3(0.299, 0.587, 0.114));
    return lerp(lum.xxx, albedo, _Saturation);
}

#endif
