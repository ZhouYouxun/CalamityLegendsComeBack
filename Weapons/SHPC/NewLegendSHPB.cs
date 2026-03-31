using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class NewLegendSHPB : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/SHPB";

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;

        public new string LocalizationCategory => "Projectiles";

        public ref float ExplodeTimer => ref Projectile.ai[2];

        public bool CanExplodeFromProximity
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value.ToInt();
        }

        private int pulseTimer = 0;
        private float shrinkStartScale = 0f;

        private static readonly DefaultEffect DefaultEffectInstance = new();
        private RulesOfEffect cachedEffect;


        //private RulesOfEffect CurrentEffect
        //{
        //    get
        //    {
        //        var effect = EffectRegistry.GetEffectByID((int)Projectile.ai[0]);
        //        return effect ?? DefaultEffectInstance;
        //    }
        //}
        private RulesOfEffect CurrentEffect => cachedEffect ?? DefaultEffectInstance;
        private Color ThemeColor => CurrentEffect.EffectID == -1 ? new Color(205, 205, 205) : CurrentEffect.ThemeColor;
        private Color StartColor => CurrentEffect.EffectID == -1 ? new Color(245, 245, 245) : CurrentEffect.StartColor;
        private Color EndColor => CurrentEffect.EffectID == -1 ? new Color(125, 125, 125) : CurrentEffect.EndColor;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.alpha = 0;
            Projectile.scale = 0f;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];

            cachedEffect = EffectRegistry.GetEffectByID((int)Projectile.ai[0]) ?? DefaultEffectInstance;

            cachedEffect.OnSpawn(Projectile, owner);

            // 同步粒子系数（关键）
            SquishyLightParticleFactor = cachedEffect.SquishyLightParticleFactor;
            ExplosionPulseFactor = cachedEffect.ExplosionPulseFactor;

            // 同步光芒控制
            GlowScaleFactor = cachedEffect.GlowScaleFactor;
            GlowIntensityFactor = cachedEffect.GlowIntensityFactor;
        }
        // 是否启用默认减速（默认开启）
        public virtual bool EnableDefaultSlowdown => true;
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // ===== 默认发光 =====
            float lightStrength = Main.rand.Next(90, 111) * 0.01f;
            lightStrength *= Main.essScale;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * lightStrength * 0.55f);

            // ===== 帧动画 =====
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame > 3)
                    Projectile.frame = 0;
            }

            // ===== 默认减速 =====
            if (CurrentEffect.EnableDefaultSlowdown)
                Projectile.velocity *= 0.98f;

            // ===== 默认接近爆炸 =====
            float explodeRange = 250f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                if (!Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                float npcDist = Vector2.Distance(Projectile.Center, npc.Center);

                if (npcDist < explodeRange)
                {
                    explodeRange = npcDist;
                    CanExplodeFromProximity = true;
                }
            }

            // ===== 插件AI =====
            CurrentEffect.AI(Projectile, owner);

            // ===== 爆炸逻辑 =====
            if (CanExplodeFromProximity)
            {
                ExplodeTimer++;

                if (ExplodeTimer >= 60f)
                    Projectile.Kill();

                Projectile.scale = MathHelper.Clamp(Projectile.scale - 0.075f, 0.5f, 1.9f);
            }
            else
            {
                if (Projectile.timeLeft >= 285)
                {
                    Projectile.scale += 0.125f;
                    if (Projectile.scale > 1.5f)
                        Projectile.scale = 1.5f;
                }
                else if (Projectile.timeLeft >= 45)
                {
                    pulseTimer++;
                    float pulse = CalamityUtils.SineBumpEasing(pulseTimer / 75f, (int)1f);
                    Projectile.scale = MathHelper.Lerp(1.5f, 1.9f, pulse);
                }
                else
                {
                    if (shrinkStartScale <= 0f)
                        shrinkStartScale = Projectile.scale;

                    Projectile.scale = Utils.Remap(Projectile.timeLeft, 45, 0, shrinkStartScale, 0.35f, true);
                }
            }
            Projectile.scale = 1f;
            // ===== hitbox =====
            Projectile.ExpandHitboxBy((int)(24f * Projectile.scale));

            // ===== 朝向 =====
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // ===== 粒子 =====
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(Projectile.Center, 1, 1, DustID.TintableDustLighted);
                dust.noGravity = true;
                dust.color = ThemeColor;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            CurrentEffect.ModifyHitNPC(Projectile, owner, target, ref modifiers);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            CurrentEffect.OnHitNPC(Projectile, owner, target, hit, damageDone);
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Player owner = Main.player[Projectile.owner];

            // 交给Effect处理
            return CurrentEffect.OnTileCollide(Projectile, owner, oldVelocity);
        }
        
        
        // SquishyLightParticle强度系数（默认1，可被Effect修改）
        public float SquishyLightParticleFactor = 1f;
        // 光球强度
        public float ExplosionPulseFactor = 1f;
        public override void OnKill(int timeLeft)
        {
            Player owner = Main.player[Projectile.owner];


            // 经典音效
            SoundEngine.PlaySound(SoundID.Item105, Projectile.Center);

            // 屏幕震动效果
            float shakePower = 5f;
            float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower =
                Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceFactor);




            // 光粒子散射特效
            if (Projectile.owner == Main.myPlayer && SquishyLightParticleFactor > 0f)
            {
                // ===== SquishyLightParticle 爆散 =====
                int particleCount = (int)(25 * SquishyLightParticleFactor);

                for (int i = 0; i < particleCount; i++)
                {
                    Vector2 velocity = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(6f, 11f) * SquishyLightParticleFactor * 0.5f;

                    float scale = Main.rand.NextFloat(0.8f, 1.4f) * SquishyLightParticleFactor * 0.5f;

                    Color particleColor = Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat());

                    int lifetime = Main.rand.Next(30, 45);

                    SquishyLightParticle particle = new(
                        Projectile.Center,
                        velocity,
                        scale,
                        particleColor,
                        lifetime
                    );

                    GeneralParticleHandler.SpawnParticle(particle);
                }
            }


            // 光球多层爆炸
            if (Projectile.owner == Main.myPlayer && ExplosionPulseFactor > 0f)
            {
                // ===== 爆炸核心参数 =====
                float startSize = 0.07f * ExplosionPulseFactor;
                float endSize = 0.33f * ExplosionPulseFactor;
                float num2 = Projectile.scale;

                Color color = ThemeColor;

                Particle p1 = new CustomPulse(Projectile.Center, Vector2.Zero, color, 
                    "CalamityMod/Particles/BloomCircle", Vector2.One * 0.33f, 
                    Main.rand.NextFloat(-10f, 10f), startSize, endSize, 30); 
                GeneralParticleHandler.SpawnParticle(p1);

                Particle p2 = new CustomPulse(Projectile.Center, Vector2.Zero, color, 
                    "CalamityMod/Particles/BloomRing", Vector2.One * 0.33f, 
                    Main.rand.NextFloat(-10f, 10f), startSize, endSize, 30); 
                GeneralParticleHandler.SpawnParticle(p2);

                Particle p3 = new CustomPulse(Projectile.Center, Vector2.Zero, color, 
                    "CalamityMod/Particles/PlasmaExplosion", Vector2.One * 0.33f, 
                    Main.rand.NextFloat(-10f, 10f), startSize, endSize, 30); 
                GeneralParticleHandler.SpawnParticle(p3);
            }


            // 插件死亡逻辑
            CurrentEffect.OnKill(Projectile, owner, timeLeft);
        }
        // 光芒大小控制
        public float GlowScaleFactor = 1f;

        // 光芒亮度控制
        public float GlowIntensityFactor = 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];

            // 插件绘制
            CurrentEffect.PreDraw(Projectile, owner, Main.spriteBatch);

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Color drawColor = ThemeColor;



            // 光芒绘制（可控开关）
            if (GlowScaleFactor > 0f && GlowIntensityFactor > 0f)
            {
                Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

                // 主题色，否则白色
                Color glowColor = ThemeColor == default ? Color.White : ThemeColor;

                // 亮度控制
                Color finalGlow = glowColor * GlowIntensityFactor;

                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Main.EntitySpriteDraw(
                    bloomTex,
                    drawPos,
                    null,
                    finalGlow,
                    Projectile.rotation,
                    bloomTex.Size() / 2f,
                    Projectile.scale * 0.9f * GlowScaleFactor, // 大小控制
                    SpriteEffects.None
                );

                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }



            Main.spriteBatch.Draw(tex, drawPos, frame, drawColor, Projectile.rotation, frame.Size() / 2, Projectile.scale, SpriteEffects.None, 0f);

            // 插件后绘制
            CurrentEffect.PostDraw(Projectile, owner, Main.spriteBatch);

            return false;
        }

        public float WidthFunc(float t, Vector2 v)
        {
            float maxBodyWidth = Projectile.scale * 40f;
            float curveRatio = 0.15f;

            if (t < curveRatio)
                return MathF.Sin(t / curveRatio * MathHelper.PiOver2) * maxBodyWidth;

            return Utils.Remap(t, curveRatio, 1f, maxBodyWidth, 0f);
        }

        public Color ColorFunc(float t, Vector2 v)
        {
            Color bodyColor = Color.Lerp(Color.Transparent, StartColor, Utils.GetLerpValue(0f, 0.35f, t, true));
            return Color.Lerp(bodyColor, EndColor, Utils.GetLerpValue(0.35f, 1f, t, true));
        }
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(WidthFunc, ColorFunc, (_, _) => Projectile.Size * 0.5f, true, true,
                GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                Projectile.oldPos.Length * 2
            );
        }
    }
}