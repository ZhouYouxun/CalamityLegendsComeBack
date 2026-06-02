sampler2D uTexture;  // 贴图
float uTime;         // 时间变量，让扫描线滚动
float uLineDensity;  // 扫描线密度
float uOpacity;      // 透明度控制

// **着色器主函数**
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0 {
    float4 color = tex2D(uTexture, coords); // 采样原始颜色

    // **转换为灰度**
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114)); // 标准黑白计算
    float4 grayColor = float4(gray, gray, gray, color.a);

    // **计算扫描线效果**
    float scanline = sin((coords.y * uLineDensity) + uTime * 3.14) * 0.2 + 0.9;

    // **应用扫描线和透明度**
    return lerp(color, grayColor * scanline, uOpacity);
}

// **定义 Shader**
technique ScanlineEffect {
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
