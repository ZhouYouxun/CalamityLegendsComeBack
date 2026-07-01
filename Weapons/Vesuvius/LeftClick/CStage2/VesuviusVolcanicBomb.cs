using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.AStage0;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.CStage2
{
    public class VesuviusVolcanicBomb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Melee/VolcanicFireball";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 9;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = Projectile.ai[1] <= 0f ? 1.18f : Projectile.ai[1];
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.45f, Pitch = -0.15f }, Projectile.Center);
                Projectile.localAI[1] = Main.rand.Next(5, 16); // initial turbulence timer
                Projectile.localAI[0] = 1f;
            }

            // Irregular tumbling rotation — volcanic bombs don't spin cleanly
            float spinDir = Math.Sign(Projectile.velocity.X != 0f ? Projectile.velocity.X : 1f);
            Projectile.rotation += Main.rand.NextFloat(0.06f, 0.15f) * spinDir;

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];
            Lighting.AddLight(Projectile.Center, 0.25f, 0.2f, 0.01f);

            VesuviusProjectileVisuals.SpawnBombTrail(Projectile, 1.05f);

            if (Projectile.wet && !Projectile.lavaWet)
                Projectile.Kill();

            if (Main.rand.NextBool(4))
            {
                Dust fire = Dust.NewDustDirect(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke);
                fire.noGravity = true;
                fire.velocity *= 0f;
            }

            // Turbulence events: cross-wind kicks at irregular intervals
            if (--Projectile.localAI[1] <= 0f)
            {
                Projectile.localAI[1] = Main.rand.Next(7, 20);
                Projectile.velocity.X += Main.rand.NextFloat(-1.1f, 1.1f);
            }

            // Heavy volcanic mass — falls hard and fast, clearly heavier than the drifting ash
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + Main.rand.NextFloat(0.14f, 0.28f), -16f, 24f);
            Projectile.velocity.X *= Main.rand.NextFloat(0.987f, 0.997f);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 oldCenter = Projectile.Center;

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    oldCenter,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusLingeringLava>(),
                    Math.Max(1, (int)(Projectile.damage * 0.42f)),
                    0f,
                    Projectile.owner,
                    82f * Projectile.scale);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.72f, Pitch = -0.18f }, oldCenter);
                VesuviusProjectileVisuals.SpawnBombDetonation(oldCenter, Projectile.scale);

                for (int i = 0; i < 32; i++)
                {
                    Dust fire = Dust.NewDustPerfect(
                        oldCenter,
                        Main.rand.NextBool(3) ? DustID.InfernoFork : DustID.CopperCoin,
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 9f),
                        90,
                        Main.rand.NextBool(3) ? VesuviusProjectileVisuals.LavaGold : VesuviusProjectileVisuals.LavaOrange,
                        Main.rand.NextFloat(0.8f, 1.7f));
                    fire.noGravity = true;
                }
            }

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 center = Projectile.Center;
                Projectile.Resize((int)(112f * Projectile.scale), (int)(112f * Projectile.scale));
                Projectile.Center = center;
                Projectile.penetrate = -1;
                Projectile.maxPenetrate = -1;
                Projectile.Damage();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 300);
        }
    }

    public class VesuviusLingeringLava : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 88;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                float size = Projectile.ai[0] <= 0f ? 80f : Projectile.ai[0];
                Projectile.Resize((int)size, (int)(size * 0.65f));
                Projectile.localAI[0] = 1f;
            }

            if (!Main.dedServ)
            {
                VesuviusProjectileVisuals.SpawnLavaPoolBubble(
                    Projectile.Center,
                    new Vector2(Projectile.width * 0.45f, Projectile.height * 0.3f),
                    Projectile.timeLeft > 28 ? 1f : Utils.GetLerpValue(0f, 28f, Projectile.timeLeft, true));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smoke = ModContent.Request<Texture2D>("CalamityMod/Particles/HighResFoggyCircleHardEdge").Value;
            float fade = Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + Projectile.identity);
            Vector2 poolScale = new(Projectile.width / (float)bloom.Width, Projectile.height / (float)bloom.Height);

            Main.EntitySpriteDraw(
                smoke,
                Projectile.Center - Main.screenPosition - Vector2.UnitY * 8f,
                null,
                Color.Lerp(Color.Black, VesuviusProjectileVisuals.RavagerSmoke, 0.55f) * 0.22f * fade * VesuviusProjectileVisuals.VisualIntensity,
                Projectile.rotation + pulse * 0.12f,
                smoke.Size() * 0.5f,
                poolScale * new Vector2(1.9f, 0.95f) * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(255, 70, 20, 0) * (0.2f + pulse * 0.05f) * fade * VesuviusProjectileVisuals.VisualIntensity,
                0f,
                bloom.Size() * 0.5f,
                poolScale * 1.55f * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(VesuviusProjectileVisuals.LavaGold.R, VesuviusProjectileVisuals.LavaGold.G, VesuviusProjectileVisuals.LavaGold.B, 0) * 0.16f * fade * VesuviusProjectileVisuals.VisualIntensity,
                0f,
                bloom.Size() * 0.5f,
                poolScale * new Vector2(0.82f, 0.42f) * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);

            return false;
        }
    }
}
