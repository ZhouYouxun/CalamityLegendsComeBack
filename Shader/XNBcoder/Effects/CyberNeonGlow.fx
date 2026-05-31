
// 失败品失败原因不是霓虹描边，而是大量的竖线，并且是蓝色的周期性脉动
//sampler2D uTexture;       // 纹理贴图
//float2 uResolution;       // 屏幕分辨率
//float uTime;              // 全局时间
//float3 uNeonColor;        // 霓虹颜色
//float uGlowIntensity;     // 光晕强度
//float uPulseSpeed;        // 霓虹脉动速度
//float uGlowRange;         // 光晕扩散范围
//
//// **计算像素的边缘强度**
//float GetEdgeFactor(float2 coords) {
//    float4 centerColor = tex2D(uTexture, coords);
//    float edgeFactor = 0.0;
//
//    float2 offsets[4] = {
//        float2(-1, 0), float2(1, 0),
//        float2(0, -1), float2(0, 1)
//    };
//
//    for (int i = 0; i < 4; i++) {
//        float4 neighborColor = tex2D(uTexture, coords + offsets[i] / uResolution);
//        edgeFactor += distance(centerColor.rgb, neighborColor.rgb);
//    }
//
//    return saturate(edgeFactor * 4.0);
//}
//
//// **像素着色器**
//float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
//    float4 color = tex2D(uTexture, coords);
//    float edge = GetEdgeFactor(coords);
//
//    // **霓虹脉动效果**
//    float glow = sin(uTime * uPulseSpeed) * 0.5 + 0.5;
//
//    // **光晕扩散**
//    float glowMask = smoothstep(0.0, uGlowRange, edge);
//    float3 finalGlow = glowMask * glow * uGlowIntensity * uNeonColor;
//
//    // **混合霓虹光晕**
//    float3 finalColor = color.rgb + finalGlow;
//
//    return float4(finalColor, color.a);
//}
//
//// **定义 Technique**
//technique CyberNeonGlowEffect{
//    pass P0 {
//        PixelShader = compile ps_2_0 PixelShaderFunction();
//    }
//}




//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.CyberNeonGlow;
//    if (shader == null) return true;
//
//    // **设置 Shader 变量**
//    shader.Parameters["uResolution"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uNeonColor"].SetValue(new Vector3(0.1f, 0.8f, 1.0f)); // 霓虹蓝色
//    shader.Parameters["uGlowIntensity"].SetValue(3.0f); // 霓虹强度（可调）
//    shader.Parameters["uPulseSpeed"].SetValue(6.0f); // 霓虹脉动速度（可调）
//    shader.Parameters["uGlowRange"].SetValue(1.2f); // 光晕扩散范围（可调）
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








// 还是存在一些相关的问题，只能检测一个方向的，另外一个方向的线条检测不到

//sampler2D uTexture;       // 纹理贴图
//float2 uResolution;       // 屏幕分辨率
//float uTime;              // 全局时间
//float3 uNeonColor;        // 霓虹颜色
//float uGlowIntensity;     // 光晕强度
//float uPulseSpeed;        // 霓虹脉动速度
//float uGlowRange;         // 光晕扩散范围
//
//// **改进边缘检测**
//float GetEdgeFactor(float2 coords) {
//    float alphaCenter = tex2D(uTexture, coords).a;
//    float edgeFactor = 0.0;
//
//    // **改进边缘检测：检测水平、垂直、对角线变化**
//    float2 offsets[8] = {
//        float2(-1, 0), float2(1, 0),   // 水平（左右）
//        float2(0, -1), float2(0, 1),   // 垂直（上下）
//        float2(-1, -1), float2(1, 1),  // 对角线 左上、右下
//        float2(-1, 1), float2(1, -1)   // 对角线 右上、左下
//    };
//
//    for (int i = 0; i < 8; i++) {
//        float alphaNeighbor = tex2D(uTexture, coords + offsets[i] / uResolution).a;
//        edgeFactor += abs(alphaCenter - alphaNeighbor); // 计算透明度差异
//    }
//
//    return saturate(edgeFactor * 4.0);
//}
//
//// **像素着色器**
//float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
//    float4 color = tex2D(uTexture, coords);
//    float edge = GetEdgeFactor(coords);
//
//    // **霓虹脉动效果**
//    float glow = sin(uTime * uPulseSpeed) * 0.5 + 0.5;
//
//    // **光晕扩散**
//    float glowMask = smoothstep(0.0, uGlowRange, edge);
//    float3 finalGlow = glowMask * glow * uGlowIntensity * uNeonColor;
//
//    // **混合霓虹光晕**
//    float3 finalColor = color.rgb + finalGlow;
//
//    return float4(finalColor, color.a);
//}
//
//// **定义 Technique**
//technique CyberNeonGlowEffect{
//    pass P0 {
//        PixelShader = compile ps_2_0 PixelShaderFunction();
//    }
//}



//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.CyberNeonGlow;
//    if (shader == null) return true;
//
//    // **设置 Shader 变量**
//    shader.Parameters["uResolution"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uNeonColor"].SetValue(new Vector3(1.0f, 0.2f, 0.7f)); // 经典霓虹粉紫色
//    shader.Parameters["uGlowIntensity"].SetValue(4.0f); // 霓虹强度（可调）
//    shader.Parameters["uPulseSpeed"].SetValue(6.0f); // 霓虹脉动速度（可调）
//    shader.Parameters["uGlowRange"].SetValue(1.5f); // 光晕扩散范围（可调）
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


// 因此我们制作了第3版
sampler2D uTexture;       // 纹理贴图
float2 uResolution;       // 屏幕分辨率
float uTime;              // 全局时间
float3 uNeonColor;        // 霓虹颜色
float uGlowIntensity;     // 光晕强度
float uPulseSpeed;        // 霓虹脉动速度
float uGlowRange;         // 光晕扩散范围

// **改进边缘检测：仅检测透明像素是否接触不透明像素**
float GetEdgeFactor(float2 coords) {
    float alphaCenter = tex2D(uTexture, coords).a;
    if (alphaCenter > 0.0) return 0.0; // 不是透明像素，跳过

    // **检测四个方向**
    float2 offsets[4] = {
        float2(-1, 0), float2(1, 0),   // 左右
        float2(0, -1), float2(0, 1)    // 上下
    };

    for (int i = 0; i < 4; i++) {
        float alphaNeighbor = tex2D(uTexture, coords + offsets[i] / uResolution).a;
        if (alphaNeighbor > 0.0) return 1.0; // 如果相邻像素不透明，说明是边界
    }

    return 0.0; // 不是边界，返回 0
}

// **像素着色器**
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float4 color = tex2D(uTexture, coords);
    float edge = GetEdgeFactor(coords);

    // **霓虹脉动效果**
    float glow = sin(uTime * uPulseSpeed) * 0.5 + 0.5;

    // **光晕扩散**
    float glowMask = smoothstep(0.0, uGlowRange, edge);
    float3 finalGlow = glowMask * glow * uGlowIntensity * uNeonColor;

    // **混合霓虹光晕**
    float3 finalColor = color.rgb + finalGlow;

    return float4(finalColor, color.a);
}

// **定义 Technique**
technique CyberNeonGlowEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}





//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.CyberNeonGlow;
//    if (shader == null) return true;
//
//    // **设置 Shader 变量**
//    shader.Parameters["uResolution"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uNeonColor"].SetValue(new Vector3(1.0f, 0.2f, 0.7f)); // 经典霓虹粉紫色
//    shader.Parameters["uGlowIntensity"].SetValue(4.0f); // 霓虹强度（可调）
//    shader.Parameters["uPulseSpeed"].SetValue(6.0f); // 霓虹脉动速度（可调）
//    shader.Parameters["uGlowRange"].SetValue(3.5f); // 光晕扩散范围（可调）
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



