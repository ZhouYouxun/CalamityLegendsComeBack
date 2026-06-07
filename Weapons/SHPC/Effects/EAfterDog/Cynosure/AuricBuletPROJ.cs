using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure
{
    /// <summary>
    /// Cynosure 鐨勪富绌跨敳寮广€傚畠涓嶄娇鐢ㄦ櫘閫?SHPC 鍏夌悆鐨勮〃鐜帮紝鑰屾槸鐩存帴鎵挎媴鍛戒腑鍜屽睍寮€鏁村鐏姏鐨勮亴璐ｃ€?    /// </summary>
    public class CynosureArmorPiercingRound : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Cynosure/AuricCell";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private bool PayloadReleased
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 28;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 100;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.65f, 1f) * 0.72f);

            // Flight trail axes.
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 2; i++)
            {
                float wave = MathF.Sin((Projectile.timeLeft + i * 6f) * 0.34f) * 6f;
                Dust blue = Dust.NewDustPerfect(Projectile.Center + normal * wave, DustID.Electric, -forward * 1.4f, 0, Color.Cyan, 0.95f);
                blue.noGravity = true;
                Dust gold = Dust.NewDustPerfect(Projectile.Center - normal * wave, DustID.GoldFlame, -forward * 1.1f, 0, Color.Gold, 0.85f);
                gold.noGravity = true;
            }

            // Small visual sparks orbit the projectile without adding hitboxes.
            for (int i = 0; i < 3; i++)
            {
                float angle = Main.GlobalTimeWrappedHourly * (17f + i * 2f) + i * MathHelper.TwoPi / 3f;
                Vector2 offset = new(MathF.Cos(angle) * 19f, MathF.Sin(angle) * 7f);
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                    Projectile.Center + offset.RotatedBy(Projectile.rotation),
                    -forward * 1.3f,
                    Color.Cyan,
                    Color.White,
                    0.45f,
                    5,
                    0f,
                    0.8f));
            }

            if (Main.rand.NextBool(1))
            {
                Vector2 smearOffset = normal * Main.rand.NextFloat(-7f, 7f);
                Color smearColor = Main.rand.NextBool() ? new Color(255, 218, 86) : new Color(74, 208, 255);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + smearOffset,
                    -forward * Main.rand.NextFloat(2.2f, 4.6f),
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.07f, 0.13f),
                    smearColor,
                    new Vector2(1.4f, 0.55f),
                    true,
                    true));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
            if (!PayloadReleased)
                ReleasePayload(target.whoAmI);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!PayloadReleased)
                ReleasePayload(-1);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!PayloadReleased)
                ReleasePayload(-1);
        }

        private void ReleasePayload(int preferredTarget)
        {
            PayloadReleased = true;
            if (Projectile.owner != Main.myPlayer)
                return;

            // Heavy auric impact tone.
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/AuricBulletHit") { Volume = 0.9f, Pitch = -0.12f }, Projectile.Center);

            SpawnImpactSparks();

            int burstDamage = Math.Max(1, (int)(Projectile.damage * 0.72f));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CynosureLightningExplosion>(), burstDamage, Projectile.knockBack, Projectile.owner);

            // Two auric ellipse payload rings.
            SpawnEllipse(preferredTarget, ellipseGroup: 0, count: 12);
            SpawnEllipse(preferredTarget, ellipseGroup: 1, count: 18);

            // Charged outer cells.
            const int chargedCount = 12;
            for (int i = 0; i < chargedCount; i++)
            {
                float angle = MathHelper.TwoPi * i / chargedCount;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureChargedCell>(), Math.Max(1, (int)(Projectile.damage * 0.18f)),
                    Projectile.knockBack, Projectile.owner, preferredTarget, angle);
            }
        }

        private void SpawnEllipse(int preferredTarget, int ellipseGroup, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureAuricBall>(), Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    Projectile.knockBack, Projectile.owner, preferredTarget, ellipseGroup, angle);
            }
        }

        private void SpawnImpactSparks()
        {
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(Color.Cyan, Color.White, 0.35f),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.045f,
                0.42f,
                18));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 214, 84),
                "CalamityMod/Particles/PlasmaExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.035f,
                0.28f,
                16));

            // 缁孩鎭堕瓟寮忕伀鑺憋細鏁伴噺鍥哄畾锛屼絾姣忔潯鐏姳鐨勯琛岃窛绂诲湪 0.5 鍒?2 鍊嶄箣闂村彉鍖栥€?            for (int i = 0; i < 28; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 20f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.04f, 0.09f),
                    Main.rand.NextBool(4) ? Color.White : Color.Cyan,
                    new Vector2(2.4f, 0.55f),
                    true));
            }

            for (int i = 0; i < 36; i++)
            {
                float angle = MathHelper.TwoPi * i / 36f;
                float rose = 0.78f + 0.32f * (float)Math.Sin(angle * 5f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 velocity = direction * Main.rand.NextFloat(4f, 15f) * rose;
                Color color = Color.Lerp(new Color(255, 214, 82), Color.Cyan, i % 2 == 0 ? 0.25f : 0.65f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + direction * Main.rand.NextFloat(4f, 16f),
                    velocity,
                    false,
                    Main.rand.Next(13, 24),
                    Main.rand.NextFloat(0.035f, 0.08f),
                    color,
                    new Vector2(2.2f, 0.5f),
                    true));
            }
        }

        internal float TrailWidth(float completion, Vector2 _) => MathHelper.Lerp(18f, 0f, completion);

        internal Color TrailColor(float completion, Vector2 _)
        {
            Color color = Color.Lerp(Color.White, new Color(30, 145, 255), completion);
            return color * (1f - completion);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:OverpoweredTouhouSpearShader"]
                .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(TrailWidth, TrailColor, (_, _) => Projectile.Size * 0.5f, shader: GameShaders.Misc["CalamityMod:OverpoweredTouhouSpearShader"]),
                42);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                texture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None);
            return false;
        }
    }
}

