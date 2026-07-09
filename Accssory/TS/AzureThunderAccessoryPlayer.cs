using System;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.ZhuangFangYiPet;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class AzureThunderAccessoryPlayer : ModPlayer
    {
        public bool GuZhouEquipped;
        public bool YiGanYiYingEquipped;
        public bool QianDingWanDingEquipped;
        public bool FengYunZhiBianEquipped;
        public bool OverclockEquipped;
        public bool WorldSplitterEquipped;

        private int guZhouDamageTimer;
        private int guZhouConsumedCharge;
        private int yiGanDamageTimer;
        private float yiGanDamageBonus;
        private int worldSplitterDamageTimer;
        private int worldSplitterShieldTimer;
        private float worldSplitterShieldHitPoints;
        private bool worldSplitterShieldHitThisFrame;

        public bool AnyTSEquipped => GuZhouEquipped ||
            YiGanYiYingEquipped ||
            QianDingWanDingEquipped ||
            FengYunZhiBianEquipped ||
            OverclockEquipped ||
            WorldSplitterEquipped;

        public bool WorldSplitterShieldActive => worldSplitterShieldTimer > 0 && worldSplitterShieldHitPoints > 0f && !Player.dead;
        public int WorldSplitterShieldCurrent => Math.Max(0, (int)MathF.Ceiling(worldSplitterShieldHitPoints));
        public int WorldSplitterShieldMax => Math.Max(1, (int)MathF.Ceiling(Player.statLifeMax2 * 0.2f));

        public override void ResetEffects()
        {
            GuZhouEquipped = false;
            YiGanYiYingEquipped = false;
            QianDingWanDingEquipped = false;
            FengYunZhiBianEquipped = false;
            OverclockEquipped = false;
            WorldSplitterEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            bool holdingAzureThunder = Player.HeldItem?.type == ModContent.ItemType<AzureThunder>();

            if (GuZhouEquipped)
            {
                Player.GetDamage(DamageClass.Magic) += 0.1f;
                if (holdingAzureThunder)
                    Player.GetDamage(DamageClass.Magic) += 0.05f;
            }

            if (YiGanYiYingEquipped)
            {
                Player.statManaMax2 += 50;
                Player.manaCost -= 0.15f;
                Player.GetDamage(DamageClass.Magic) += 0.09f;
                Player.GetCritChance(DamageClass.Magic) += 9f;
            }

            if (QianDingWanDingEquipped)
            {
                Player.statManaMax2 += 120;
                Player.manaCost -= 0.20f;
            }

            if (FengYunZhiBianEquipped)
            {
                if (holdingAzureThunder)
                    Player.GetDamage(DamageClass.Magic) += 0.18f;

                if (holdingAzureThunder && Player.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>()))
                {
                    Player.GetDamage(DamageClass.Magic) *= 1.09f;
                    Player.GetArmorPenetration(DamageClass.Magic) += 36f;
                }
            }

            if (WorldSplitterEquipped)
            {
                Player.GetCritChance(DamageClass.Generic) += 8f;
                if (holdingAzureThunder)
                    Player.GetCritChance(DamageClass.Magic) += 5f;
            }

            if (worldSplitterDamageTimer > 0)
                Player.GetDamage(DamageClass.Generic) += 0.09f;

            UpdateTimedDamageBuffs();
        }

        public override void PostUpdate()
        {
            if (worldSplitterDamageTimer > 0)
                worldSplitterDamageTimer--;

            if (worldSplitterShieldHitThisFrame)
                worldSplitterShieldHitThisFrame = false;

            if (Player.dead)
            {
                worldSplitterDamageTimer = 0;
                worldSplitterShieldTimer = 0;
                worldSplitterShieldHitPoints = 0f;
                return;
            }

            if (worldSplitterShieldTimer > 0)
            {
                worldSplitterShieldTimer--;
                worldSplitterShieldHitPoints = Math.Min(worldSplitterShieldHitPoints, WorldSplitterShieldMax);
                if (Main.rand.NextBool(worldSplitterShieldHitThisFrame ? 1 : 8))
                    SpawnWorldSplitterShieldDust();
            }
            else
                worldSplitterShieldHitPoints = 0f;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (WorldSplitterShieldActive)
                modifiers.ModifyHurtInfo += ApplyWorldSplitterShield;
        }

        public void OnConsumeThunderCharge(int consumedCharge, bool harmonyActive, int activeSwordCount, NPC lockedTarget)
        {
            if (consumedCharge <= 0 && !harmonyActive)
                return;

            if (GuZhouEquipped && consumedCharge > 0)
            {
                guZhouConsumedCharge = consumedCharge;
                guZhouDamageTimer = 20 * 60;
            }

            if (YiGanYiYingEquipped)
            {
                yiGanDamageBonus = activeSwordCount * (harmonyActive ? 0.03f : 0.02f);
                yiGanDamageTimer = (harmonyActive ? 15 : 5) * 60;
            }

            if (GuZhouEquipped && harmonyActive && lockedTarget != null && CanApplyGuZhouSlow(lockedTarget))
                lockedTarget.AddBuff(ModContent.BuffType<AzureThunderGuZhouSlowDebuff>(), 5 * 60);
        }

        public static void ApplyAzureThunderAccessoryOnHit(Projectile projectile, NPC target)
        {
            if (!Main.player.IndexInRange(projectile.owner))
                return;

            Player owner = Main.player[projectile.owner];
            if (owner.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>()) &&
                owner.GetModPlayer<AzureThunderAccessoryPlayer>().FengYunZhiBianEquipped)
                target.AddBuff(ModContent.BuffType<AzureThunderQingTingDebuff>(), 120);
        }

        public static float GetGroundSwordEffectRadius(Player player)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().QianDingWanDingEquipped ? 75f * 16f : 50f * 16f;
        }

        public static int GetAutoGroundSwordInterval(Player player)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().QianDingWanDingEquipped ? 6 * 60 : AzureThunderPlayer.AutoGroundSwordInterval;
        }

        public static int GetRightClickLightningEnergyGain(Player player)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().QianDingWanDingEquipped ? 7 : 6;
        }

        public static int GetHarmonyDuration(Player player)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().FengYunZhiBianEquipped ? 30 * 60 : AzureThunderPlayer.HarmonyDuration;
        }

        public static int GetZhuangFangYiStrongAttackCooldown(Player player, int defaultCooldown)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().OverclockEquipped ? 7 * 60 : defaultCooldown;
        }

        public static bool ShouldOverclockElectricDamage(Player player, NPC target)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().OverclockEquipped &&
                target != null &&
                target.active &&
                AzureThunderPlayer.CountElectroDebuffs(target) > 0;
        }

        public static bool ShouldReduceAzureThunderRightClickDamage(Player player)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().OverclockEquipped;
        }

        public static void TryReleaseWorldSplitter(Player owner, Vector2 source, NPC target, Vector2 fallbackFocus, bool strongAttack)
        {
            AzureThunderAccessoryPlayer accessoryPlayer = owner.GetModPlayer<AzureThunderAccessoryPlayer>();
            if (!accessoryPlayer.WorldSplitterEquipped || Main.myPlayer != owner.whoAmI)
                return;

            NPC lockedTarget = target;
            if (lockedTarget == null || !lockedTarget.active || !lockedTarget.CanBeChasedBy())
                lockedTarget = ZhuangFangYiPetPlayer.FindNearestPetTarget(fallbackFocus, 1200f, requireElectricDebuff: false);

            if (lockedTarget == null)
                return;

            Vector2 direction = (lockedTarget.Center - source).SafeNormalize(Vector2.UnitX * owner.direction);
            Vector2 spawnPosition = lockedTarget.Center - direction * 260f - Vector2.UnitY * 10f;
            int damage = Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 5.4f));
            Projectile.NewProjectile(
                owner.GetSource_FromThis(),
                spawnPosition,
                direction * 31f,
                ModContent.ProjectileType<WorldSplitterMifu>(),
                damage,
                owner.GetWeaponKnockback(owner.HeldItem) * 0.8f,
                owner.whoAmI,
                lockedTarget.whoAmI);

            accessoryPlayer.ActivateWorldSplitterShield();
            if (strongAttack)
                accessoryPlayer.worldSplitterDamageTimer = 10 * 60;
        }

        private void ActivateWorldSplitterShield()
        {
            worldSplitterShieldTimer = 10 * 60;
            worldSplitterShieldHitPoints = WorldSplitterShieldMax;
            worldSplitterShieldHitThisFrame = true;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.62f, Pitch = 0.28f }, Player.Center);
        }

        public static bool ShouldGroundSwordFollowPlayer(Projectile projectile, out int followSlot)
        {
            followSlot = 0;
            if (!Main.player.IndexInRange(projectile.owner))
                return false;

            Player owner = Main.player[projectile.owner];
            if (!owner.GetModPlayer<AzureThunderAccessoryPlayer>().QianDingWanDingEquipped)
                return false;

            int groundSwordType = ModContent.ProjectileType<AzureThunderGroundSword>();
            int slot = 0;
            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (!other.active || other.owner != projectile.owner || other.type != groundSwordType)
                    continue;

                if (other.whoAmI == projectile.whoAmI)
                {
                    followSlot = slot;
                    return slot < 3;
                }

                slot++;
            }

            return false;
        }

        private static bool CanApplyGuZhouSlow(NPC target)
        {
            return target.active && target.realLife < 0 && target.aiStyle != NPCAIStyleID.Worm;
        }

        private void UpdateTimedDamageBuffs()
        {
            if (guZhouDamageTimer > 0)
            {
                Player.GetDamage(DamageClass.Magic) += guZhouConsumedCharge * 0.05f;
                Player.AddBuff(ModContent.BuffType<AzureThunderGuZhouDamageBuff>(), guZhouDamageTimer);
                guZhouDamageTimer--;
            }
            else
                guZhouConsumedCharge = 0;

            if (yiGanDamageTimer > 0)
            {
                Player.GetDamage(DamageClass.Magic) += yiGanDamageBonus;
                Player.AddBuff(ModContent.BuffType<AzureThunderYiGanDamageBuff>(), yiGanDamageTimer);
                yiGanDamageTimer--;
            }
            else
                yiGanDamageBonus = 0f;
        }

        private void ApplyWorldSplitterShield(ref Player.HurtInfo info)
        {
            if (!WorldSplitterShieldActive || info.Damage <= 0)
                return;

            int absorbedDamage = Math.Min(info.Damage, WorldSplitterShieldCurrent);
            if (absorbedDamage <= 0)
                return;

            worldSplitterShieldHitPoints = Math.Max(0f, worldSplitterShieldHitPoints - absorbedDamage);
            info.Damage = Math.Max(0, info.Damage - absorbedDamage);
            worldSplitterShieldHitThisFrame = true;

            Player.GiveIFrames(info.CooldownCounter, Player.ComputeHitIFrames(info), true);
            if (info.Damage <= 0)
                Player.Calamity().freeDodgeFromShieldAbsorption = true;

            SoundEngine.PlaySound(
                worldSplitterShieldHitPoints <= 0f ? SoundID.Item122 with { Volume = 0.68f, Pitch = -0.28f } : SoundID.Item29 with { Volume = 0.46f, Pitch = 0.44f },
                Player.Center);
        }

        private void SpawnWorldSplitterShieldDust()
        {
            float radiusX = Player.width * 0.72f + 28f;
            float radiusY = Player.height * 0.72f + 22f;
            Vector2 offset = Main.rand.NextVector2CircularEdge(radiusX, radiusY);
            Vector2 velocity = offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.5f, 1.8f);
            Dust dust = Dust.NewDustPerfect(
                Player.Center + offset,
                DustID.FireworksRGB,
                velocity,
                0,
                Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                Main.rand.NextFloat(0.85f, 1.35f));
            dust.noGravity = true;
        }
    }

    internal sealed class AzureThunderSlowGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void PostAI(NPC npc)
        {
            if (npc.HasBuff(ModContent.BuffType<AzureThunderGuZhouSlowDebuff>()) && npc.realLife < 0 && npc.aiStyle != NPCAIStyleID.Worm)
                npc.velocity *= 0.85f;
        }
    }

    internal sealed class AzureThunderAccessoryGlobalProjectile : GlobalProjectile
    {
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!Main.player.IndexInRange(projectile.owner))
                return;

            Player owner = Main.player[projectile.owner];
            if (AzureThunderAccessoryPlayer.ShouldOverclockElectricDamage(owner, target))
                modifiers.FinalDamage *= 2f;
        }
    }

    internal sealed class AzureThunderAccessoryGlobalItem : GlobalItem
    {
        public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (AzureThunderAccessoryPlayer.ShouldOverclockElectricDamage(player, target))
                modifiers.FinalDamage *= 2f;
        }
    }

    internal sealed class WorldSplitterMifu : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 32;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityLegendsComeBack/Accssory/TS/图片放这里/弭弗";

        private int TargetIndex => (int)Projectile.ai[0];
        private float Fade => Utils.GetLerpValue(0f, 4f, Lifetime - Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 7f, Projectile.timeLeft, true);

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, AzureThunderColors.Azure.ToVector3() * 0.55f);

            if (Projectile.timeLeft == Lifetime - 1)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.78f, Pitch = 0.1f }, Projectile.Center);
                SpawnDashBurst(Projectile.Center, 18, 4.2f);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 12f),
                    DustID.FireworksRGB,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.2f, 4.6f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.75f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center - direction * 70f,
                Projectile.Center + direction * 110f,
                34f,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Crumbling>(), 300);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            SpawnDashBurst(target.Center, target.whoAmI == TargetIndex ? 16 : 9, target.whoAmI == TargetIndex ? 3.6f : 2.1f);
            SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.52f, Pitch = 0.22f }, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color afterimageColor = AzureThunderColors.Azure with { A = 0 };

            Main.spriteBatch.EnterShaderRegion(Microsoft.Xna.Framework.Graphics.BlendState.Additive);
            for (int i = 1; i <= 5; i++)
            {
                Vector2 offset = -Projectile.velocity.SafeNormalize(Vector2.UnitX) * i * 18f;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, afterimageColor * Fade * (0.28f / i), Projectile.rotation, origin, Projectile.scale * (1f + i * 0.035f), SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * Fade, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }

        private static void SpawnDashBurst(Vector2 center, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    DustID.FireworksRGB,
                    Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(speed * 0.35f, speed),
                    0,
                    Main.rand.NextBool(3) ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.75f, 1.45f));
                dust.noGravity = true;
            }
        }
    }
}
