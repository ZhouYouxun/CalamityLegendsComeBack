using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Passive
{
    public class SHPCPassiveOrb : ModProjectile, ILocalizedModType
    {
        private static readonly Color TechBlue = new(38, 170, 255);
        private static readonly Color TechBlueBright = new(160, 245, 255);
        private static readonly Color TechBlueDeep = new(28, 88, 200);
        private const float ManaRestoreRatio = 0.07f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float time => ref Projectile.ai[0];
        public bool onSpawn = true;
        private Player targeted;
        public bool startAttackEffects = true;
        public int attackTime = 160;
        private float previousFlightState = -1f;

        private ref float FlightState => ref Projectile.localAI[0];
        private ref float FlightTimer => ref Projectile.localAI[1];

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 5;
            Projectile.timeLeft = 540;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, TechBlue.ToVector3() * 0.5f);
            Player owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(owner.Center, Projectile.Center);

            if (onSpawn)
            {
                SpawnSpawnEffects();
                onSpawn = false;
            }

            if (time >= attackTime)
            {
                if (targeted == null)
                {
                    startAttackEffects = true;
                    targeted = owner;
                }

                Projectile.extraUpdates = 5;
                UpdateReturnFlight(targeted);

                if (startAttackEffects)
                {
                    SoundStyle pulse = new("CalamityMod/Sounds/Item/PulseSound");
                    SoundEngine.PlaySound(pulse with
                    {
                        Volume = 0.25f,
                        Pitch = Math.Max(0.6f, Main.rand.NextFloat(0.3f, 0.4f) + Projectile.numHits * 0.1f),
                        MaxInstances = 5
                    }, Projectile.Center);

                    startAttackEffects = false;
                }
            }
            else
            {
                Projectile.velocity *= Projectile.numHits > 0 ? 0.955f : 0.97f;
            }

            if (previousFlightState != FlightState)
            {
                SpawnFlightStateSwapEffects(owner, previousFlightState, FlightState);
                previousFlightState = FlightState;
            }

            float squash = Utils.GetLerpValue(1, 3, Projectile.velocity.Length(), true);
            SpawnFlightTrail(owner, targetDist, squash);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            time++;
        }

        private void UpdateReturnFlight(Player owner)
        {
            FlightTimer++;

            FlightState = 2f;
            Vector2 returnVelocity = (owner.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 18f;
            Projectile.velocity = returnVelocity;

            if (Vector2.Distance(Projectile.Center, owner.Center) < 20f)
            {
                RestoreOwnerMana(owner);
                Projectile.Kill();
            }
        }

        private static void RestoreOwnerMana(Player owner)
        {
            if (!owner.active || owner.dead || owner.statMana >= owner.statManaMax2)
                return;

            int previousMana = owner.statMana;
            int manaToRestore = Math.Max(1, (int)MathF.Ceiling(owner.statManaMax2 * ManaRestoreRatio));
            owner.statMana = Utils.Clamp(owner.statMana + manaToRestore, 0, owner.statManaMax2);

            int restored = owner.statMana - previousMana;
            if (restored > 0 && owner.whoAmI == Main.myPlayer)
                owner.ManaEffect(restored);
        }

        private void SpawnSpawnEffects()
        {
            SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with
            {
                Volume = 1.25f,
                Pitch = 0.25f
            }, Projectile.Center);

            Particle coreBurst = new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(TechBlue, TechBlueBright, 0.45f),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-0.15f, 0.15f),
                0.42f,
                0.05f,
                18,
                true);
            GeneralParticleHandler.SpawnParticle(coreBurst);
        }

        private void SpawnFlightStateSwapEffects(Player owner, float oldState, float newState)
        {
            if (oldState < 0f)
                return;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/PulseSound")
            {
                Volume = 0.18f,
                Pitch = 0.55f
            }, Projectile.Center);
        }

        private void SpawnFlightTrail(Player owner, float targetDist, float squash)
        {
            if (targetDist >= 1400f || squash <= 0.2f || time <= 5f)
                return;

            float stretchBoost = Utils.GetLerpValue(10f, 16f, Projectile.velocity.Length(), true);
            Color trailColor = Color.Lerp(TechBlueDeep, TechBlueBright, 0.58f) * 0.62f * squash;

            Particle trail = new CustomSpark(
                Projectile.Center,
                Projectile.velocity * 0.01f,
                "CalamityMod/Particles/DualTrail",
                false,
                13,
                0.075f * Projectile.scale,
                trailColor,
                new Vector2(1f - 0.15f * squash, 1.3f + stretchBoost * 1.5f),
                true,
                false,
                shrinkSpeed: 0.2f * squash);
            GeneralParticleHandler.SpawnParticle(trail);

            if (FlightState == 1f && Main.rand.NextBool(3))
            {
                Vector2 orbitDirectionNow = (Projectile.Center - owner.Center).SafeNormalize(Vector2.UnitY);
                //Particle orbitSpark = new CustomSpark(
                //    Projectile.Center + orbitDirectionNow * Main.rand.NextFloat(4f, 10f),
                //    Projectile.velocity * 0.015f + orbitDirectionNow.RotatedBy(MathHelper.PiOver2 * orbitDirection) * Main.rand.NextFloat(0.8f, 1.4f),
                //    "CalamityLegendsComeBack/Texture/KsTexture/window_04",
                //    false,
                //    9,
                //    0.08f * Projectile.scale,
                //    Color.Lerp(TechBlue, Color.White, 0.35f) * 0.68f,
                //    new Vector2(0.55f, 1.45f),
                //    glowCenter: true,
                //    shrinkSpeed: 0.8f,
                //    glowCenterScale: 0.85f,
                //    glowOpacity: 0.6f);
                //GeneralParticleHandler.SpawnParticle(orbitSpark);
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with
            {
                Volume = 0.45f,
                Pitch = 0.35f
            }, Projectile.Center);

            Particle vanishPulse = new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(TechBlue, Color.White, 0.28f),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-0.08f, 0.08f),
                0.34f,
                0.04f,
                16,
                true);
            GeneralParticleHandler.SpawnParticle(vanishPulse);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> orb = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Vector2 squash = new Vector2(Utils.Remap(Projectile.velocity.Length(), 5, 10, 1, 0.6f), Utils.Remap(Projectile.velocity.Length(), 5, 10, 1, 2f));
            float timeleftFade = (float)Math.Pow(Utils.GetLerpValue(0, 40 * Projectile.extraUpdates, Projectile.timeLeft, true), 5);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + Projectile.identity * 0.31f);

            for (int i = 0; i < 7; i++)
            {
                Color orbColor = Color.Lerp(TechBlue, Color.White, i * 0.08f) with { A = 0 } * (0.42f + pulse * 0.08f);
                Vector2 scale = Projectile.scale * timeleftFade * squash * (0.045f + i * 0.012f) * (3f + pulse * 0.25f);
                Main.EntitySpriteDraw(orb.Value, Projectile.Center - Main.screenPosition, null, orbColor, Projectile.rotation, orb.Size() * 0.5f, scale, SpriteEffects.None);
            }

            //Asset<Texture2D> line = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/window_04");
            //Color coreColor = Color.Lerp(TechBlueBright, Color.White, 0.25f) * 0.7f * timeleftFade;
            //Vector2 coreScale = new Vector2(0.2f, 0.42f + pulse * 0.08f) * Projectile.scale;
            //Main.EntitySpriteDraw(line.Value, Projectile.Center - Main.screenPosition, null, coreColor, Projectile.rotation, line.Size() * 0.5f, coreScale, SpriteEffects.None);

            return false;
        }
    }
}
