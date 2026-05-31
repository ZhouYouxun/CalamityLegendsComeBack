using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFFakeCalamityEffect
    {
        private const int FireInterval = 28;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftTimer++;
            if (holdout.LeftTimer < FireInterval)
                return;

            holdout.LeftTimer = 0;
            Vector2 mouse = holdout.GetMouseWorld();
            NPC nearby = FindMouseTarget(mouse, 170f);
            Vector2 spawnPosition = nearby?.Center ?? mouse + Main.rand.NextVector2Circular(24f, 24f);
            Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3.2f, 5.8f);
            int projectileIndex = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<PFFakeCalamity_SoulFlame>(),
                holdout.GetScaledDamage(0.7f),
                holdout.Projectile.knockBack * 0.25f,
                holdout.Projectile.owner,
                holdout.Projectile.whoAmI,
                Main.rand.NextFloat(MathHelper.TwoPi));
            PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);

            holdout.TriggerMuzzleFlash(8);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.32f, Pitch = 0.36f }, spawnPosition);
        }

        private static NPC FindMouseTarget(Vector2 mouse, float range)
        {
            NPC closest = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(mouse, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }
    }

    internal sealed class PFFakeCalamity_SoulFlame : ModProjectile, ILocalizedModType
    {
        private ref float Timer => ref Projectile.localAI[0];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Magic/RancorFog";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 190;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Timer++;
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Timer < 28f)
                Projectile.velocity = Projectile.velocity.RotatedBy(System.MathF.Sin(Timer * 0.22f + Projectile.ai[1]) * 0.055f) * 0.985f;
            else
            {
                Vector2 desired = (owner.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 8.4f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.058f);
                if (Projectile.Distance(owner.Center) < 26f)
                {
                    Projectile.Kill();
                    return;
                }
            }

            Projectile.rotation += 0.028f;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.32f);
            EmitSoulEffects(direction);
        }

        private void EmitSoulEffects(Vector2 direction)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            Color outer = Color.Lerp(ThemeColor, Color.White, 0.2f);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                -direction * Main.rand.NextFloat(0.3f, 1f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                false,
                Main.rand.Next(12, 20),
                Main.rand.NextFloat(0.42f, 0.8f),
                outer,
                true,
                false,
                true));

            if (Main.rand.NextBool(4))
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, ThemeColor * 0.4f, Vector2.One, Projectile.rotation, 0.02f, 0.24f, 18));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityMod.CalamityUtils.CircularHitboxCollision(Projectile.Center, 24f, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 240);
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D fog = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color theme = ThemeColor with { A = 0 };
            Vector2 center = Projectile.Center - Main.screenPosition;
            float pulse = 0.86f + System.MathF.Sin(Timer * 0.12f) * 0.14f;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(fog, center, null, theme * 0.32f, Projectile.rotation, fog.Size() * 0.5f, 0.58f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(fog, center, null, Color.Lerp(theme, Color.White with { A = 0 }, 0.42f) * 0.2f, -Projectile.rotation * 0.72f, fog.Size() * 0.5f, 0.42f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, theme * 0.4f, Projectile.rotation, bloom.Size() * 0.5f, 0.18f * pulse, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
