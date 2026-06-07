using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentSolar_Spear : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";

        private const int DaybreakExplosionProjectileID = 953;

        private int hitCount;
        private int visualTimer;
        private Vector2 flameBeam;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 320;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.extraUpdates = 5;
        }

        public override void OnSpawn(IEntitySource source)
        {
            visualTimer = 0;
            flameBeam = Projectile.Center;
        }

        public override void AI()
        {
            if (Projectile.numUpdates == 0)
                visualTimer++;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            EmitSolarSpearFlightFlames();
        }

        private void EmitSolarSpearFlightFlames()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float age = Utils.GetLerpValue(0f, 28f, visualTimer, true);
            float phase = visualTimer * 0.54f / MathHelper.Pi + Projectile.identity * 0.37f;
            float sine = (float)Math.Sin(phase);

            flameBeam = Projectile.Center + side * sine * 45f * age - forward * 8f;

            Lighting.AddLight(Projectile.Center, new Vector3(1.25f, 0.72f, 0.18f) * 0.72f);

            if (Main.dedServ)
                return;

            Color beamColor = Color.Lerp(new Color(255, 78, 20), new Color(255, 210, 92), age);
            Particle beam = new CustomSpark(
                flameBeam + Main.rand.NextVector2Circular(2f, 2f),
                Projectile.velocity * 0.06f + back * Main.rand.NextFloat(0.45f, 1.4f),
                "CalamityMod/Particles/SmallBloom",
                false,
                Main.rand.Next(6, 9),
                (Main.rand.NextFloat(0.045f, 0.075f) + 0.035f * age) * 1.5f,
                beamColor,
                new Vector2(1f, 2.25f) * 1.5f,
                true,
                false);
            GeneralParticleHandler.SpawnParticle(beam);

            if (Projectile.numUpdates == 0 && visualTimer % 5 == 0)
            {
                Particle ember = new SparkParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(6f, 14f) + Main.rand.NextVector2Circular(3f, 3f),
                    back.RotatedByRandom(0.24f) * Main.rand.NextFloat(1.4f, 3.8f),
                    true,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Main.rand.NextBool(3) ? Color.Goldenrod : Color.OrangeRed);
                GeneralParticleHandler.SpawnParticle(ember);
            }

            if (Projectile.numUpdates == 0 && visualTimer % 4 == 0)
            {
                Vector2 flareDir = back.RotatedByRandom(0.28f);
                Particle solarFlare = new CustomSpark(
                    Projectile.Center - forward * Main.rand.NextFloat(8f, 18f) + side * Main.rand.NextFloat(-8f, 8f),
                    flareDir * Main.rand.NextFloat(2.4f, 5.2f),
                    "CalamityMod/Particles/ProvidenceMarkParticle",
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.42f, 0.62f),
                    Color.Lerp(new Color(255, 235, 150), new Color(255, 92, 20), Main.rand.NextFloat(0.35f, 0.9f)),
                    new Vector2(Main.rand.NextFloat(0.95f, 1.25f), Main.rand.NextFloat(0.22f, 0.36f)),
                    true,
                    false,
                    Main.rand.NextFloat(-0.12f, 0.12f),
                    false,
                    false,
                    0.08f);
                GeneralParticleHandler.SpawnParticle(solarFlare);
            }

            if (Main.rand.NextBool(4))
            {
                Vector2 smokePos = Projectile.Center - forward * Main.rand.NextFloat(4f, 12f) + side * Main.rand.NextFloat(-12f, 12f);
                Vector2 smokeVel = back.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.45f, 1.5f);
                Particle smoke = new MediumMistParticle(
                    smokePos,
                    smokeVel,
                    Main.rand.NextBool() ? new Color(255, 115, 32) : Color.DarkOrange,
                    Color.Black,
                    Main.rand.NextFloat(0.38f, 0.76f),
                    Main.rand.NextFloat(90f, 135f),
                    0.08f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextBool(5))
            {
                Dust cinder = Dust.NewDustPerfect(
                    Projectile.Center + side * Main.rand.NextFloat(-10f, 10f) - forward * Main.rand.NextFloat(2f, 14f),
                    DustID.Torch,
                    back.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.1f, 3.5f),
                    0,
                    Color.Lerp(Color.OrangeRed, Color.White, Main.rand.NextFloat(0.15f, 0.45f)),
                    Main.rand.NextFloat(0.95f, 1.45f));
                cinder.noGravity = true;
                cinder.fadeIn = 1.15f;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitCount++;
            SpawnDaybreakExplosion(target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = 0.08f }, Projectile.Center);
            SpawnSolarExplosion();
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        private void SpawnSolarExplosion()
        {
            Vector2 center = Projectile.Center;
            Lighting.AddLight(center, new Vector3(1.8f, 1.05f, 0.28f) * 1.1f);
        }

        private void SpawnDaybreakExplosion(Vector2 center)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int explosionIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                DaybreakExplosionProjectileID,
                Math.Max(1, (int)(Projectile.damage * 0.75f)),
                Projectile.knockBack * 0.35f,
                Projectile.owner);

            if (!Main.projectile.IndexInRange(explosionIndex))
                return;

            Projectile explosion = Main.projectile[explosionIndex];
            explosion.Center = center;
            explosion.friendly = true;
            explosion.hostile = false;
            explosion.DamageType = DamageClass.Magic;
            explosion.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (visualTimer < 5)
                return false;

            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D mist = ModContent.Request<Texture2D>("CalamityMod/Particles/MediumMist").Value;

            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float drawRotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            float opacity = Utils.GetLerpValue(0f, 10f, visualTimer, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            float flameAge = Utils.GetLerpValue(0f, 28f, visualTimer, true);
            Color hotCore = Color.Lerp(new Color(255, 248, 190), new Color(255, 174, 56), 0.45f);
            Color orange = new Color(255, 112, 26);
            Color darkEdge = new Color(170, 28, 8);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Math.Max(1, Projectile.oldPos.Length - 1);
                float trailFade = (1f - completion) * opacity;
                if (trailFade <= 0.03f)
                    continue;

                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float wave = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8.5f + Projectile.identity * 0.31f + i * 0.64f);
                float radius = MathHelper.Lerp(42f, 9f, completion) * flameAge;
                float mistScale = MathHelper.Lerp(0.42f, 0.10f, completion) * Projectile.scale * 1.5f;
                Color trailColor = Color.Lerp(darkEdge, Color.Lerp(orange, hotCore, 0.45f), 1f - completion);
                trailColor.A = 0;

                Vector2 helixPos = trailPos + side * wave * radius - forward * MathHelper.Lerp(2f, 16f, completion);
                float helixRot = forward.ToRotation() + MathHelper.PiOver2 + wave * 0.18f;

                Main.EntitySpriteDraw(
                    mist,
                    helixPos,
                    null,
                    trailColor * trailFade * 0.45f,
                    helixRot,
                    mist.Size() * 0.5f,
                    new Vector2(mistScale * 0.55f, mistScale * 1.45f),
                    SpriteEffects.None,
                    0);

                Main.EntitySpriteDraw(
                    bloom,
                    helixPos,
                    null,
                    Color.Lerp(orange, hotCore, 0.35f) * trailFade * 0.12f,
                    0f,
                    bloom.Size() * 0.5f,
                    mistScale * 0.28f,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPos + forward * 10f,
                null,
                hotCore * opacity * 0.38f,
                0f,
                bloom.Size() * 0.5f,
                0.36f * Projectile.scale,
                SpriteEffects.None,
                0);

            Color singleBeamColor = hotCore;
            singleBeamColor.A = 0;
            Main.EntitySpriteDraw(
                bloom,
                flameBeam - Main.screenPosition,
                null,
                singleBeamColor * opacity * 0.24f,
                0f,
                bloom.Size() * 0.5f,
                0.3f * Projectile.scale,
                SpriteEffects.None,
                0);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Projectile.oldPos.Length; i += 2)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float fade = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color c = new Color(255, 128, 36) * 0.28f * fade * opacity;
                c.A = 0;

                sb.Draw(tex, pos, null, c, drawRotation, origin, Projectile.scale * (0.52f + fade * 0.34f), SpriteEffects.None, 0f);
            }

            sb.Draw(tex, drawPos, null, Color.White, drawRotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
