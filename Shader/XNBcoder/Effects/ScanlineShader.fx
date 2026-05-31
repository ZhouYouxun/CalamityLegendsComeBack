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


//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.ScanlineShader;
//    if (shader == null) return true;
//
//    // **设置 Shader 变量**
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uLineDensity"].SetValue(80f); // ✅ 控制扫描线密度
//    shader.Parameters["uOpacity"].SetValue(1.0f); // ✅ 1.0 = 完全应用黑白扫描线
//
//    // **应用 Shader**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
//
//    // **获取弹幕贴图**
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    int frameHeight = texture.Height / Main.projFrames[Projectile.type];
//    Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
//
//    // **绘制弹幕**
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//    Main.spriteBatch.Draw(texture, drawPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, frameHeight / 2), Projectile.scale, SpriteEffects.None, 0f);
//
//    // **恢复默认绘制**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    return false; // `false` 表示我们自己手动绘制了
//}