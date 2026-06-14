using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal class BFArrow_CDetec : ModProjectile
    {
        private const int PriorityMarkDuration = 15 * 60;
        private const float MaxScanRadius = 240f;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightClick/CDetec/BFArrow_CDetec";

        private ref float BestTargetIndex => ref Projectile.ai[0];
        private ref float BestTargetLifeMax => ref Projectile.ai[1];
        private ref float FlightTimer => ref Projectile.localAI[0];
        private ref float ScanRadius => ref Projectile.localAI[1];
        private int configuredMarkDuration = PriorityMarkDuration;
        private int configuredEffectTier;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 16;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 240, penetrate: -1, extraUpdates: 6, tileCollide: true);
            Projectile.localNPCHitCooldown = 12;
        }

        public void ConfigureMark(int markDuration, int effectTier)
        {
            configuredMarkDuration = System.Math.Max(60, markDuration);
            configuredEffectTier = Utils.Clamp(effectTier, 0, 2);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(configuredMarkDuration);
            writer.Write(configuredEffectTier);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            configuredMarkDuration = reader.ReadInt32();
            configuredEffectTier = reader.ReadInt32();
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            BestTargetIndex = -1f;
            BestTargetLifeMax = -1f;
            ScanRadius = 28f;
            BFArrowCommon.FaceForward(Projectile);
        }

        public override void AI()
        {
            FlightTimer++;
            ScanRadius = MathHelper.Clamp(ScanRadius + 3.6f, 28f, MaxScanRadius);

            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec).ToVector3() * 0.52f);
            BFArrowCommon.FaceForward(Projectile);
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_CDetec, 1.18f);
            EmitPenetrationFlightFX();

            if ((int)FlightTimer % 16 == 0 && Projectile.owner == Main.myPlayer)
                SoundEngine.PlaySound(BlossomFluxSounds.RightReconProjAction, Projectile.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.lifeMax > BestTargetLifeMax)
            {
                BestTargetLifeMax = target.lifeMax;
                BestTargetIndex = target.whoAmI;
                Projectile.netUpdate = true;
            }

            Projectile.localNPCImmunity[target.whoAmI] = 24;
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_CDetec, 8, 0.7f, 2.6f, 0.72f, 1.05f);
            SpawnPenetrationImpactFX(target.Center, 1f);
            SoundEngine.PlaySound(BlossomFluxSounds.RightReconProjHit, target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_CDetec, 12, 1f, 3.8f, 0.82f, 1.18f);
            SoundEngine.PlaySound(BlossomFluxSounds.RightReconTileCollide, Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            ApplyBestTargetMark();
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_CDetec, 14, 1.1f, 4.8f, 0.88f, 1.25f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawReconOpticOverlay();
            BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_CDetec, 1.05f);
            return false;
        }

        private void DrawReconOpticOverlay()
        {
            if (Main.dedServ)
                return;

            Texture2D opticTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/muzzle_02").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float fadeIn = Utils.GetLerpValue(0f, 18f, FlightTimer, true);
            float fadeOut = Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            float fade = fadeIn * fadeOut;
            float scanPulse = 0.5f + 0.5f * (float)System.Math.Sin(FlightTimer * 0.35f + Projectile.identity * 0.47f);
            float wingOpen = 0.2f + 0.28f * scanPulse;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            DrawReconOpticWing(opticTexture, center, side, 1f, wingOpen, fade);
            DrawReconOpticWing(opticTexture, center, -side, -1f, wingOpen, fade);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private void DrawReconOpticWing(Texture2D opticTexture, Vector2 center, Vector2 wingDirection, float sideSign, float wingOpen, float fade)
        {
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Vector2 origin = new(opticTexture.Width * 0.5f, opticTexture.Height * 0.84f);
            float baseRotation = wingDirection.ToRotation() + MathHelper.PiOver2;
            float flutter = (float)System.Math.Sin(FlightTimer * 0.62f + sideSign * 0.8f + Projectile.identity * 0.13f) * wingOpen;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 basePosition = center + wingDirection * 4f - forward * 1.5f;

            for (int i = 0; i < 3; i++)
            {
                float ghostOffset = i - 1f;
                float opacity = (i == 1 ? 0.38f : 0.14f) * fade;
                Color wingColor = Color.Lerp(mainColor, Color.Lerp(accentColor, Color.White, 0.25f), 0.32f + 0.18f * i) with { A = 0 } * opacity;
                Main.EntitySpriteDraw(
                    opticTexture,
                    basePosition + wingDirection * ghostOffset * 1.6f,
                    null,
                    wingColor,
                    baseRotation + flutter + ghostOffset * 0.2f,
                    origin,
                    new Vector2(0.072f, 0.105f),
                    SpriteEffects.None,
                    0);
            }
        }

        private void ApplyBestTargetMark()
        {
            if (!BFArrowCommon.InBounds(BestTargetIndex, Main.maxNPCs) || !BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
                return;

            NPC target = Main.npc[(int)BestTargetIndex];
            if (!target.active || target.dontTakeDamage)
                return;

            target.GetGlobalNPC<BFArrow_CDetecNPC>().ApplyPriorityMark(Projectile.owner, configuredMarkDuration);
            Main.player[Projectile.owner].GetModPlayer<BFRightUIPlayer>().SetReconPriorityTarget(target.whoAmI, configuredMarkDuration);

            SpawnMarkAcquireFX(target.Center);
            SoundEngine.PlaySound(BlossomFluxSounds.RightReconProjEvent, target.Center);

            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BFArrow_CDetecReticle>(),
                0,
                0f,
                Projectile.owner,
                target.whoAmI);
        }

        private void EmitPenetrationFlightFX()
        {
            if (Main.dedServ || !Projectile.FinalExtraUpdate())
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);

            GeneralParticleHandler.SpawnParticle(new CritSpark(
                Projectile.Center + normal * Main.rand.NextFloat(-7f, 7f),
                direction * Main.rand.NextFloat(1.8f, 3.8f),
                Color.White,
                accentColor,
                0.58f,
                9));
        }

        private void SpawnPenetrationImpactFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                direction * 0.7f,
                Color.Lerp(mainColor, Color.White, 0.18f),
                new Vector2(0.78f, 1.95f),
                direction.ToRotation(),
                0.14f * intensity,
                0.032f,
                10));

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, accentColor, 0.35f),
                0.38f * intensity,
                9));

            // 上下交替爆发 — 参考 Nightwither 的 CritSpark 垂直爆射风格
            for (int i = 0; i < 10; i++)
            {
                float yDir = (i % 2 == 0) ? -1f : 1f;
                float baseSpeed = yDir < 0f ? Main.rand.NextFloat(5f, 9f) : Main.rand.NextFloat(3f, 6f);
                Vector2 sparkVel = new Vector2(0f, yDir * baseSpeed);
                sparkVel = sparkVel.RotatedByRandom(MathHelper.Pi / 7.2f);
                sparkVel *= Main.rand.NextFloat(0.1f, 1.9f) * intensity;

                Color primary = Main.rand.NextBool() ? Color.Cyan : Color.Turquoise;
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    sparkVel,
                    primary,
                    Color.PaleTurquoise,
                    Main.rand.NextFloat(0.55f, 0.85f) * intensity,
                    Main.rand.Next(11, 17)));
            }
        }

        private void SpawnMarkAcquireFX(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);

            float tierScale = 1f + configuredEffectTier * 0.28f;
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.Lerp(mainColor, Color.White, 0.22f), 0.92f * tierScale, 16 + configuredEffectTier * 4));
            for (int i = 0; i < 3 + configuredEffectTier * 2; i++)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    center,
                    Vector2.Zero,
                    Color.Lerp(mainColor, accentColor, 0.25f + i * 0.12f),
                    new Vector2(0.78f + i * 0.16f, 1.35f + i * 0.24f),
                    MathHelper.TwoPi * i / (3f + configuredEffectTier * 2f),
                    (0.14f + i * 0.03f) * tierScale,
                    0.026f,
                    12 + i * 2));
            }
        }
    }
}
