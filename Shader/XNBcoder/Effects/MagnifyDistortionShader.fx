sampler2D uTexture;      // 纹理贴图
float2 uCenter;          // 放大中心点（归一化坐标）
float uRadius;           // 放大区域的半径
float uStrength;         // 放大倍率
float2 uScreenResolution; // 屏幕分辨率，防止扭曲变形

// **像素着色器**
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float4 color = tex2D(uTexture, coords); // **采样原始颜色**

// **计算当前像素到中心的偏移**
float2 offset = coords - uCenter;

// **计算修正后的屏幕比例，防止宽高比影响**
float2 aspectFix = float2(uScreenResolution.x / uScreenResolution.y, 1);
float2 rpos = offset * aspectFix;

// **计算到中心的距离**
float dist = length(rpos);

// **如果在放大区域内，进行变形**
if (dist < uRadius) {
    float scale = 1.0 + (1.0 - (dist / uRadius)) * uStrength; // 放大倍率
    coords = uCenter + offset * scale; // **调整采样坐标**

    // **确保坐标不会超出 0~1 采样范围**
    coords = clamp(coords, 0.0, 1.0);
}

// **返回变换后的颜色**
return tex2D(uTexture, coords);
}

// **定义 Technique**
technique MagnifyDistortionEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}


// 仔细调节一下这两个可变值，可玩性还是不少的
//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.MagnifyDistortionShader;
//    if (shader == null) return true;
//
//    // **获取弹幕中心点（归一化坐标）**
//    Vector2 screenCenter = (Projectile.Center - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight);
//
//    // **设置 Shader 变量**
//    shader.Parameters["uCenter"].SetValue(screenCenter);
//    shader.Parameters["uRadius"].SetValue(0.4f);   // ✅ 放大区域半径（0.2 = 20% 画面）
//    shader.Parameters["uStrength"].SetValue(0.75f); // ✅ 放大倍率（0.5 = 额外放大 50%）
//    shader.Parameters["uScreenResolution"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
//
//    // **应用 Shader**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
//
//    // **绘制弹幕**
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    Rectangle sourceRect = new Rectangle(0, Projectile.frame * texture.Height / Main.projFrames[Projectile.type], texture.Width, texture.Height / Main.projFrames[Projectile.type]);
//
//    // **计算绘制位置**
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//    Main.spriteBatch.Draw(texture, drawPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, sourceRect.Height / 2), Projectile.scale, SpriteEffects.None, 0f);
//
//    // **恢复默认绘制**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    return false;
//}