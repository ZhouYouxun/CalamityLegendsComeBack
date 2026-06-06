sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uTime;
float uPulseSpeed;
float uVoidStrength;
float uBandDensity;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float InverseLerp(float a, float b, float value)
{
    return saturate((value - a) / (b - a));
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates.xy;
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;

    float cross = abs(coords.y - 0.5) * 2.0;
    float center = saturate(1.0 - cross);
    float edge = saturate(1.0 - abs(cross - 0.62) * 2.6);

    float noiseA = tex2D(uImage1, coords * float2(1.5, 0.85) - float2(uTime * 0.72, 0.0)).r;
    float noiseB = tex2D(uImage1, coords * float2(3.1, 1.4) + float2(uTime * 0.22, 0.0)).g;
    float brokenBands = sin(coords.x * uBandDensity + noiseB * 4.0 - uTime * uPulseSpeed) * 0.5 + 0.5;
    float pulse = sin(uTime * (uPulseSpeed + 1.8) + coords.x * 27.0 + input.Color.r * 3.0) * 0.5 + 0.5;

    float3 tint = lerp(uColor, input.Color.rgb, 0.42);
    float3 bright = lerp(uSecondaryColor, float3(1.0, 1.0, 1.0), center * 0.35 + pulse * 0.18);
    float3 color = lerp(tint, bright, saturate(center * 0.64 + brokenBands * 0.32));
    color = lerp(color, float3(0.01, 0.01, 0.01), saturate(edge * uVoidStrength * (0.42 + noiseA * 0.34)));
    color += edge * uSecondaryColor * (0.2 + pulse * 0.22);

    float rootFade = InverseLerp(0.0, 0.1, coords.x);
    float tailFade = InverseLerp(1.0, 0.58, coords.x);
    float vertexOpacity = saturate(max(input.Color.a, dot(input.Color.rgb, float3(0.333, 0.333, 0.333))));
    float alpha = saturate((center * 0.74 + edge * 0.46 + noiseA * 0.2) * tailFade * rootFade) * uOpacity * vertexOpacity;

    return float4(color, alpha);
}

technique SHPCMiniLaserDyeTrail
{
    pass TrailPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
