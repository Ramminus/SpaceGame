

//1/7
#define K 0.142857142857
//3/7
#define Ko 0.428571428571

float3 mod_float3(float3 x, float y)
{
    return x - y * floor(x / y);
}
float2 mod_float2(float2 x, float y)
{
    return x - y * floor(x / y);
}

// Permutation polynomial: (34x^2 + x) mod 289
float3 Permutation_float3(float3 x)
{
    return mod_float3((34.0 * x + 1.0) * x, 289.0);
}

float2 inoise_float2(float3 P, float jitter)
{
    float3 Pi = mod_float3(floor(P), 289.0);
    float3 Pf = frac(P);
    float3 oi = float3(-1.0, 0.0, 1.0);
    float3 of = float3(-0.5, 0.5, 1.5);
    float3 px = Permutation_float3(Pi.x + oi);
    float3 py = Permutation_float3(Pi.y + oi);

    float3 p, ox, oy, oz, dx, dy, dz;
    float2 F = 1e6;

    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            p = Permutation_float3(px[i] + py[j] + Pi.z + oi); // pij1, pij2, pij3

            ox = frac(p * K) - Ko;
            oy = mod_float3(floor(p * K), 7.0) * K - Ko;
			
            p = Permutation_float3(p);
			
            oz = frac(p * K) - Ko;
		
            dx = Pf.x - of[i] + jitter * ox;
            dy = Pf.y - of[j] + jitter * oy;
            dz = Pf.z - of + jitter * oz;
			
            float3 d = dx * dx + dy * dy + dz * dz; // dij1, dij2 and dij3, squared
			
			//Find lowest and second lowest distances
            for (int n = 0; n < 3; n++)
            {
                if (d[n] < F[0])
                {
                    F[1] = F[0];
                    F[0] = d[n];
                }
                else if (d[n] < F[1])
                {
                    F[1] = d[n];
                }
            }
        }
    }
	
    return F;
}

// fractal sum, range -1.0 - 1.0
void fBm_F0_float(float3 p, int octaves, float frequency , float jitter, float lucunarity,float gain, out float sum)
{
    
    float freq = frequency, amp = 0.5;
    sum = 0;
    for (int i = 0; i < octaves; i++)
    {
        float2 F = inoise_float2(p * freq, jitter) * amp;
		
        sum += 0.1 + sqrt(F[0]);
		
        freq *= lucunarity;
        amp *= gain;
    }
    
}

//float fBm_F1_F0_float(float3 p, int octaves)
//{
//    float freq = _Frequency, amp = 0.5;
//    float sum = 0;
//    for (int i = 0; i < octaves; i++)
//    {
//        float2 F = inoise_float2(p * freq, _Jitter) * amp;
		
//        sum += 0.1 + sqrt(F[1]) - sqrt(F[0]);
		
//        freq *= _Lacunarity;
//        amp *= _Gain;
//    }
//    return sum;
//}