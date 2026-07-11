using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Straight-flying companion projectile spawned with torpedoes in stages 3-4.
    // Unlike AcidRocket it doesn't home; it leaves a toxic cloud on death.
    internal sealed class SeasSearingPollutionRocket : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/AcidRocket";

        private static readonly int TrailLength = 18;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 12;
            Projectile.height         = 12;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = 2;
            Projectile.timeLeft       = 320;
            Projectile.tileCollide    = true;
            Projectile.ignoreWater    = true;
            Projectile.ArmorPenetration = 14;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 12;
        }

        public override void AI()
        {
            Projectile.rotation  = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity += Vector2.UnitY * 0.04f;

            Lighting.AddLight(Projectile.Center, SeasSearingPalette.ToxicGreen.ToVector3() * 0.44f);

            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
            {
                Dust trail = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.5f,
                    DustID.GemEmerald,
                    Projectile.velocity * -Main.rand.NextFloat(0.18f, 0.44f),
                    115, Color.Lerp(SeasSearingPalette.ToxicGreen, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.45f, 0.85f));
                trail.noGravity = true;
            }

            if (!Main.dedServ && Main.GameUpdateCount % 12 == 0)
                SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 1.05f, 8f, 8, SeasSearingPalette.ToxicGreen);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;

            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color color = Color.Lerp(SeasSearingPalette.ToxicGreen, SeasSearingPalette.RadioactiveCyan, completion) * (completion * completion * 0.58f);
                color.A = 0;

                Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.72f, 0f,
                    bloomOrigin, 0.05f * completion, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.oldRot[i],
                    origin, Projectile.scale * MathHelper.Lerp(0.35f, 0.9f, completion), SpriteEffects.None, 0);
            }

            Color main = Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, 0.62f);
            main.A = 0;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, main * 0.62f, 0f,
                bloomOrigin, 0.085f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, main, Projectile.rotation,
                origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SeasSearingPollutionNPC pollution = target.GetGlobalNPC<SeasSearingPollutionNPC>();
            pollution.ApplyPollution(target, Projectile.owner, 5, 12 * 60);
            target.AddBuff(BuffID.Venom, 240);

            if (Main.myPlayer == Projectile.owner)
                SeasSearingVisualUtility.SpawnAbyssDust(target.Center, 8, 2.2f, 16f);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.28f, PitchVariance = 0.1f }, Projectile.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 16, 3.2f, 24f, 1.1f);
            SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 2.5f, 18f, 14, SeasSearingPalette.ToxicGreen);

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingFalloutCloud>(),
                    Math.Max(1, Projectile.damage / 2), 0f, Projectile.owner, 8f);
            }
        }
    }
}
