sampler2D uTexture;        // 原始纹理
float2 uScreenPosition;    // 弹幕在屏幕上的绝对位置
float uPixelSize;          // 马赛克强度（屏幕单位）

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    // 使用弹幕的屏幕绝对坐标进行马赛克划分
    float2 screenCoords = coords + uScreenPosition;

// 根据屏幕坐标划分马赛克
float2 pixelatedScreenCoords = floor(screenCoords / uPixelSize) * uPixelSize;

// 转换回弹幕纹理空间
float2 pixelatedUV = pixelatedScreenCoords - uScreenPosition;

// 采样纹理颜色
return tex2D(uTexture, pixelatedUV);
}

technique PixelationEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}


//public override bool PreDraw(ref Color lightColor)
//{
//    Effect shader = ShaderGames.PixelationShader;
//    if (shader == null) return true;
//
//    // 弹幕屏幕坐标的绝对位置
//    Vector2 screenPos = Projectile.position - Main.screenPosition;
//
//    // 设置shader参数
//    shader.Parameters["uScreenPosition"].SetValue(screenPos);
//    shader.Parameters["uPixelSize"].SetValue(0.3f); // 数值越大，马赛克效果越明显
//
//    shader.CurrentTechnique.Passes[0].Apply();
//
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
//        DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
//
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//
//    Main.spriteBatch.Draw(texture, drawPosition, null, Color.White, Projectile.rotation,
//        texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
//
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
//        DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    return false;
//}
