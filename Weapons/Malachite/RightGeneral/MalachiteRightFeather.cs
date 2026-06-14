using CalamityLegendsComeBack.Accssory.MC.MalachiteFeather;
using CalamityLegendsComeBack.Accssory.MC.PeacockDart;
using CalamityLegendsComeBack.Weapons.Malachite.RightGeneral.Stealth;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    internal enum MalachiteRightFeatherState
    {
        Stored,
        Released
    }

    public sealed class MalachiteRightFeather : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private MalachiteRightFeatherState State
        {
            get => (MalachiteRightFeatherState)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        private bool StealthEnhanced => Projectile.ai[2] >= 100f;
        private int ReleasedTotal => StealthEnhanced ? (int)Projectile.ai[2] - 100 : (int)Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];

        private float hoverAngle;
        private Vector2 QueuedTarget => new(Projectile.localAI[1], Projectile.localAI[2]);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 360;
            Projectile.extraUpdates = 0;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool ShouldUpdatePosition() => State == MalachiteRightFeatherState.Released && Timer > 0f;

        public override bool? CanDamage() => State == MalachiteRightFeatherState.Released && Timer > 0f;
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (State == MalachiteRightFeatherState.Stored)
            {
                Timer++;
                AIStored(owner);
                return;
            }

            if (Timer <= 0f)
            {
                Timer++;
                AIStored(owner);
                if (Timer >= 0f)
                    Launch(owner);

                return;
            }

            Timer++;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
            Projectile.velocity *= 1.003f;
            ApplyPeacockDartGravity(owner);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.08f, 0.42f, 0.16f);
            SpawnFlightDust();
        }

        public static bool HasStoredRightFeathers(Player player) => CountStoredRightFeathers(player) > 0;

        public static int CountStoredRightFeathers(Player player)
        {
            int count = 0;
            int type = ModContent.ProjectileType<MalachiteRightFeather>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == player.whoAmI &&
                    projectile.type == type &&
                    (MalachiteRightFeatherState)(int)projectile.ai[0] == MalachiteRightFeatherState.Stored)
                {
                    count++;
                }
            }

            return count;
        }

        public static bool TrySpawnStoredRightFeather(Player player, IEntitySource source, int damage, float knockback)
        {
            int currentCount = CountStoredRightFeathers(player);
            if (currentCount >= MalachiteBalance.RightFeatherMaxCount)
                return false;

            int projType = ModContent.ProjectileType<MalachiteRightFeather>();
            int index = Projectile.NewProjectile(
                source,
                player.Center,
                Vector2.Zero,
                projType,
                Math.Max(1, (int)(damage * 0.92f)),
                knockback,
                player.whoAmI,
                (float)MalachiteRightFeatherState.Stored,
                (float)currentCount);

            if (index >= 0 && index < 1000)
            {
                Projectile projectile = Main.projectile[index];
                projectile.alpha = 95;
                projectile.netUpdate = true;
            }

            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.28f, Pitch = 0.65f, MaxInstances = 4 }, player.Center);
            return true;
        }

        public static bool ReleaseStoredRightFeathers(Player player, IEntitySource source, Vector2 mouseWorld, int damage, float knockback, bool stealthEnhanced)
        {
            List<Projectile> feathers = new();
            int type = ModContent.ProjectileType<MalachiteRightFeather>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == player.whoAmI &&
                    projectile.type == type &&
                    (MalachiteRightFeatherState)(int)projectile.ai[0] == MalachiteRightFeatherState.Stored)
                {
                    feathers.Add(projectile);
                }
            }

            if (feathers.Count <= 0)
                return false;

            feathers.Sort((left, right) => left.whoAmI.CompareTo(right.whoAmI));
            for (int i = 0; i < feathers.Count; i++)
            {
                Projectile projectile = feathers[i];
                projectile.ai[0] = (float)MalachiteRightFeatherState.Released;
                projectile.ai[1] = i;
                projectile.ai[2] = (stealthEnhanced ? 100f : 0f) + feathers.Count;
                projectile.localAI[0] = -i * MalachiteBalance.RightFeatherReleaseSpacingFrames;
                projectile.localAI[1] = mouseWorld.X;
                projectile.localAI[2] = mouseWorld.Y;
                projectile.damage = Math.Max(1, (int)(damage * 1.85f * (stealthEnhanced ? 1.24f : 0.98f)));
                projectile.knockBack = knockback;
                projectile.friendly = false;
                projectile.tileCollide = false;
                projectile.penetrate = -1;
                projectile.localNPCHitCooldown = 4;
                projectile.timeLeft = 180 + i * MalachiteBalance.RightFeatherReleaseSpacingFrames;
                projectile.Calamity().stealthStrike = stealthEnhanced;
                projectile.netUpdate = true;
            }

            return true;
        }

        private void AIStored(Player owner)
        {
            Projectile.timeLeft = 2;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.velocity = Vector2.Zero;

            Projectile.penetrate = -1;
            Projectile.localNPCHitCooldown = 15;

            int total = State == MalachiteRightFeatherState.Released
                ? Math.Max(1, ReleasedTotal)
                : Math.Max(1, CountStoredRightFeathers(owner));
            int order = State == MalachiteRightFeatherState.Released
                ? Utils.Clamp((int)Projectile.ai[1], 0, total - 1)
                : GetStoredOrder(owner);
            float progress = total <= 1 ? 0.5f : order / (float)(total - 1);
            float centered = progress - 0.5f;
            float open = Utils.GetLerpValue(0f, 18f, Math.Max(0f, Timer), true);
            open = open * open * (3f - 2f * open);
            float spread = MathHelper.Lerp(-0.82f, 0.82f, progress);
            float layer = order % 2 == 0 ? 0f : 12f;
            Vector2 behind = new Vector2(-owner.direction, -0.18f).SafeNormalize(-Vector2.UnitX * owner.direction);
            Vector2 targetOffset =
                behind.RotatedBy(spread * owner.direction) * (48f + MathF.Sin(progress * MathHelper.Pi) * 34f + layer) -
                Vector2.UnitY * (8f + MathF.Sin(progress * MathHelper.Pi) * 20f) +
                Vector2.UnitX * owner.direction * centered * -10f;
            Vector2 target = owner.MountedCenter + targetOffset * open;

            Projectile.Center =
            Vector2.Lerp(
                Projectile.Center,
                target,
                0.10f)
            .MoveTowards(
                target,
                10f);

            float targetAngle =
            targetOffset.SafeNormalize(
                -Vector2.UnitX * owner.direction
            ).ToRotation();

            if (hoverAngle == 0f)
                hoverAngle = targetAngle;

            hoverAngle = Utils.AngleLerp(
                hoverAngle,
                targetAngle,
                0.12f);

            Projectile.rotation =
            hoverAngle + MathHelper.PiOver2;

            Projectile.scale = MathHelper.Lerp(0.35f, 0.65f, open);
            Projectile.alpha = (int)MathHelper.Lerp(140f, 20f, open);
            Lighting.AddLight(Projectile.Center, 0.06f, 0.34f, 0.12f);
        }

        private int GetStoredOrder(Player owner)
        {
            int order = 0;
            int type = ModContent.ProjectileType<MalachiteRightFeather>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != owner.whoAmI ||
                    projectile.type != type ||
                    (MalachiteRightFeatherState)(int)projectile.ai[0] != MalachiteRightFeatherState.Stored)
                {
                    continue;
                }

                if (projectile.whoAmI < Projectile.whoAmI)
                    order++;
            }

            return order;
        }

        private void Launch(Player owner)
        {
            Vector2 target = QueuedTarget == Vector2.Zero ? Main.MouseWorld : QueuedTarget;
            Vector2 direction = (target - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);
            // 5 degrees is approximately 0.087266 radians. Spread range is -2.5 to 2.5 degrees (-0.043633 to 0.043633 radians).
            direction = direction.RotatedBy(Main.rand.NextFloat(-0.043633f, 0.043633f));
            float speed = Main.rand.NextFloat(32f, 42f) * owner.GetModPlayer<MalachiteFeatherPlayer>().MalachiteProjectileVelocityMultiplier;

            Projectile.velocity = direction * speed + owner.velocity * 0.08f;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 150;
            Projectile.scale = StealthEnhanced ? 0.78f : 0.65f;
            Projectile.alpha = 0;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            for (int i = 0; i < Projectile.oldRot.Length; i++)
            {
                Projectile.oldRot[i] = Projectile.rotation;
                Projectile.oldPos[i] = Projectile.position;
            }

            Projectile.netUpdate = true;

            if ((int)Projectile.ai[1] % 4 == 0)
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.42f, MaxInstances = 5 }, Projectile.Center);
        }

        private void SpawnFlightDust()
        {
            SpawnPlagueFlightVisuals();

            if (Main.rand.NextBool(2))
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * 8f + Main.rand.NextVector2Circular(3f, 3f),
                DustID.Terra,
                -direction * Main.rand.NextFloat(0.4f, 1.4f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                80,
                StealthEnhanced ? new Color(190, 255, 110) : new Color(80, 255, 135),
                Main.rand.NextFloat(0.72f, 1.05f));
            dust.noGravity = true;
        }

        private void ApplyPeacockDartGravity(Player owner)
        {
            if (!owner.active || !owner.GetModPlayer<PeacockDartPlayer>().PeacockDartEquipped)
                return;

            Projectile.velocity.Y += 0.16f;
            Projectile.velocity.X *= 0.996f;
        }

        private void SpawnPlagueFlightVisuals()
        {
            Lighting.AddLight(Projectile.Center, 0.07f, 0.15f, 0.01f);

            if (Main.rand.NextBool(3))
            {
                Dust toxicDust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(30) ? DustID.Terra : DustID.GreenTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.015f, 0.04f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    120,
                    default,
                    Main.rand.NextBool(30) ? Main.rand.NextFloat(0.9f, 1.05f) : Main.rand.NextFloat(0.3f, 0.42f));
                toxicDust.noGravity = true;
            }

            if (Timer % 18f != 0f)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                (Main.rand.NextBool(3) ? Color.LimeGreen : Color.Green) * 0.42f,
                Vector2.One,
                Projectile.rotation,
                Main.rand.NextFloat(0.045f, 0.09f),
                0f,
                15));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 8 * 60);
            target.AddBuff(ModContent.BuffType<Plague>(), 4 * 60);

            if (!StealthEnhanced || Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<MalachiteRightFeatherExplosion>(),
                Math.Max(1, (int)(Projectile.damage * 0.62f)),
                0f,
                Projectile.owner);

            int shardDamage = Math.Max(1, (int)(Projectile.damage * 0.32f));
            Vector2 baseDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 4; i++)
            {
                Vector2 shardVelocity = baseDirection.RotatedBy(MathHelper.Lerp(-0.62f, 0.62f, i / 3f) + Main.rand.NextFloat(-0.14f, 0.14f)) * Main.rand.NextFloat(8.5f, 13f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center + Main.rand.NextVector2Circular(12f, 12f),
                    shardVelocity,
                    ModContent.ProjectileType<MalachiteRightNovaShard>(),
                    shardDamage,
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.active && owner.GetModPlayer<PeacockDartPlayer>().PeacockDartEquipped)
                modifiers.SourceDamage *= 1.3f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            if (State == MalachiteRightFeatherState.Released && Timer > 0f)
            {
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            }

            if (State == MalachiteRightFeatherState.Stored || Timer <= 0f)
                DrawGreenBacklight(texture, frame, origin, drawPosition, effects);

            Color drawColor = Color.Lerp(lightColor, StealthEnhanced ? new Color(210, 255, 115) : new Color(90, 255, 135), 0.78f) * Projectile.Opacity;
            Main.EntitySpriteDraw(texture, drawPosition, frame, drawColor, Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }

        private void DrawGreenBacklight(Texture2D texture, Rectangle frame, Vector2 origin, Vector2 drawPosition, SpriteEffects effects)
        {
            float pulse = 0.86f + 0.14f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6.4f + Projectile.identity);
            float chargeOffset = 4.5f * Projectile.scale * pulse;
            for (int i = 0; i < 16; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * chargeOffset;
                Color backlightColor = i % 2 == 0 ? new Color(70, 255, 125, 0) * 0.52f : new Color(205, 255, 160, 0) * 0.34f;
                Main.EntitySpriteDraw(texture, drawPosition + drawOffset, frame, backlightColor * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale * 1.03f, effects);
            }
        }
    }

   

  
}
