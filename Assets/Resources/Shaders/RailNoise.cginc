#ifndef RAIL_NOISE_INCLUDED
#define RAIL_NOISE_INCLUDED

float RailHash(float3 p)
{
    p = frac(p * 0.3183099 + .1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Smooth value noise in [0,1].
float RailNoise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(lerp(RailHash(i), RailHash(i + float3(1,0,0)), f.x),
                     lerp(RailHash(i + float3(0,1,0)), RailHash(i + float3(1,1,0)), f.x), f.y),
                lerp(lerp(RailHash(i + float3(0,0,1)), RailHash(i + float3(1,0,1)), f.x),
                     lerp(RailHash(i + float3(0,1,1)), RailHash(i + float3(1,1,1)), f.x), f.y), f.z);
}

float RailFbm(float3 x)
{
    float sum = 0, amplitude = .5;
    for (int i = 0; i < 4; i++) { sum += RailNoise(x) * amplitude; x *= 2.03; amplitude *= .5; }
    return sum;
}

#endif
