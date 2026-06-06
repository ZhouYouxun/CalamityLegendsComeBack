sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uTime;
float uPulseSpeed;
float uVoidStrength;
float uBandDensity;
float uCoreSharpness;
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
    float core = pow(saturate(1.0 - cross), max(0.35, uCoreSharpness));
    float rim = pow(saturate(cross), 2.0);
    float edgeMask = pow(saturate(1.0 - abs(cross - 0.78) * 2.4), 2.0);

    float scrollA = uTime * (0.44 + uPulseSpeed * 0.08);
    float scrollB = uTime * (0.18 + uPulseSpeed * 0.05);
    float streak = tex2D(uImage1, coords - float2(scrollA, 0.0)).r;
    float hotStreak = tex2D(uImage1, coords * float2(1.7, 0.85) + float2(scrollB, 0.0)).g;
    float axialBand = sin(coords.x * uBandDensity - uTime * uPulseSpeed + hotStreak * 2.2) * 0.5 + 0.5;
    float pulse = sin(uTime * uPulseSpeed + coords.x * 18.0) * 0.5 + 0.5;

    float3 vertexTint = max(input.Color.rgb, float3(0.08, 0.08, 0.08));
    float3 cold = lerp(uColor, vertexTint, 0.38);
    float3 bright = lerp(uSecondaryColor, float3(1.0, 1.0, 1.0), core * (0.45 + pulse * 0.22));
    float3 horizon = lerp(float3(0.005, 0.005, 0.005), uColor * 0.16, streak * 0.35);

    float3 dyed = lerp(cold, bright, saturate(core * 0.82 + axialBand * 0.22));
    dyed = lerp(dyed, horizon, saturate(rim * uVoidStrength));
    dyed += edgeMask * uSecondaryColor * (0.34 + pulse * 0.26);
    dyed += core * hotStreak * 0.32;

    float sideFade = saturate(1.0 - pow(cross, 2.7));
    float endFade = InverseLerp(0.0, 0.12, coords.x) * InverseLerp(1.0, 0.72, coords.x);
    float vertexOpacity = saturate(max(input.Color.a, dot(input.Color.rgb, float3(0.333, 0.333, 0.333))));
    float alpha = saturate((core * 0.82 + edgeMask * 0.48 + streak * 0.28) * sideFade * endFade) * uOpacity * vertexOpacity;

    return float4(dyed, alpha);
}

technique SHPCLaserDyeTrail
{
    pass TrailPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
