sampler2D uImage0; // 主纹理
float uTime;       // 时间变量
float uDistortionStrength; // 扭曲强度

// 主要像素着色器
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    // 计算波浪扭曲偏移
    float wave = sin(uTime * 3 + coords.y * 15) * 0.02 * uDistortionStrength;
    coords.x += wave; // 在 x 轴上增加偏移

    // 读取被扭曲后的颜色
    float4 color = tex2D(uImage0, coords);
    return color;
}

// 定义 Technique
technique Basic{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
