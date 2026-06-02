sampler2D uImage0;
float4 uColor;
float uOpacity;
float uTime;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR
{
    float wave = sin(uTime * 5 + coords.y * 20) * 0.5 + 0.5;
    return uColor * wave * uOpacity;
}

technique Basic
{
    pass P0
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
