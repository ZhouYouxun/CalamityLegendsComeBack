sampler2D uTexture;      // 原始纹理
float uTime;             // 全局时间
float uBurnSpeed;        // 火焰流动速度
float uIntensity;        // 火焰强度（推荐0.2~1.5）

// 简单随机函数，兼容ps_2_0
float rand(float2 uv) {
    return frac(sin(dot(uv, float2(12.98, 78.23))) * 43758.54);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 baseColor = tex2D(uTexture, coords);

    if (baseColor.a < 0.05) return baseColor;

    // 动态向上燃烧效果
    float flameLine = coords.y * 2.0 - frac(uTime * uBurnSpeed);

    // 随机火焰边缘效果
    float noise = frac(sin(dot(coords.xy * 20.0, float2(12.98, 78.23))) * 43758.54);

    // 控制火焰范围和强度
    float flame = saturate(flameLine + noise * 0.5);
    flame = flame * (1.0 - coords.y);  // 火焰顶部淡化

    // 强化火焰的锐度
    flame = saturate(pow(flame, 2.0));

    // 明亮的橙色火焰颜色（推荐橙红色）
    float4 flameColor = float4(1.0, 0.4, 0.0, baseColor.a) * flame * uIntensity;

    // 混合原始颜色与火焰效果
    return lerp(baseColor, baseColor + flameColor, flame);
}

technique FireBurnEffect
{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}


//public override bool PreDraw(ref Color lightColor)
//{
//    Effect shader = ShaderGames.FireBurnShader;
//    if (shader == null) return true;
//
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uBurnSpeed"].SetValue(1.6f);      // 可调整速度
//    shader.Parameters["uIntensity"].SetValue(5.5f);      // 火焰强度调整
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