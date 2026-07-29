using System;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;
using CalamityMod.Dusts;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    // Full grey-white adaptation of Ontological Despoiler's small negative tracking shot.
    public class LeonidVoidSeeker : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/OntologicalDespoilerShot";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        private ref float Time => ref Projectile.ai[0];
        private Color BaseColor;
        private int SineDirection = 1;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 750;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            float colorPulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + Projectile.whoAmI);
            BaseColor = Color.Lerp(Color.White, new Color(190, 226, 255), colorPulse);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 10)
            {
                Projectile.frame = (Projectile.frame + 1) % 6;
                Projectile.frameCounter = 0;
            }

            if (Time == 0f)
            {
                Projectile.scale = 1f;
                SineDirection = Main.rand.NextBool() ? 1 : -1;
                Projectile.frame = Main.rand.Next(6);
            }

            if (Time > 20f)
            {
                float sine = MathF.Sin(Projectile.timeLeft * 0.575f / MathHelper.Pi);
                Vector2 offset = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * sine * 13f;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset * SineDirection,
                    Time % 2f == 0f ? ModContent.DustType<VoidDustInverted>() : ModContent.DustType<VoidDust>(),
                    -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.45f, 0.55f);
                dust.color = BaseColor;

                NPC target = FindClosestNPC(700f);
                if (target != null)
                    CalamityUtils.HomeInOnSelectedNPC(Projectile, target, true, 0.42f, 8f, 0.95f, accelerate: true);
            }

            Lighting.AddLight(Projectile.Center, BaseColor.ToVector3() * 0.58f);
            Time++;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center,
                    Main.rand.NextBool() ? ModContent.DustType<VoidDustInverted>() : ModContent.DustType<VoidDust>(),
                    (Projectile.velocity * 3f).RotatedByRandom(0.7f) * Main.rand.NextFloat(0.2f, 1f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.15f, 1.45f);
                dust.color = BaseColor;
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.White,
                "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.15f, 0.65f, 8));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Black,
                "CalamityMod/Particles/SmallBloomRingLayered", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.2f, 0.75f, 8, false));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Black,
                "CalamityMod/Particles/SmallBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.4f, 0.05f, 9, false));
            SoundEngine.PlaySound(OntologicalDespoiler.SmallImpact with { Volume = 1f, Pitch = Main.rand.NextFloat(0.15f, 0.45f), MaxInstances = 1 }, Projectile.Center);
        }

        private NPC FindClosestNPC(float range)
        {
            NPC closest = null;
            float closestDistanceSquared = range * range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closest = npc;
                }
            }
            return closest;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, 6, 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D bloomTrail = ModContent.Request<Texture2D>("CalamityMod/Particles/DrainLineBloom2").Value;
            Texture2D darkTrail = ModContent.Request<Texture2D>("CalamityMod/Particles/DrainLine2").Value;
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], BaseColor with { A = 0 } * 0.8f, 1, bloomTrail);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.Lerp(Color.Black, BaseColor, 0.18f) with { A = 0 }, 1, darkTrail, true, true);

            Main.EntitySpriteDraw(texture, drawPosition, frame, BaseColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            LeonidVisualUtils.DrawBloom(Projectile.Center, BaseColor * 0.4f, 0.1f);
            LeonidVisualUtils.DrawCelestialHead(Projectile.Center, BaseColor, 0.65f, 0.72f, Projectile.rotation);
            LeonidStarlight.DrawFlare(Projectile.Center, Color.White, 0.5f, 0.075f, -Projectile.rotation * 0.4f);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();
            return false;
        }
    }
}
