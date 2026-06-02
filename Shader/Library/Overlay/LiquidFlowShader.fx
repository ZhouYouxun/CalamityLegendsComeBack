sampler2D uTexture;             // 纹理贴图
float uTime;                    // 时间变量
float2 uFlowDirection;          // 扭曲方向向量 (如：(1,0)表示横向，(0,1)竖向)
float uFlowIntensity;           // 扭曲强度
float uFlowSpeed;               // 扭曲速度

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    // 基础颜色采样
    float4 baseColor = tex2D(uTexture, coords);

// 计算扭曲偏移量（使用可控方向与强度）
float wave = sin(dot(coords, uFlowDirection) * 20 + uTime * uFlowSpeed);

// 根据强度控制扭曲偏移量
float2 offset = uFlowIntensity * uFlowDirection * wave * 0.01;

// 应用偏移采样扭曲后的颜色
float4 distortedColor = tex2D(uTexture, coords + offset);

// 将扭曲颜色与基础颜色混合
return lerp(baseColor, distortedColor, 0.5);
}

// Shader Technique定义
technique LiquidFlowEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}



//public override bool PreDraw(ref Color lightColor)
//{
//    Effect shader = ShaderGames.LiquidFlowShader;
//    if (shader == null) return true;
//
//    // 设置Shader动态参数
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uFlowIntensity"].SetValue(5f);           // 扭曲强度（动态可调）
//    shader.Parameters["uFlowSpeed"].SetValue(4.0f);            // 流动速度（可调）
//    shader.Parameters["uFlowDirection"].SetValue(new Vector2(1f, 0.5f)); // X和Y方向（可调）
//
//    // 应用Shader绘制
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
//        DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
//
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//    Main.spriteBatch.Draw(texture, drawPosition, null, Color.White, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
//
//    // 恢复默认绘制模式
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
//        DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    return false;
//}