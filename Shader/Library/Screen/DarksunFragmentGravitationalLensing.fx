sampler2D Sampler : register(s0);

float2 uCenter;
float uRadius;
float uHorizonRadius;
float uStrength;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float2 delta = uv - uCenter;
    float dist = length(delta);
    float factor = saturate(1 - dist / uRadius);

    float lens = factor * factor * uStrength;
    float2 newUV = uv - delta * lens;
    newUV = clamp(newUV, 0.001, 0.999);

    float4 color = tex2D(Sampler, newUV);
    color.rgb *= 1 - factor * 0.1;

    float horizonSoftness = uHorizonRadius * 0.18;
    float horizon = saturate((uHorizonRadius - dist) / horizonSoftness);
    color.rgb *= 1 - horizon;

    return color;
}

technique DarksunFragmentGravitationalLensing
{
    pass Pass1 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
