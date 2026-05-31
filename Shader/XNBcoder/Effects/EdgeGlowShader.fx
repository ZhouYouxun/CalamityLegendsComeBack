sampler2D uImage0; // 主纹理
float uThreshold;   // 边缘检测阈值
float4 uGlowColor;  // 发光颜色

// Sobel 算子计算梯度（用于检测边缘）
float EdgeDetection(float2 coords) {
    float2 offsets[4] = { float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1) };
    float4 color = tex2D(uImage0, coords);
    float edge = 0.0;

    for (int i = 0; i < 4; i++) {
        float4 sample = tex2D(uImage0, coords + offsets[i] * 0.002);
        edge += abs(sample.r - color.r) + abs(sample.g - color.g) + abs(sample.b - color.b);
    }

    return edge;
}

// 主要像素着色器
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float edge = EdgeDetection(coords);

// 如果边缘强度高于阈值，则使用发光颜色
if (edge > uThreshold)
    return uGlowColor;

// 否则正常显示
return tex2D(uImage0, coords);
}

// 定义 Technique
technique Basic{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
