using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.AStage0;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.CStage2;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.DStage3;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.EStage4;
using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.FStage5;
using CalamityLegendsComeBack.Weapons.Vesuvius.Passive;
using CalamityLegendsComeBack.Weapons.Vesuvius.RightClick;
using CalamityLegendsComeBack.Weapons;
using CalamityMod;
using CalamityMod.Cooldowns;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.EXSkill
{
    internal sealed class VesuviusEXPlayer : ModPlayer
    {
        public const int EXMax = 7200;
        public const int EXDisplayMax = 60;

        public int EXValue;
        private bool holdingVesuvius;
        private bool wasEXReady;

        public bool EXReady => EXValue >= EXMax;

        public override void ResetEffects()
        {
            holdingVesuvius = false;
        }

        public override void PostUpdate()
        {
            if (holdingVesuvius)
                EXValue = Utils.Clamp(EXValue + 1, 0, EXMax);
            else
                EXValue = Utils.Clamp(EXValue - 2, 0, EXMax);

            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasEXReady, EXReady);
        }

        public void SetHoldingVesuvius()
        {
            holdingVesuvius = true;
        }

        public void GainEX(int displayUnits)
        {
            EXValue = Utils.Clamp(EXValue + Math.Max(1, displayUnits) * GetFramesPerDisplayUnit(), 0, EXMax);
        }

        public bool ConsumeAllEX()
        {
            if (!EXReady)
                return false;

            EXValue = 0;
            return true;
        }

        public static int GetFramesPerDisplayUnit() => EXMax / EXDisplayMax;
    }

    internal sealed class VesuviusEXCooldown : CooldownHandler
    {
        public static new string ID => "Vesuvius_EX";

        private float AdjustedCompletion => instance.timeLeft / (float)VesuviusEXPlayer.EXMax;
        private int DisplayValue => Utils.Clamp(instance.timeLeft / VesuviusEXPlayer.GetFramesPerDisplayUnit(), 0, VesuviusEXPlayer.EXDisplayMax);

        public override bool CanTickDown => false;

        public override bool ShouldDisplay => instance.player.HeldItem.type == ModContent.ItemType<NewVesuvius>();

        public override LocalizedText DisplayName => Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.Vesuvius_EX");

        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOverlay";

        public override Color OutlineColor => new(78, 28, 16);
        public override Color CooldownStartColor => Color.Lerp(new Color(110, 35, 18), new Color(255, 96, 28), AdjustedCompletion);
        public override Color CooldownEndColor => Color.Lerp(new Color(34, 16, 12), new Color(255, 220, 90), AdjustedCompletion);

        public override void ApplyBarShaders(float opacity)
        {
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSaturation(AdjustedCompletion);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseColor(CooldownStartColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSecondaryColor(CooldownEndColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].Apply();
        }

        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            base.DrawExpanded(spriteBatch, position, opacity, scale);
            DrawCounter(spriteBatch, position, opacity, scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D icon = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0f, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(icon, position, null, Color.White * opacity, 0f, icon.Size() * 0.5f, scale * 0.62f, SpriteEffects.None, 0f);

            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop = new(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(
                overlay,
                position + Vector2.UnitY * lostHeight * scale,
                crop,
                OutlineColor * opacity * 0.82f,
                0f,
                overlay.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);

            DrawCounter(spriteBatch, position, opacity, scale);
        }

        private void DrawCounter(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Vector2 textOffset = new(DisplayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, DisplayValue.ToString(), position + textOffset * scale, Color.White * opacity, Color.Black * opacity, scale);
        }
    }

    internal class VesuviusEXWeapon : ModProjectile, ILocalizedModType
    {
        private const int ChargeTime = 180;
        private const int EruptionTime = 300;
        private const int EndTime = 30;

        private int state;
        private int timer;
        private SlotId chargeSoundSlot;

        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuvius";

        private Player Owner => Main.player[Projectile.owner];
        private Vector2 GunTip => Projectile.Center - Vector2.UnitY * 58f;

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ChargeTime + EruptionTime + EndTime + 180;
            Projectile.DamageType = DamageClass.Magic;
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

            UpdateHeldPosition();
            ManipulateOwner();

            switch (state)
            {
                case 0:
                    ChargePhase();
                    break;
                case 1:
                    ReadyPhase();
                    break;
                case 2:
                    EruptionPhase();
                    break;
                default:
                    EndPhase();
                    break;
            }
        }

        private void UpdateHeldPosition()
        {
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, true) - Vector2.UnitY * (58f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 3f);
            Projectile.rotation = -MathHelper.PiOver2 + MathHelper.ToRadians(45f * Owner.direction);
            Projectile.direction = Owner.direction;
            Projectile.spriteDirection = Owner.direction;
        }

        private void ManipulateOwner()
        {
            Owner.ChangeDir(Main.MouseWorld.X >= Owner.Center.X ? 1 : -1);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = Projectile.rotation * Owner.direction;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        }

        private void ChargePhase()
        {
            timer++;
            float progress = timer / (float)ChargeTime;

            if (timer == 1)
            {
                chargeSoundSlot = SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.76f, Pitch = -0.45f }, Projectile.Center);
            }

            SpawnChargeEffects(progress);

            if (timer < ChargeTime)
                return;

            if (SoundEngine.TryGetActiveSound(chargeSoundSlot, out var sound))
                sound?.Stop();

            state = 1;
            timer = 0;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1.05f, Pitch = -0.2f }, Projectile.Center);
            ApplyScreenShake(11f);
        }

        private void ReadyPhase()
        {
            timer++;
            SpawnReadyEffects();

            if (Main.myPlayer == Projectile.owner && Main.mouseLeft && Main.mouseLeftRelease && !Owner.mouseInterface && !Main.mapFullscreen && !Main.blockMouse)
            {
                state = 2;
                timer = 0;
                Projectile.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, Pitch = -0.42f }, Projectile.Center);
                ApplyScreenShake(18f);
            }
        }

        private void EruptionPhase()
        {
            timer++;
            SpawnEruptionEffects(timer / (float)EruptionTime);

            if (Main.myPlayer == Projectile.owner)
                SpawnEruptionProjectiles();

            Owner.velocity -= Vector2.UnitY * 0.09f;

            if (timer >= EruptionTime)
            {
                state = 3;
                timer = 0;
            }
        }

        private void EndPhase()
        {
            timer++;
            if (timer >= EndTime)
                Projectile.Kill();
        }

        private void SpawnEruptionProjectiles()
        {
            if (timer % 2 == 0)
            {
                Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.65f, 0.65f)) * Main.rand.NextFloat(12f, 20f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTip + Main.rand.NextVector2Circular(28f, 16f),
                    velocity,
                    ModContent.ProjectileType<VesuviusMoltenAsteroid>(),
                    Math.Max(1, (int)(Projectile.damage * 0.34f)),
                    Projectile.knockBack * 0.3f,
                    Projectile.owner,
                    Main.rand.Next(6),
                    Main.rand.NextFloat(0.48f, 0.78f),
                    1f);
            }

            if (timer % 8 == 0)
            {
                Vector2 spawnPosition = Owner.Center + new Vector2(Main.rand.NextFloat(-760f, 760f), Main.rand.NextFloat(-820f, -620f));
                Vector2 target = FindMeteorTarget();
                Vector2 velocity = (target - spawnPosition).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(15f, 22f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<VesuviusVolcanicBomb>(),
                    Math.Max(1, (int)(Projectile.damage * 0.62f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    Main.rand.Next(6),
                    Main.rand.NextFloat(0.95f, 1.35f));
            }

            if (timer % 10 == 3)
            {
                Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-1.05f, 1.05f)) * Main.rand.NextFloat(8f, 13f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTip + Main.rand.NextVector2Circular(34f, 24f),
                    velocity,
                    ModContent.ProjectileType<VesuviusHomingCinder>(),
                    Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    Projectile.knockBack * 0.22f,
                    Projectile.owner);
            }

            if (timer % 26 == 6)
            {
                Vector2 velocity = Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.55f, 0.55f)) * 29f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.Center + new Vector2(Main.rand.NextFloat(-520f, 520f), -620f),
                    velocity,
                    ModContent.ProjectileType<VesuviusMagmaPillar>(),
                    Math.Max(1, (int)(Projectile.damage * 0.58f)),
                    Projectile.knockBack * 0.25f,
                    Projectile.owner);
            }

            if (timer % 54 == 18)
            {
                Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * 18f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTip + Main.rand.NextVector2Circular(36f, 20f),
                    velocity,
                    ModContent.ProjectileType<VesuviusObsidianShard>(),
                    Math.Max(1, (int)(Projectile.damage * 1.08f)),
                    Projectile.knockBack,
                    Projectile.owner);
            }

            if (timer % 4 == 0)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.Center + new Vector2(Main.rand.NextFloat(-540f, 540f), Main.rand.NextFloat(-360f, -180f)),
                    new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(2.8f, 5.5f)),
                    ModContent.ProjectileType<VesuviusAshFall>(),
                    Math.Max(1, (int)(Projectile.damage * 0.12f)),
                    0f,
                    Projectile.owner,
                    Main.rand.NextBool() ? -1f : 1f);
            }

            if (timer % 18 == 0)
            {
                Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.8f, 0.8f)) * Main.rand.NextFloat(8f, 14f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTip,
                    velocity,
                    ModContent.ProjectileType<VesuviusFaultFireball>(),
                    Math.Max(1, (int)(Projectile.damage * 0.24f)),
                    Projectile.knockBack * 0.3f,
                    Projectile.owner,
                    5f);
            }
        }

        private Vector2 FindMeteorTarget()
        {
            NPC target = null;
            float distance = 900f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float current = Vector2.Distance(Owner.Center, npc.Center);
                if (current < distance)
                {
                    distance = current;
                    target = npc;
                }
            }

            return target?.Center ?? Owner.Calamity().mouseWorld;
        }

        private void SpawnChargeEffects(float progress)
        {
            if (Main.dedServ)
                return;

            Color color = Color.Lerp(new Color(255, 80, 24), new Color(255, 230, 90), progress);
            if (timer % 2 == 0)
            {
                RancorLavaMetaball.SpawnParticle(GunTip + Main.rand.NextVector2Circular(22f, 22f), Main.rand.NextFloat(24f, 58f) * (0.35f + progress));
                Particle smoke = new HeavySmokeParticle(
                    GunTip + Main.rand.NextVector2Circular(70f * progress, 40f * progress),
                    (GunTip - (Projectile.Center + Main.rand.NextVector2Circular(180f, 140f))).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1f, 5f),
                    Color.Lerp(Color.DarkGray, color, 0.28f),
                    Main.rand.Next(28, 56),
                    Main.rand.NextFloat(0.55f, 1.25f) * (0.4f + progress),
                    0.76f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    progress > 0.7f,
                    required: true);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (timer % 8 == 0)
                GeneralParticleHandler.SpawnParticle(new PulseRing(GunTip, Vector2.Zero, color * 0.62f, 0.08f + progress * 0.2f, 1.8f + progress * 2.4f, 18));
        }

        private void SpawnReadyEffects()
        {
            if (Main.dedServ)
                return;

            Color color = new Color(255, 220, 80);
            RancorLavaMetaball.SpawnParticle(GunTip + Main.rand.NextVector2Circular(16f, 16f), Main.rand.NextFloat(36f, 70f));

            // Fully charged idle intentionally avoids shockwave rings.
        }

        private void SpawnEruptionEffects(float progress)
        {
            if (Main.dedServ)
                return;

            ApplyScreenShake(4f + progress * 9f);

            for (int i = 0; i < 4; i++)
            {
                Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.52f, 0.52f)) * Main.rand.NextFloat(7f, 18f);
                RancorLavaMetaball.SpawnParticle(GunTip + Main.rand.NextVector2Circular(32f, 18f), Main.rand.NextFloat(34f, 76f));

                Particle smoke = new HeavySmokeParticle(
                    GunTip + Main.rand.NextVector2Circular(42f, 28f),
                    velocity * Main.rand.NextFloat(0.12f, 0.34f),
                    Color.Lerp(Color.DimGray, Color.OrangeRed, 0.22f),
                    Main.rand.Next(30, 62),
                    Main.rand.NextFloat(0.8f, 1.9f),
                    0.86f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    true,
                    required: true);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(2200f, 180f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(chargeSoundSlot, out var sound))
                sound?.Stop();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuviusGlow").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f);
            Color color = state == 2 ? new Color(255, 90, 28, 0) : new Color(255, 210, 80, 0);

            Main.EntitySpriteDraw(bloom, GunTip - Main.screenPosition, null, color * (0.28f + pulse * 0.18f), 0f, bloom.Size() * 0.5f, state == 2 ? 1.2f + pulse * 0.45f : 0.7f + pulse * 0.22f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * (0.75f + pulse * 0.25f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
