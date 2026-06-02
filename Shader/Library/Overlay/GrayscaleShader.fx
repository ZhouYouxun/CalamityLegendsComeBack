
// 它会不断的旋转，一半显示颜色，一半显示灰色
sampler2D uTexture;   // 纹理贴图
float2 uCenter;       // 扩散中心点
float uTime;          // 时间变量
float uWaveSpeed;     // 扩散速度
float uLineDensity;   // 波浪密度（每个波的宽度）
float uOpacity;       // 透明度控制

// **像素着色器**
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float4 color = tex2D(uTexture, coords); // 采样原始颜色

// **转换为灰度**
float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
float4 grayColor = float4(gray, gray, gray, color.a);

// **计算当前像素到中心的距离**
float2 diff = coords - uCenter;
float dist = sqrt(dot(diff, diff));

// **计算波浪扩散效果**
float wave = sin((dist * uLineDensity) - (uTime * uWaveSpeed)) * 0.5 + 0.5;

// **混合灰度波浪和原始颜色**
return lerp(color, grayColor, wave * uOpacity);
}

// **定义 Shader**
technique GrayscaleWaveEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.GrayscaleShader;
//    if (shader == null) return true;
//
//    // **获取弹幕中心点（归一化坐标）**
//    Vector2 screenCenter = (Projectile.Center - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight);
//
//    // **设置 Shader 变量**
//    shader.Parameters["uCenter"].SetValue(screenCenter);
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uWaveSpeed"].SetValue(5.0f);  // ✅ 控制波浪扩散速度
//    shader.Parameters["uLineDensity"].SetValue(20f); // ✅ 控制波浪密度
//    shader.Parameters["uOpacity"].SetValue(1.0f);    // ✅ 控制波浪对比度
//
//    // **应用 Shader**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
//
//    // **绘制弹幕**
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    int frameHeight = texture.Height / Main.projFrames[Projectile.type];
//    Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
//
//    // **计算绘制位置**
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//    Main.spriteBatch.Draw(texture, drawPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, frameHeight / 2), Projectile.scale, SpriteEffects.None, 0f);
//
//    // **恢复默认绘制**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    return false;
//}


//fmod() 在 ps_2_0 不稳定 → 改为% （模运算），确保计算正确。
//length() 超出 ps_2_0 指令限制 → 改为 sqrt(dot(diff, diff))，减少计算量。
//smoothstep() 计算错误 → 调整 uWaveWidth 计算，确保 edge0 < edge1，防止 NaN。