using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.FairyDance
{
    internal sealed class FairyDanceWisp : ModProjectile
    {
        internal const int FairyCount = 3;
        private const float DashSpeed = 21f;
        private const float MaxDashDistance = 640f;

        // Vanilla resources: Terraria/Images/NPC_583, NPC_584 and NPC_585.
        private static readonly string[] FairyTextures =
        {
            "Terraria/Images/NPC_583",
            "Terraria/Images/NPC_584",
            "Terraria/Images/NPC_585"
        };

        private ref float VariantValue => ref Projectile.ai[0];
        private ref float StateValue => ref Projectile.ai[1];
        private int Variant => Utils.Clamp((int)VariantValue, 0, FairyCount - 1);
        private int State { get => (int)StateValue; set => StateValue = value; }
        private int dashTimer;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ArmorPenetration = 10;
        }

        public override bool? CanDamage() => State == 1 ? null : false;

        public override void AI()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            BFAccessoryPlayer accessoryPlayer = owner.GetModPlayer<BFAccessoryPlayer>();
            if (!owner.active || owner.dead || !accessoryPlayer.FairyDanceEquipped)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            owner.AddBuff(GetFairyBuff(Variant), 2);
            Lighting.AddLight(Projectile.Center, GetFairyColor(Variant).ToVector3() * 0.35f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }

            switch (State)
            {
                case 1:
                    UpdateDash(owner);
                    break;
                case 2:
                    UpdateReturn(owner);
                    break;
                default:
                    UpdateOrbit(owner);
                    break;
            }

            Projectile.rotation = Projectile.velocity.X * 0.1f;
        }

        internal void TriggerDash(Vector2 target, int baseDamage)
        {
            // The charged right-click release must recall and relaunch all three fairies
            // even if a normal 20/30-damage contact dash is still in progress.
            if (State == 1 && baseDamage < 120)
                return;

            Player owner = Main.player[Projectile.owner];
            Vector2 direction = (target - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.velocity = direction * DashSpeed;
            Projectile.damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(baseDamage));
            Projectile.CritChance = 0;
            dashTimer = 0;
            State = 1;
            Projectile.netUpdate = true;
        }

        private void UpdateOrbit(Player owner)
        {
            float angle = Main.GameUpdateCount * 0.025f + MathHelper.TwoPi * Variant / FairyCount;
            Vector2 desiredPosition = owner.Center + new Vector2(72f, 0f).RotatedBy(angle) + Vector2.UnitY * (float)Math.Sin(angle * 1.7f) * 12f;
            MoveTowards(desiredPosition, 0.18f, 12f);
            Projectile.damage = 0;
        }

        private void UpdateDash(Player owner)
        {
            dashTimer++;
            if (dashTimer >= 48 || Vector2.DistanceSquared(Projectile.Center, owner.Center) >= MaxDashDistance * MaxDashDistance)
            {
                State = 2;
                Projectile.damage = 0;
                Projectile.netUpdate = true;
            }
        }

        private void UpdateReturn(Player owner)
        {
            float angle = Main.GameUpdateCount * 0.025f + MathHelper.TwoPi * Variant / FairyCount;
            Vector2 desiredPosition = owner.Center + new Vector2(72f, 0f).RotatedBy(angle);
            MoveTowards(desiredPosition, 0.22f, 18f);
            Projectile.damage = 0;

            if (Vector2.DistanceSquared(Projectile.Center, desiredPosition) <= 24f * 24f)
            {
                State = 0;
                Projectile.netUpdate = true;
            }
        }

        private void MoveTowards(Vector2 destination, float inertia, float maxSpeed)
        {
            Vector2 desiredVelocity = (destination - Projectile.Center) * inertia;
            if (desiredVelocity.LengthSquared() > maxSpeed * maxSpeed)
                desiredVelocity = desiredVelocity.SafeNormalize(Vector2.Zero) * maxSpeed;

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.24f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.DisableCrit();

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(FairyTextures[Variant]).Value;
            Rectangle frame = texture.Frame(1, 4, 0, Projectile.frame);
            SpriteEffects effects = Projectile.velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color drawColor = Color.Lerp(Projectile.GetAlpha(lightColor), Color.White, 0.35f);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                drawColor,
                Projectile.rotation,
                frame.Size() * 0.5f,
                Projectile.scale,
                effects);
            return false;
        }

        private static Color GetFairyColor(int variant) => variant switch
        {
            1 => Color.LimeGreen,
            2 => Color.RoyalBlue,
            _ => Color.HotPink
        };

        private static int GetFairyBuff(int variant) => variant switch
        {
            1 => ModContent.BuffType<GreenFairyBlessing>(),
            2 => ModContent.BuffType<BlueFairyBlessing>(),
            _ => ModContent.BuffType<PinkFairyBlessing>()
        };
    }

    internal sealed class RainbowSpiritLacewing : ModProjectile
    {
        internal const int LifetimeFrames = 18 * 60;
        internal const int MaximumCount = 7;
        private const float DashSpeed = 30f;
        private const string LacewingTexture = "Terraria/Images/NPC_661";

        private int State { get => (int)Projectile.ai[1]; set => Projectile.ai[1] = value; }
        private int shootTimer;
        private double animationCounter;
        private bool wasRightHeld;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeFrames;
        }

        public override bool? CanDamage() => State == 1 ? null : false;

        public override void AI()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            BFAccessoryPlayer accessoryPlayer = owner.GetModPlayer<BFAccessoryPlayer>();
            if (!owner.active || owner.dead || !accessoryPlayer.RainbowSpiritDanceEquipped)
            {
                Projectile.Kill();
                return;
            }

            UpdateAnimation();
            Lighting.AddLight(Projectile.Center, Main.hslToRgb((float)(Main.GlobalTimeWrappedHourly * 0.35f + Projectile.identity * 0.11f) % 1f, 0.9f, 0.62f).ToVector3() * 0.48f);

            if (State == 1)
            {
                if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > 1400f * 1400f)
                    Projectile.Kill();
                return;
            }

            bool localOwner = Projectile.owner == Main.myPlayer;
            BFRightUIPlayer input = owner.GetModPlayer<BFRightUIPlayer>();
            bool rightHeld = localOwner && accessoryPlayer.HoldingBlossomFlux && input.RightMouseHeld;
            float orbitSpeed = rightHeld ? 0.17f : 0.035f;
            float angle = Main.GameUpdateCount * orbitSpeed + MathHelper.TwoPi * ((int)Projectile.ai[0] % MaximumCount) / MaximumCount;
            float radius = rightHeld ? 82f : 98f;
            Vector2 desiredPosition = owner.Center + new Vector2(radius, 0f).RotatedBy(angle);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, (desiredPosition - Projectile.Center) * 0.24f, rightHeld ? 0.34f : 0.2f);
            if (Projectile.velocity.LengthSquared() > 22f * 22f)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * 22f;

            if (localOwner && wasRightHeld && !rightHeld)
            {
                ReleaseTowards(Main.MouseWorld);
                return;
            }

            wasRightHeld = rightHeld;
            if (rightHeld)
            {
                shootTimer = 0;
                return;
            }

            bool leftHeld = localOwner && accessoryPlayer.HoldingBlossomFlux && Main.mouseLeft && !owner.mouseInterface && !Main.blockMouse;
            if (!leftHeld)
            {
                shootTimer = 0;
                return;
            }

            shootTimer++;
            if (shootTimer >= 45)
            {
                shootTimer = 0;
                FireRainbowBolt(owner, Main.MouseWorld);
            }
        }

        private void ReleaseTowards(Vector2 target)
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.velocity = (target - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction) * DashSpeed;
            Projectile.damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(1200f));
            Projectile.penetrate = 1;
            State = 1;
            Projectile.netUpdate = true;
        }

        private void FireRainbowBolt(Player owner, Vector2 target)
        {
            Vector2 velocity = (target - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction) * 28f;
            int damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(60f));
            int index = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                velocity,
                ModContent.ProjectileType<RainbowSpiritBolt>(),
                damage,
                1f,
                Projectile.owner,
                Projectile.Center.X,
                Projectile.Center.Y);

            if (index >= 0 && index < Main.maxProjectiles)
                Main.projectile[index].CritChance = (int)Math.Round(owner.GetTotalCritChance(DamageClass.Ranged));
        }

        private void UpdateAnimation()
        {
            animationCounter += 1d + (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.5d;
            const int frameInterval = 7;
            if (animationCounter < frameInterval)
                Projectile.frame = 0;
            else if (animationCounter < frameInterval * 2)
                Projectile.frame = 1;
            else if (animationCounter < frameInterval * 3)
                Projectile.frame = 2;
            else
                Projectile.frame = 1;

            if (animationCounter >= frameInterval * 4 - 1)
                animationCounter = 0d;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (State == 1)
                modifiers.SetCrit();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(LacewingTexture).Value;
            Rectangle frame = texture.Frame(1, 3, 0, Projectile.frame);
            SpriteEffects effects = Projectile.velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color rainbow = Main.hslToRgb((float)(Main.GlobalTimeWrappedHourly * 0.4f + Projectile.identity * 0.13f) % 1f, 1f, 0.68f);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, rainbow * 0.35f, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale * 1.12f, effects);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, effects);
            return false;
        }
    }

    internal sealed class RainbowSpiritBolt : ModProjectile
    {
        private const float MaximumRange = 35f * 16f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Vector2 origin = new(Projectile.ai[0], Projectile.ai[1]);
            if (Vector2.DistanceSquared(Projectile.Center, origin) >= MaximumRange * MaximumRange)
            {
                Projectile.Kill();
                return;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Color color = Main.hslToRgb((float)(Main.GlobalTimeWrappedHourly * 0.6f + Projectile.identity * 0.17f) % 1f, 1f, 0.65f);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.55f);
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, -Projectile.velocity * 0.04f, 100, color, 0.8f);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color color = Main.hslToRgb((float)(Main.GlobalTimeWrappedHourly * 0.6f + Projectile.identity * 0.17f) % 1f, 1f, 0.65f);
            Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition - direction * 18f, null, color * 0.8f, Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(36f, 4f), SpriteEffects.None);
            Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition - direction * 10f, null, Color.White, Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(20f, 1.5f), SpriteEffects.None);
            return false;
        }
    }
}
