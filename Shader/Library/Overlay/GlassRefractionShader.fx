sampler2D uImage0; // 主纹理
sampler2D uBackground; // 背景纹理
float uRefractStrength; // 折射强度
float uOpacity; // 透明度

// 主要像素着色器
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float4 color = tex2D(uImage0, coords);
    if (!any(color)) return color; // 透明区域不折射

    // 计算折射偏移
    float2 offset = float2(sin(coords.y * 50) * 0.005, cos(coords.x * 50) * 0.005) * uRefractStrength;
    float4 bgColor = tex2D(uBackground, coords + offset);

    // 透明融合
    return lerp(bgColor, color, uOpacity);
}

// 定义 Technique
technique Basic{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
