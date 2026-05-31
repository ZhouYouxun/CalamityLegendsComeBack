using CalamityMod.Buffs.DamageOverTime;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal sealed class StarblightSootShard : ModProjectile, ILocalizedModType
    {
        private const string GlowBladeTexture = "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade";

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private int starFrame;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 33;
            Projectile.height = 33;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 140;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(IEntitySource source)
        {
            starFrame = Utils.Clamp((int)Projectile.ai[1], 0, 4);
            Projectile.scale = Main.rand.NextFloat(0.72f, 1.05f) * 1.5f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.24f, Pitch = 0.2f, PitchVariance = 0.12f, MaxInstances = 6 }, Projectile.Center);
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity *= 0.99f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + Timer * 0.03f;
            Lighting.AddLight(Projectile.Center, new Color(255, 120, 76).ToVector3() * 0.38f);

            if (Main.dedServ)
                return;

            SpawnOrbitSparks();

            if (Main.rand.NextBool(2))
            {
                Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + backward * Main.rand.NextFloat(3f, 12f),
                    Utils.SelectRandom(Main.rand, ModContent.DustType<AstralOrange>(), ModContent.DustType<AstralBlue>()),
                    backward.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.6f, 2.2f),
                    0,
                    default,
                    Main.rand.NextFloat(0.82f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int explosionIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<NewLegendSHPE>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner);

                if (Main.projectile.IndexInRange(explosionIndex))
                {
                    Projectile explosion = Main.projectile[explosionIndex];
                    explosion.width = 50;
                    explosion.height = 50;
                    explosion.Center = Projectile.Center;
                    explosion.netUpdate = true;
                }
            }

            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.28f, Pitch = 0.18f, PitchVariance = 0.1f, MaxInstances = 6 }, Projectile.Center);

            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Utils.SelectRandom(Main.rand, ModContent.DustType<AstralOrange>(), ModContent.DustType<AstralBlue>()),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 7.6f),
                    0,
                    default,
                    Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 136, 72),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-0.2f, 0.2f),
                0.05f,
                0.52f,
                16));
        }

        private void SpawnOrbitSparks()
        {
            float spinPhase = Main.GlobalTimeWrappedHourly * 16f + Projectile.identity * 0.37f;
            float orbitRadius = 9f;
            Color sparkColor = Color.Lerp(new Color(255, 130, 68), new Color(92, 205, 255), 0.35f) * 0.82f;

            for (int i = 0; i < 4; i++)
            {
                float angle = spinPhase + MathHelper.PiOver2 * i;
                Vector2 radialDirection = angle.ToRotationVector2();
                Vector2 sparkCenter = Projectile.Center + radialDirection * orbitRadius;
                Vector2 finalVelocity = radialDirection.RotatedBy(MathHelper.PiOver2) * 2.05f + Projectile.velocity * 0.04f;
                float extraRot = radialDirection.ToRotation() - finalVelocity.ToRotation();

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    sparkCenter,
                    finalVelocity,
                    GlowBladeTexture,
                    false,
                    2,
                    0.08f,
                    sparkColor,
                    new Vector2(0.28f, 0.58f),
                    glowCenter: true,
                    shrinkSpeed: 1.2f,
                    glowCenterScale: 0.62f,
                    glowOpacity: 0.56f,
                    extraRotation: extraRot));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Particles/FancyStars").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Rectangle frame = texture.Frame(horizontalFrames: 5, verticalFrames: 1, frameX: starFrame);
            Vector2 origin = frame.Size() * 0.5f;
            Color mainColor = new(255, 136, 72, 0);
            Color blueColor = new(92, 210, 255, 0);
            Vector2 center = Projectile.Center - Main.screenPosition;

            // FancyStars 与 BloomCircle 都是供加法混合使用的贴图。
            // 普通 AlphaBlend 会把贴图内的黑色底也绘制出来。
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldPosition = Projectile.oldPos[i];
                if (oldPosition == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = oldPosition + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(mainColor, blueColor, 0.35f + completion * 0.35f) * (completion * 0.42f);
                Main.EntitySpriteDraw(texture, drawPosition, frame, trailColor, Projectile.rotation, origin, Projectile.scale * completion * 0.74f, SpriteEffects.None);
            }

            float pulse = 1f + (float)Math.Sin(Timer * 0.22f) * 0.08f;
            Main.EntitySpriteDraw(bloom, center, null, mainColor * 0.36f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.22f * pulse, SpriteEffects.None);

            // 飞行期间持续重绘三枚旋转星芒，让碎片本体带有独立的星辉包裹效果。
            for (int i = 0; i < 3; i++)
            {
                float angle = Timer * (0.12f + i * 0.035f) + MathHelper.TwoPi / 3f * i;
                Vector2 orbitOffset = angle.ToRotationVector2() * Projectile.scale * (4.2f + i * 1.15f);
                float orbitScale = Projectile.scale * (0.3f - i * 0.045f) * pulse;
                Color orbitColor = Color.Lerp(mainColor, blueColor, i * 0.34f) * (0.38f - i * 0.06f);
                Main.EntitySpriteDraw(texture, center + orbitOffset, frame, orbitColor, -angle, origin, orbitScale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, center, frame, mainColor * 0.86f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, center, frame, Color.White * 0.28f, -Projectile.rotation * 0.7f, origin, Projectile.scale * 0.58f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
