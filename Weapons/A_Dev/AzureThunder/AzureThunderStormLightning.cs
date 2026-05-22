using System;
using System.IO;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderStormLightning : ModProjectile, ILocalizedModType
    {
        public const int GainChargeFlag = 1;
        public const int StaticDischargeFlag = 2;
        public const int BigLightningFlag = 4;
        public const int CrumblingFlag = 8;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/LightningProj";

        private int noTileHitCounter = 81;
        private bool hasPlayedSound;

        private ref float InitialVelocityAngle => ref Projectile.ai[0];
        private ref float BaseTurnSeed => ref Projectile.ai[1];
        private int Flags => (int)Projectile.ai[2];
        private bool GainCharge => (Flags & GainChargeFlag) != 0;
        private bool ApplyStaticDischarge => (Flags & StaticDischargeFlag) != 0;
        private bool BigLightning => (Flags & BigLightningFlag) != 0;
        private bool ApplyCrumbling => (Flags & CrumblingFlag) != 0;

        private float WidthMultiplier => BigLightning ? 1.75f : 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
            ProjectileID.Sets.TrailingMode[Type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 50;
        }

        public override void SetDefaults()
        {
            Projectile.width = 35;
            Projectile.height = 35;
            Projectile.alpha = 255;
            Projectile.penetrate = 4;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.MaxUpdates = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Projectile.MaxUpdates * 45;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            noTileHitCounter--;
            if (noTileHitCounter == 0)
                Projectile.tileCollide = true;

            if (Main.rand.NextBool(10))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, Vector2.Zero, 100, AzureThunderColors.PaleYellow, Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
                dust.position = Projectile.Center;
            }

            Projectile.frameCounter++;
            Projectile.oldPos[1] = Projectile.oldPos[0];

            float adjustedTimeLeft = Projectile.timeLeft / (float)Projectile.MaxUpdates;
            Projectile.Opacity = Utils.GetLerpValue(0f, 9f, adjustedTimeLeft, true) * Utils.GetLerpValue(45f, 42f, adjustedTimeLeft, true);
            Projectile.scale = Projectile.Opacity;

            if (!hasPlayedSound)
            {
                SoundStyle lightningSound = CommonCalamitySounds.LightningSound;
                lightningSound.Volume = BigLightning ? 0.75f : 0.5f;
                SoundEngine.PlaySound(lightningSound, Main.player[Projectile.owner].Center);
                hasPlayedSound = true;
            }

            Lighting.AddLight(Projectile.Center, AzureThunderColors.PaleYellow.ToVector3() * (BigLightning ? 1.25f : 0.8f));
            if (Projectile.frameCounter < Projectile.extraUpdates * 2)
                return;

            Projectile.frameCounter = 0;
            float originalSpeed = MathHelper.Min(40f, Projectile.velocity.Length());
            UnifiedRandom unifiedRandom = new((int)BaseTurnSeed);
            int turnTries = 0;
            Vector2 newBaseDirection = -Vector2.UnitY;

            do
            {
                BaseTurnSeed = unifiedRandom.Next() % 100;
                Vector2 potentialBaseDirection = (BaseTurnSeed / 100f * MathHelper.TwoPi).ToRotationVector2();
                potentialBaseDirection.Y = -Math.Abs(potentialBaseDirection.Y);

                bool canChangeLightningDirection = potentialBaseDirection.Y <= -0.2f &&
                    potentialBaseDirection.X >= -0.2f &&
                    potentialBaseDirection.X <= 0.2f;

                if (canChangeLightningDirection)
                    newBaseDirection = potentialBaseDirection;

                turnTries++;
            }
            while (turnTries < 20);

            if (Projectile.velocity != Vector2.Zero)
            {
                Projectile.velocity = newBaseDirection.RotatedBy(InitialVelocityAngle + MathHelper.PiOver2) * originalSpeed;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
        }

        private float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPosition)
        {
            return CalamityUtils.Convert01To010(completionRatio) * Projectile.scale * Projectile.width * WidthMultiplier;
        }

        private Color PrimitiveColorFunction(float completionRatio, Vector2 vertexPosition)
        {
            float pulse = (float)Math.Sin(Projectile.identity / 3f + completionRatio * 20f + Main.GlobalTimeWrappedHourly * 1.1f) * 0.5f + 0.5f;
            Color color = CalamityUtils.MulticolorLerp(pulse, AzureThunderColors.Yellow, AzureThunderColors.PaleYellow, AzureThunderColors.Azure);
            color.A = 0;
            return color;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item93, Projectile.position);
            target.AddBuff(BuffID.Electrified, 240);
            if (ApplyStaticDischarge)
            {
                Player ownerPlayer = Main.player[Projectile.owner];
                if (ownerPlayer.GetModPlayer<AzureThunderPlayer>().HarmonyActive)
                    AzureThunderPlayer.ApplyUltimateDot(target, 180);
                else
                    target.AddBuff(ModContent.BuffType<StaticDischarge>(), 180);
            }

            if (ApplyCrumbling)
                target.AddBuff(ModContent.BuffType<CalamityMod.Buffs.StatDebuffs.Crumbling>(), 180);

            Player owner = Main.player[Projectile.owner];
            if (Projectile.localAI[1] > 0f)
                owner.GetModPlayer<AzureThunderPlayer>().AddUltimateEnergy((int)Projectile.localAI[1]);

            if (GainCharge)
                owner.GetModPlayer<AzureThunderPlayer>().TryGainThunderChargeFromTarget(target);

            Sparks();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:HeavenlyGaleLightningArc"].UseImage1("Images/Misc/Perlin");
            GameShaders.Misc["CalamityMod:HeavenlyGaleLightningArc"].Apply();

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(
                    PrimitiveWidthFunction,
                    PrimitiveColorFunction,
                    (_, _) => Projectile.Size * 0.3f,
                    smoothen: false,
                    pixelate: false,
                    shader: GameShaders.Misc["CalamityMod:HeavenlyGaleLightningArc"]),
                10);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCHit53 with { Volume = BigLightning ? 0.75f : 0.48f }, Projectile.Center);
            Sparks();
        }

        private void Sparks()
        {
            for (int i = 0; i < (BigLightning ? 10 : 5); i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-10f, 0f)).RotatedByRandom(0.45f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, velocity, 0, Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure, Main.rand.NextFloat(0.9f, 1.35f) * WidthMultiplier);
                dust.noGravity = true;
            }
        }
    }
}
