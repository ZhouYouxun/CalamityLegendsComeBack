using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
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

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFArrow_CDetec : ModProjectile
    {
        private const int PriorityMarkDuration = 15 * 60;
        private const float MaxScanRadius = 240f;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/CDetec/BFArrow_CDetec";

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
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.12f, Pitch = 0.72f }, Projectile.Center);
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
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.28f, Pitch = 0.24f }, target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_CDetec, 12, 1f, 3.8f, 0.82f, 1.18f);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.25f, Pitch = 0.35f }, Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            ApplyBestTargetMark();
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_CDetec, 14, 1.1f, 4.8f, 0.88f, 1.25f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_CDetec, 1.05f);
            return false;
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
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.38f, Pitch = 0.34f }, target.Center);

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
