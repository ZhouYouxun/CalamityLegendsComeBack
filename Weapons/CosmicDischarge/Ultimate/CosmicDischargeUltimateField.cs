using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeUltimateField : ModProjectile, ILocalizedModType
    {
        private const float GlacialAgeRadius = 25f * 16f;
        private const float FieldRadius = 8f * 16f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];
        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = (int)(FieldRadius * 2f);
            Projectile.height = (int)(FieldRadius * 2f);
            Projectile.penetrate = -1;
            Projectile.timeLeft = CosmicDischargePlayer.UltimateFieldDuration;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Time++;
            Projectile.Center = Owner.Center;
            Projectile.Opacity = Utils.GetLerpValue(0f, 18f, Time, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Owner.AddBuff(ModContent.BuffType<CosmicDischargeUltimateGuardBuff>(), 4);

            if (Time == 1f)
                ApplyGlacialAge();

            SlowEnemiesInField();
            SpawnFieldDust();
        }

        private void ApplyGlacialAge()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage || Vector2.DistanceSquared(npc.Center, Projectile.Center) > GlacialAgeRadius * GlacialAgeRadius)
                    continue;

                CosmicDischargeCommon.ApplyDoGDebuffs(npc, 10 * 60);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.82f, Pitch = -0.05f }, Projectile.Center);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerSpawn") { Volume = 0.46f, Pitch = 0.18f }, Projectile.Center);
                GeneralParticleHandler.SpawnParticle(new PulseRing(
                    Projectile.Center,
                    Vector2.Zero,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.65f,
                    0.08f,
                    GlacialAgeRadius / 90f,
                    26));
                GeneralParticleHandler.SpawnParticle(new StrongBloom(
                    Projectile.Center,
                    Vector2.Zero,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.7f,
                    1.25f,
                    28));
                CosmicDischargeCommon.SpawnDoGSparkBurst(Projectile.Center, 40, 4f, 16f, 0.82f);
                CosmicDischargeCommon.SpawnDoGRiftCracks(Projectile.Center, 25, 8f, 18f, 0.75f);
            }
        }

        private void SlowEnemiesInField()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage || Vector2.DistanceSquared(npc.Center, Projectile.Center) > FieldRadius * FieldRadius)
                    continue;

                npc.velocity *= npc.boss ? 0.93f : 0.76f;
                CosmicDischargeCommon.ApplyDoGDebuffs(npc, 90);
            }
        }

        private void SpawnFieldDust()
        {
            if (Time % 90f == 30f && Main.myPlayer == Projectile.owner)
                SpawnLaserWallCycle();

            if (Main.dedServ || Main.rand.NextBool(2))
                return;

            Vector2 rim = Projectile.Center + Main.rand.NextVector2CircularEdge(FieldRadius, FieldRadius);
            Vector2 velocity = (Projectile.Center - rim).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.25f, 1.1f);
            Dust dust = Dust.NewDustPerfect(
                rim,
                DustID.PurpleTorch,
                velocity,
                120,
                CosmicDischargeCommon.RandomDoGColor(),
                Main.rand.NextFloat(0.85f, 1.25f));
            dust.noGravity = true;

        }

        private void SpawnLaserWallCycle()
        {
            int beams = Main.rand.Next(2, 5);
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < beams; i++)
            {
                Vector2 direction = (baseAngle + MathHelper.TwoPi * i / beams).ToRotationVector2();
                int beam = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    direction,
                    ModContent.ProjectileType<FriendlyLaserWallBeam>(),
                    (int)(Projectile.damage * 0.38f),
                    0f,
                    Projectile.owner,
                    1f,
                    0f,
                    4f);
                if (Main.projectile.IndexInRange(beam))
                    Main.projectile[beam].scale = 1.1f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Vector2 portalOrigin = portal.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 1f + 0.035f * MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi);
            float portalRotation = Main.GlobalTimeWrappedHourly * 5.5f + Projectile.identity * 0.18f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.15f * Projectile.Opacity,
                0f,
                origin,
                FieldRadius / bloom.Width * 2f * pulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(portal, drawPosition, null, Color.Black * 0.28f * Projectile.Opacity, portalRotation, portalOrigin, 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * 0.34f * Projectile.Opacity, portalRotation * 0.6f, portalOrigin, 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.34f * Projectile.Opacity, -portalRotation * 0.7f, portalOrigin, 0.42f * pulse, SpriteEffects.None);

            for (int i = 0; i < 42; i++)
            {
                float angle = MathHelper.TwoPi * i / 42f + Main.GlobalTimeWrappedHourly * 0.35f;
                Vector2 point = drawPosition + angle.ToRotationVector2() * FieldRadius * pulse;
                Main.EntitySpriteDraw(
                    pixel,
                    point,
                    new Rectangle(0, 0, 1, 1),
                    CosmicDischargeCommon.Transparent(Color.Lerp(CosmicDischargeCommon.DoGCyanColor, CosmicDischargeCommon.DoGFuchsiaColor, (float)i / 41f)) * 0.48f * Projectile.Opacity,
                    angle,
                    new Vector2(0.5f),
                    new Vector2(3f, 3f),
                    SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }
    }
}
