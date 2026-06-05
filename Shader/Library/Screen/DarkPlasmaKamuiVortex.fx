sampler2D Sampler : register(s0);

float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float uRadius;
float uStrength;
float uTime;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float2 targetUV = (uTargetPosition - uScreenPosition) / uScreenResolution;

    float2 worldDelta = (uv - targetUV) * uScreenResolution;
    float dist = length(worldDelta);
    float4 originalColor = tex2D(Sampler, uv);
    if (dist >= uRadius)
        return originalColor;

    float factor = saturate(1 - dist / uRadius);
    float angle = factor * (uTime * 0.8 + factor * uStrength * 6.28);
    float cs = cos(angle);
    float sn = sin(angle);
    float2 rotated = float2(
        worldDelta.x * cs - worldDelta.y * sn,
        worldDelta.x * sn + worldDelta.y * cs
    );

    float scale = 1 - factor * uStrength * 0.32;
    rotated *= scale;

    float2 newUV = targetUV + rotated / uScreenResolution;
    newUV = clamp(newUV, 0.001, 0.999);

    float4 newColor = tex2D(Sampler, newUV);
    float darkness = pow(factor, 3);
    newColor.rgb *= 1 - darkness;
    return newColor;
}

technique DarkPlasmaKamuiVortex
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
