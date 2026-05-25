using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderGroundSword : ModProjectile, ILocalizedModType
    {
        public const int MaxGroundSwords = 9;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunder";

        private bool initialized;
        private Vector2 diveTarget;
        private int outlinePulse;

        private int Mode
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private int Timer
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private bool Diving => Mode == 1;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 42;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = AzureThunderProgression.GroundSwordLifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.scale = 0.66f;
        }

        public override bool? CanDamage() => Diving && Timer > 12;

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                SnapToGroundIfPossible();
                Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4 + MathHelper.ToRadians(Main.rand.NextBool() ? 5f : -5f);
                AzureThunderSounds.PlaySwordMaterialize(Projectile.Center);
                SpawnIntroBurst();
                PulseLightningOutline();
                Projectile.netUpdate = true;
            }

            Timer++;
            if (outlinePulse > 0)
                outlinePulse--;

            if (Diving)
            {
                DoDiveAI();
                return;
            }

            if (AzureThunderAccessoryPlayer.ShouldGroundSwordFollowPlayer(Projectile, out int followSlot))
                DoFollowOwnerAI(followSlot);
            else
                Projectile.velocity *= 0f;

            Lighting.AddLight(Projectile.Center, AzureThunderColors.Yellow.ToVector3() * 0.28f);

            if (Timer <= 45)
                SpawnChargeVisuals();

            if (Projectile.timeLeft <= 18)
                SpawnDeathChargeVisuals();
        }

        private void DoFollowOwnerAI(int followSlot)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 slotOffset = new Vector2((followSlot - 1) * 54f, -88f - Math.Abs(followSlot - 1) * 20f);
            slotOffset.X *= owner.direction == 0 ? 1 : owner.direction;
            Vector2 desiredCenter = owner.MountedCenter + slotOffset;
            Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, 0.12f);
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.rotation.AngleLerp(MathHelper.PiOver2 + MathHelper.PiOver4 + MathHelper.ToRadians((followSlot - 1) * 8f), 0.12f);
        }

        public void BeginDive(Vector2 target, int damage, float knockback)
        {
            Mode = 1;
            Timer = 0;
            diveTarget = target;
            Projectile.damage = Math.Max(1, damage);
            Projectile.knockBack = knockback;
            Projectile.friendly = false;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, 90);
            AzureThunderSounds.PlaySwordLaunch(Projectile.Center);
            Projectile.netUpdate = true;
        }

        public void PulseLightningOutline()
        {
            outlinePulse = Math.Max(outlinePulse, 20);
        }

        private void DoDiveAI()
        {
            if (diveTarget == Vector2.Zero)
                diveTarget = Projectile.Center + Vector2.UnitY * 320f;

            if (Timer <= 12)
            {
                Projectile.friendly = false;
                Projectile.velocity *= 0.8f;
                Projectile.rotation = Projectile.rotation.AngleLerp((diveTarget - Projectile.Center).ToRotation() + MathHelper.PiOver4, 0.18f);
                SpawnChargeVisuals();
                return;
            }

            Projectile.friendly = true;
            Vector2 desiredVelocity = (diveTarget - Projectile.Center).SafeNormalize(Vector2.UnitY) * 26f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.22f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (Projectile.Distance(diveTarget) < 32f)
                Projectile.Kill();
        }

        private void SnapToGroundIfPossible()
        {
            Vector2 originalCenter = Projectile.Center;
            Point startTile = originalCenter.ToTileCoordinates();

            for (int y = startTile.Y; y < startTile.Y + 45 && y < Main.maxTilesY - 2; y++)
            {
                Tile tile = Framing.GetTileSafely(startTile.X, y);
                if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
                    continue;

                Projectile.Center = new Vector2(originalCenter.X, y * 16f - 16f);
                return;
            }
        }

        private void SpawnChargeVisuals()
        {
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(28f, 34f),
                    DustID.FireworksRGB,
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.75f, 1.2f));
                dust.noGravity = true;
            }
        }

        private void SpawnIntroBurst()
        {
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 18f) * Main.rand.NextFloat(1.2f, 4.2f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(6f, 18f),
                    DustID.FireworksRGB,
                    velocity,
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.75f, 1.25f));
                dust.noGravity = true;
            }
        }

        private void SpawnDeathChargeVisuals()
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center,
                DustID.FireworksRGB,
                Vector2.UnitY.RotatedByRandom(0.8f) * Main.rand.NextFloat(-5f, -2f),
                0,
                AzureThunderColors.PaleYellow,
                Main.rand.NextFloat(1.1f, 1.6f));
            dust.noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.FireworksRGB,
                    Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2f, 8f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.Yellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.8f, 1.45f));
                dust.noGravity = true;
            }

            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 direction = (MathHelper.PiOver2 + MathHelper.TwoPi * i / 3f).ToRotationVector2();
                    Vector2 spawnPosition = Projectile.Center + direction * 10f * 16f;

                    AzureThunderPlayer.SpawnFlatLightning(
                        Projectile.GetSource_FromThis(),
                        spawnPosition,
                        Projectile.Center - spawnPosition,
                        Math.Max(1, (int)(Projectile.damage * 0.35f)),
                        Projectile.knockBack,
                        Projectile.owner,
                        Diving ? 1.05f : 0.7f);
                }
            }

            AzureThunderSounds.PlaySwordBurst(Projectile.Center, Diving);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 240);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            if (Diving)
                AzureThunderSounds.PlaySwordHit(target.Center);
            if (Diving)
                AzureThunderPlayer.ApplyUltimateDot(target, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            float fadeIn = 1f - (float)Math.Pow(1f - Utils.GetLerpValue(0f, 18f, Timer, true), 3f);
            float chargeCompletion = MathHelper.Clamp(Timer / 45f, 0f, 1f);
            float pulseCompletion = outlinePulse / 20f;
            float pulseOpacity = (float)Math.Pow(pulseCompletion, 0.55f) * 0.52f;
            float outlineOpacity = (Diving ? 0.34f : MathHelper.Lerp(0.35f, 0.12f, chargeCompletion)) * fadeIn + pulseOpacity;
            float outlineRadius = (Diving ? 4.4f : 3f) + pulseCompletion * 7f;
            float drawScale = Projectile.scale * (0.72f + 0.28f * fadeIn + (float)Math.Sin(fadeIn * MathHelper.Pi) * 0.05f);
            Color drawColor = lightColor * fadeIn;

            HoldoutOutlineHelper.DrawSolidOutline(
                texture,
                drawPosition,
                Projectile.rotation,
                origin,
                Vector2.One * drawScale,
                SpriteEffects.None,
                outlinePulse > 0 ? AzureThunderColors.PaleYellow : Diving ? AzureThunderColors.Azure : AzureThunderColors.Yellow,
                outlineRadius,
                outlineOpacity,
                Main.GlobalTimeWrappedHourly + Projectile.identity * 0.1f,
                outlinePulse > 0 ? 22 : 16);

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, drawScale, SpriteEffects.None);
            return false;
        }
    }
}
