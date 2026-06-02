sampler2D uTexture;  // 贴图
float uTime;         // 时间变量
float uDarkness;     // 颜色深度（0.5 = 偏暗，1.0 = 纯黑）

// **简单随机函数**
float rand(float2 uv) {
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float4 color = tex2D(uTexture, coords);

// **透明像素不染色**
if (color.a < 0.1) return color;

// **扰动效果**
float2 offset = coords + float2(rand(coords + uTime), rand(coords - uTime)) * 0.01;
float effect = rand(offset) * 0.3; // 限制最大影响
float darkFactor = lerp(1.0, uDarkness, effect);

color.rgb *= darkFactor;
return color;
}

// **Shader Technique**
technique WormEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}


//1️⃣ HLSL ps_2_0 版本限制
//
//寄存器数量最多 31 个，复杂 for 循环、unroll 可能导致超出寄存器上限。
//动态 for 循环不能正确展开，编译器无法优化，导致错误。
//2️⃣ Shader 计算过于复杂
//
//大量数学运算（sin、dot、atan2、多个 rand() 调用） 增加了计算成本，导致 GPU 处理失败。
//尝试在 for 循环中嵌套 rand() 计算，增加寄存器需求，导致超限。

// 失败了，暂时先放弃这一个物品
//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    if (ShaderGames.WormShader == null) return true;
//
//    Effect shader = ShaderGames.WormShader;
//    if (shader == null) return true;
//
//    // **检查 Shader 变量是否存在**
//    if (shader.Parameters["uTime"] == null) return true;
//
//    // **检查贴图是否存在**
//    if (Projectile.type < 0 || Projectile.type >= TextureAssets.Projectile.Length || TextureAssets.Projectile[Projectile.type] == null)
//        return true;
//
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    if (texture == null) return true;
//
//    // **设置 Shader 变量**
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uDarkness"].SetValue(0.5f);
//
//    // **应用 Shader**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    shader.CurrentTechnique.Passes[0].Apply(); // ✅ 确保 Shader 绑定
//
//    // **绘制弹幕**
//    int frameHeight = texture.Height / Main.projFrames[Projectile.type];
//    Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//
//    Main.spriteBatch.Draw(texture, drawPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, frameHeight / 2), Projectile.scale, SpriteEffects.None, 0f);
//
//    // **恢复 SpriteBatch**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    return false;
//}




