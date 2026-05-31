sampler2D uImage0 : register(s0);
float2 uScreenResolution;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
    float2 pos = float2(0.5, 0.5);
    float2 offset = (coords - pos);
    float2 rpos = offset * float2(uScreenResolution.x / uScreenResolution.y, 1);
    float dis = length(rpos);
    float r = 1.57;
    float2 target = mul(offset, float2x2(cos(r), -sin(r), sin(r), cos(r)));
    return tex2D(uImage0, pos + target);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
