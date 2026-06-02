sampler2D Sampler : register(s0); // 原始屏幕

float2 uCenter;    // 黑洞中心 (0~1)，可动态传入 Main.screenPosition
float uRadius;     // 黑洞影响半径 (0~0.5)，如 0.3
float uStrength;   // 扭曲强度 (建议 0.05 ~ 0.2)
float uTime;       // 游戏累计时间 (用于动态扭曲)

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    // ========== 1️⃣ 计算中心距离 ==========
    float2 delta = uv - uCenter;
    float dist = length(delta);

    // ========== 2️⃣ 计算扭曲因子 ==========
    // factor = 0 (黑洞外), factor = 1 (黑洞中心)
    float factor = saturate(1 - dist / uRadius);

    // ========== 3️⃣ 黑洞中心黑色淡入吞噬 ==========
    // 不使用硬切黑色，而是渐变暗淡
    float darkness = pow(factor, 3); // 立方加快近中心暗化
    float4 color = tex2D(Sampler, uv);
    color.rgb *= (1 - darkness);

    // ========== 4️⃣ 扭曲核心逻辑 ==========
    // 使用你师傅教的：offset * 缩放 + 旋转矩阵
    // 使黑洞越近旋转越剧烈，远处不旋转

    // 动态旋转角度：时间变化形成螺旋吸扭
    float baseAngle = uTime * 0.5; // 控制旋转速率
    float twist = factor * uStrength * 6.28; // 6.28=2PI，旋转圈数可控
    float angle = baseAngle + twist;

    // 构建旋转矩阵
    float cs = cos(angle);
    float sn = sin(angle);
    float2 rotated = float2(
        delta.x * cs - delta.y * sn,
        delta.x * sn + delta.y * cs
    );

    // ========== 5️⃣ 模拟“吸入”缩放 ==========
    // 因黑洞吸收空间而产生局部收缩
    float scale = 1 - factor * uStrength * 0.5;
    rotated *= scale;

    // ========== 6️⃣ 计算新UV并防止越界 ==========
    float2 newUV = uCenter + rotated;
    newUV = clamp(newUV, 0.001, 0.999);

    // ========== 7️⃣ 取样新位置的像素，并再乘暗化 ==========
    float4 newColor = tex2D(Sampler, newUV);
    newColor.rgb *= (1 - darkness);

    return newColor;
}

technique BlackHoleDistortion{
    pass Pass1 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
