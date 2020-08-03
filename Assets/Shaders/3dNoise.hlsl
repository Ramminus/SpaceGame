float hash_float(float n)
{
    return frac(sin(n) * 43758.5453);
}

void noise_float(float3 x, float scale, out float value)
{
    // The noise function returns a value in the range -1.0f -> 1.0f
    x *= scale;
    float3 p = floor(x);
    float3 f = frac(x);

    f = f * f * (3.0 - 2.0 * f);
    float n = p.x + p.y * 57.0 + 113.0 * p.z;

    value = lerp(lerp(lerp(hash_float(n + 0.0), hash_float(n + 1.0), f.x),
        lerp(hash_float(n + 57.0), hash_float(n + 58.0), f.x), f.y),
        lerp(lerp(hash_float(n + 113.0), hash_float(n + 114.0), f.x),
            lerp(hash_float(n + 170.0), hash_float(n + 171.0), f.x), f.y), f.z);
}