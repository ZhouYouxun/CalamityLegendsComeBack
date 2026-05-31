
// 失败品，失败原因：它是彩虹色切换，虽然基本效果差不多，但是颜色范围不对
//sampler2D uImage0;  // 物品的基础纹理
//float uTime;        // 时间变量
//float4 uGlowColor;  // 魔法光效的颜色
//float uGlowIntensity; // 发光强度
//float uOpacity;     // 光效透明度
//
//// HSV 转 RGB，制造色彩流动的魔法光效
//float3 HUEtoRGB(float H) {
//    float R = abs(H * 6 - 3) - 1;
//    float G = 2 - abs(H * 6 - 2);
//    float B = 2 - abs(H * 6 - 4);
//    return saturate(float3(R, G, B));
//}
//
//// 主要像素着色器
//float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
//    float4 baseColor = tex2D(uImage0, coords); // 获取物品的颜色
//
//// **跳过透明像素**【这一步非常重要，否则它会渲染整个长方体，而不是里面的弹幕贴图】
//if (baseColor.a < 0.01) return baseColor;
//
//// 计算魔法光效的噪声遮罩
//float noise = sin(coords.x * 10 + uTime) * cos(coords.y * 10 + uTime);
//noise = (noise + 1) * 0.5; // 归一化到 0 ~ 1
//
//// 让光效颜色随时间流动
//float hue = frac(uTime * 0.1 + coords.y); // 纵向色彩变化
//float3 glowRGB = HUEtoRGB(hue);
//
//// 生成最终魔法光效
//float4 glowEffect = float4(glowRGB, 1.0) * uGlowIntensity * noise;
//
//// 通过 lerp() 让光效自然融合，并保留 alpha 值
//return lerp(baseColor, glowEffect, uOpacity) * baseColor.a;
//}
//
//// 定义 Technique
//technique EnchantmentEffect{
//    pass P0 {
//        PixelShader = compile ps_2_0 PixelShaderFunction();
//    }
//}


        // 旧附魔
        //public override bool PreDraw(ref Color lightColor)
        //{
        //    // **确保 Shader 存在**
        //    Effect shader = ShaderGames.EnchantmentShader;
        //    if (shader == null) return true;

        //    // **设置 Shader 变量**
        //    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
        //    shader.Parameters["uGlowColor"].SetValue(Color.Purple.ToVector4()); // 魔法光效颜色
        //    shader.Parameters["uGlowIntensity"].SetValue(0.8f); // 光效强度
        //    shader.Parameters["uOpacity"].SetValue(0.6f); // 透明度控制

        //    // **应用 Shader**
        //    Main.spriteBatch.End();
        //    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);

        //    // **获取弹幕贴图**
        //    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
        //    int frameHeight = texture.Height / Main.projFrames[Projectile.type]; // 计算单帧高度
        //    Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight); // 选取当前帧

        //    // **绘制弹幕**
        //    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        //    Main.spriteBatch.Draw(texture, drawPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, frameHeight / 2), Projectile.scale, SpriteEffects.None, 0f);

        //    // **恢复 SpriteBatch**
        //    Main.spriteBatch.End();
        //    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        //    return false; // `false` 表示不让默认绘制进行，我们已经手动绘制了
        //}



sampler2D uImage0;  // 物品的基础纹理
float uTime;        // 时间变量
float uGlowIntensity; // 发光强度
float uOpacity;     // 光效透明度

// **Minecraft 附魔颜色选项**
static const float4 enchantColors[4] = {
    float4(0.6, 0.8, 1.0, 1.0),  // 浅蓝色
    float4(0.7, 0.9, 1.0, 1.0),  // 更亮的蓝色
    float4(0.6, 0.6, 0.7, 1.0),  // 浅灰蓝色
    float4(0.5, 0.5, 0.6, 1.0)   // 暗灰蓝色
};

// **伪随机函数**
float random(float2 p) {
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// 主要像素着色器
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float4 baseColor = tex2D(uImage0, coords); // 获取物品的颜色

// **跳过透明像素**
if (baseColor.a < 0.01) return baseColor;

// **基于坐标+时间生成伪随机索引**
float noiseFactor = random(coords * 10 + uTime * 5);
int colorIndex = (int)(noiseFactor * 4) % 4; // 生成 0-3 之间的整数
float4 selectedColor = enchantColors[colorIndex]; // 选择对应的附魔颜色

// **颜色脉动效果**
float pulse = sin(uTime * 6) * 0.5 + 0.5; // 让颜色亮度动态变化
selectedColor *= (0.8 + 0.2 * pulse); // 调整光效亮度

// **计算最终颜色**
float4 glowEffect = selectedColor * uGlowIntensity;
return lerp(baseColor, glowEffect, uOpacity) * baseColor.a; // 只影响非透明区域
}

// 定义 Technique
technique EnchantmentEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}




//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.EnchantmentShader;
//    if (shader == null) return true;

//    // **设置 Shader 变量**
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uGlowIntensity"].SetValue(0.7f); // 设置光效强度
//    shader.Parameters["uOpacity"].SetValue(0.5f); // 透明度控制

//    // **应用 Shader**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);

//    // **获取弹幕贴图**
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    int frameHeight = texture.Height / Main.projFrames[Projectile.type]; // 计算单帧高度
//    Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight); // 选取当前帧

//    // **绘制弹幕**
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//    Main.spriteBatch.Draw(texture, drawPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, frameHeight / 2), Projectile.scale, SpriteEffects.None, 0f);

//    // **恢复 SpriteBatch**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

//    return false; // `false` 表示不让默认绘制进行，我们已经手动绘制了
//}