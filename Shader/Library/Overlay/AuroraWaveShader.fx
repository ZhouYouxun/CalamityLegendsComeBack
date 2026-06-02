
// 不算失败品，这个确实有激光脉动的效果，只是方向不太对
sampler2D uTexture;         // 原始纹理
float uTime;                // 时间变量
float uFlowSpeed;           // 波浪流速（明确声明）
float uIntensity;           // 极光颜色强度

// 简化的随机函数 (兼容ps_2_0)
float rand(float2 co) {
    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.54);
}

// 极简版HSV转RGB
float3 HSVtoRGB(float h) {
    float3 rgb = abs(h * 6 - float3(3, 2, 4)) - 1;
    return saturate(rgb);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 baseColor = tex2D(uTexture, coords);
    if (baseColor.a < 0.05) return baseColor;

    // 波浪效果（极简避免复杂性）
    float wave = sin(coords.y * 10.0 + uTime * uFlowSpeed) * 0.5 + 0.5;

    // 加入随机扰动
    float noise = (rand(coords.xy * 20.0 + uTime) - 0.5) * 0.3;
    wave += noise;

    // 根据波动生成颜色渐变（动态色相）
    float hue = frac(coords.y * 0.5 + uTime * 0.1);
    float3 auroraRGB = HSVtoRGB(hue);

    // 计算最终颜色
    float4 auroraColor = float4(auroraRGB, 1.0) * wave;

    // 与原始颜色融合
    float4 baseColorMix = lerp(tex2D(uTexture, coords), auroraColor, 0.7 * wave);

    return baseColorMix;
}

// Technique定义
technique AuroraWaveEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}


//public override bool PreDraw(ref Color lightColor)
//{
//    Effect shader = ShaderGames.AuroraWaveShader;
//    if (shader == null) return true;
//
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uFlowSpeed"].SetValue(1.0f);  // 控制波动速度（可调）
//
//    shader.CurrentTechnique.Passes[0].Apply();
//
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
//        DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
//
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//    Main.spriteBatch.Draw(texture, drawPosition, null, Color.White, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
//
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
//        DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    return false;
//}
