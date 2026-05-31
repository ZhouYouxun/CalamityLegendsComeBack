sampler2D uImage0; // 主纹理
float uTime;       // 时间变量
float4 uColor;     // 颜色变量
float uOpacity;    // 透明度

// HSV 转 RGB（让颜色随时间变化）
float3 HUEtoRGB(float H) {
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R, G, B));
}

// 主要像素着色器
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float4 color = tex2D(uImage0, coords); // 读取原始颜色
    if (!any(color)) return color; // 如果颜色是空的，直接返回

    float hue = frac(uTime * 0.2 + coords.x); // 随时间变化的色相
    float3 rgb = HUEtoRGB(hue); // 转换为 RGB
    float4 finalColor = float4(rgb, 1.0) * uOpacity; // 应用透明度
    return finalColor;
}

// 定义 Technique
technique Basic{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
