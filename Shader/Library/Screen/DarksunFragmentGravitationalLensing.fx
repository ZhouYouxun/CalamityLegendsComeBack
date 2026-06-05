sampler2D Sampler : register(s0);

float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float uRadius;
float uHorizonRadius;
float uStrength;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float2 targetUV = (uTargetPosition - uScreenPosition) / uScreenResolution;

    float2 worldDelta = (uv - targetUV) * uScreenResolution;
    float dist = length(worldDelta);
    float4 originalColor = tex2D(Sampler, uv);
    if (dist >= uRadius)
        return originalColor;

    float factor = saturate(1 - dist / uRadius);
    float lens = factor * factor * uStrength;
    float2 warpedDelta = worldDelta * (1 - lens);
    float2 newUV = targetUV + warpedDelta / uScreenResolution;
    newUV = clamp(newUV, 0.001, 0.999);

    float4 color = tex2D(Sampler, newUV);
    float horizonSoftness = uHorizonRadius * 0.18;
    float horizon = saturate((uHorizonRadius - dist) / horizonSoftness);
    color.rgb *= 1 - horizon;
    return color;
}

technique DarksunFragmentGravitationalLensing
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
