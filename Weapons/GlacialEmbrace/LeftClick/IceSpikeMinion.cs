using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.EXSkill;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick
{
    public class IceSpikeMinion : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Summon/GlacialEmbrace";

        public bool embedded;
        public int embedNPCIndex = -1;
        public Vector2 embedOffset = Vector2.Zero;
        public bool flying;
        public bool isThrusting;
        public int pierceTimer;

        public bool hibernating;
        public int hibernateTimer;
        public bool smashing;
        public float smashProgress;

        private bool smashWaveFired;
        private int spikeIndex;
        private int orbitTimer;
        private Vector2 embedDirection = Vector2.UnitX;
        private Vector2 strikeStartOffset = Vector2.Zero;
        private Vector2 strikeTargetOffset = Vector2.Zero;
        private Vector2 strikeWaveOffset = Vector2.Zero;

        private const float SlashInnerRadius = 90f;
        private const float SlashOuterRadius = 150f;
        private const float PierceOrbitRadius = 105f;
        private const float StrikeOrbitRadius = 95f;
        private const int PierceLaunchDelay = 150;
        private const int PierceMaxFlightTime = 70;
        private const int TriggeredPierceTime = 24;
        private const int StrikeHibernateTime = 240;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 48;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 45;
        }

        public override bool ShouldUpdatePosition() => flying || smashing;

        public bool IsCirclingPlayer()
        {
            return !embedded && !flying && !smashing && !IsUltimateActive();
        }

        public bool IsLargeSlashSpike => Projectile.ai[1] == 1f;

        public void SetSpikeIndex(int index)
        {
            spikeIndex = index;
        }

        public void ResetForModeChange(int newMode)
        {
            embedded = false;
            embedNPCIndex = -1;
            embedOffset = Vector2.Zero;
            flying = false;
            isThrusting = false;
            pierceTimer = 0;
            smashing = false;
            smashProgress = 0f;
            smashWaveFired = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.localNPCHitCooldown = 45;
            orbitTimer = 0;

            if (newMode == 0)
            {
                Projectile.minionSlots = 0.5f;
            }
            else
            {
                Projectile.ai[1] = 0f;
                Projectile.minionSlots = 1f;
            }

            Projectile.netUpdate = true;
        }

        public override bool? CanDamage()
        {
            if (hibernating || embedded)
                return false;

            return null;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            if (!player.active || player.dead || !modPlayer.GlacialEmbraceMinion)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            if (IsUltimateActive())
            {
                HandleUltimateAssembly(player);
                return;
            }

            if (hibernating)
                UpdateHibernate();

            if (smashing)
            {
                HandleSmashAnimation(player);
                return;
            }

            switch (modPlayer.CurrentMode)
            {
                case 0:
                    if (embedded || flying)
                        ResetForModeChange(0);
                    HandleSlashOrbit(player, modPlayer);
                    break;
                case 1:
                    HandlePierceMode(player, modPlayer);
                    break;
                default:
                    if (embedded || flying)
                        ResetForModeChange(2);
                    HandleStrikeOrbit(player, modPlayer);
                    break;
            }
        }

        private bool IsUltimateActive()
        {
            Player player = Main.player[Projectile.owner];
            return player.ownedProjectileCounts[ModContent.ProjectileType<GlacialDrillProj>()] > 0;
        }

        private NPC FindTarget(Player player, float range = 950f)
        {
            return CalamityUtils.MinionHoming(Projectile.Center, range, player);
        }

        private void UpdateHibernate()
        {
            Projectile.friendly = false;
            hibernateTimer--;
            if (hibernateTimer > 0)
                return;

            hibernating = false;
            Projectile.friendly = true;
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Ice);
                d.velocity = Main.rand.NextVector2Circular(3f, 3f);
                d.noGravity = true;
            }
        }

        private void HandleSlashOrbit(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 0.5f;
            Projectile.friendly = !hibernating;
            Projectile.velocity = Vector2.Zero;

            bool large = IsLargeSlashSpike;
            float speedBoost = modPlayer.GlacialDivinityTimer > 0 ? 1.1f : 1f;
            float angularSpeed = MathHelper.ToRadians((large ? 2.65f : 4.35f) * speedBoost);
            Projectile.ai[0] -= angularSpeed;

            float radius = large ? SlashOuterRadius : SlashInnerRadius;
            Vector2 offset = Projectile.ai[0].ToRotationVector2() * radius;
            Projectile.Center = player.Center + offset + Vector2.UnitY * player.gfxOffY;
            Projectile.rotation = Projectile.ai[0] + MathHelper.PiOver2;

            int cycleFrames = Math.Max(30, (int)(MathHelper.TwoPi / angularSpeed));
            Projectile.localNPCHitCooldown = large ? cycleFrames + 8 : cycleFrames;

            if (Main.rand.NextBool(large ? 3 : 5))
            {
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, large ? DustID.Electric : DustID.Frost);
                d.velocity = (Projectile.ai[0] + MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(0.8f, 2.2f);
                d.scale = large ? 1.05f : 0.75f;
                d.noGravity = true;
            }
        }

        private void HandlePierceMode(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 1f;

            if (embedded)
            {
                HandleEmbedded(player);
                return;
            }

            if (flying)
            {
                HandleFlyingPierce(player);
                return;
            }

            HandlePierceOrbit(player, modPlayer);
        }

        private void HandlePierceOrbit(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.friendly = !hibernating;
            Projectile.velocity = Vector2.Zero;
            orbitTimer++;

            float speed = MathHelper.ToRadians(3.15f * (modPlayer.GlacialDivinityTimer > 0 ? 1.1f : 1f));
            Projectile.ai[0] -= speed;
            Projectile.Center = player.Center + Projectile.ai[0].ToRotationVector2() * PierceOrbitRadius + Vector2.UnitY * player.gfxOffY;
            Projectile.rotation = Projectile.ai[0] + MathHelper.PiOver2;
            Projectile.localNPCHitCooldown = 40;

            int launchDelay = PierceLaunchDelay + spikeIndex * 9;
            if (orbitTimer < launchDelay || hibernating)
                return;

            NPC target = FindTarget(player, 1050f);
            if (target == null)
                return;

            LaunchAtTarget(target);
        }

        private void LaunchAtTarget(NPC target)
        {
            flying = true;
            isThrusting = false;
            pierceTimer = 0;
            orbitTimer = 0;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 18f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.localNPCHitCooldown = 14;
            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.15f, Volume = 0.55f }, Projectile.Center);
            Projectile.netUpdate = true;
        }

        private void HandleEmbedded(Player player)
        {
            if (!Main.npc.IndexInRange(embedNPCIndex))
            {
                ResetForModeChange(1);
                return;
            }

            NPC target = Main.npc[embedNPCIndex];
            if (!target.active || target.life <= 0 || !target.CanBeChasedBy(Projectile, false))
            {
                ResetForModeChange(1);
                return;
            }

            Projectile.Center = target.Center + embedOffset.RotatedBy(target.rotation);
            Projectile.rotation = embedDirection.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;
            Projectile.timeLeft = 2;

            if (Main.rand.NextBool(10))
            {
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Frost);
                d.velocity = embedDirection.RotatedByRandom(0.25f) * -1.4f;
                d.noGravity = true;
            }
        }

        private void HandleFlyingPierce(Player player)
        {
            Projectile.friendly = !hibernating;
            Projectile.penetrate = -1;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.localNPCHitCooldown = isThrusting ? 8 : 14;

            pierceTimer++;
            int maxTime = isThrusting ? TriggeredPierceTime : PierceMaxFlightTime;
            if (pierceTimer >= maxTime)
                ReturnToOrbit(player);

            for (int i = 0; i < (isThrusting ? 3 : 1); i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, isThrusting ? DustID.Electric : DustID.Ice);
                d.velocity = -Projectile.velocity * Main.rand.NextFloat(0.06f, 0.13f);
                d.scale = isThrusting ? Main.rand.NextFloat(1f, 1.4f) : Main.rand.NextFloat(0.7f, 1f);
                d.noGravity = true;
            }
        }

        private void HandleStrikeOrbit(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 1f;
            Projectile.friendly = !hibernating;
            Projectile.velocity = Vector2.Zero;

            float speed = MathHelper.ToRadians(2.4f * (modPlayer.GlacialDivinityTimer > 0 ? 1.08f : 1f));
            if (!hibernating && !modPlayer.StrikeAligned)
                Projectile.ai[0] -= speed;

            Projectile.Center = player.Center + Projectile.ai[0].ToRotationVector2() * StrikeOrbitRadius + Vector2.UnitY * player.gfxOffY;
            Projectile.rotation = Projectile.ai[0] - MathHelper.PiOver2;
            Projectile.localNPCHitCooldown = 55;
        }

        private void ReturnToOrbit(Player player)
        {
            embedded = false;
            flying = false;
            isThrusting = false;
            pierceTimer = 0;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = true;
            Projectile.Center = Vector2.Lerp(Projectile.Center, player.Center + Projectile.ai[0].ToRotationVector2() * PierceOrbitRadius, 0.65f);
            Projectile.netUpdate = true;
        }

        private bool TryEmbed(NPC target, Player player)
        {
            if (CountEmbeddedSpikes(target, player) >= 6)
                return false;

            embedded = true;
            flying = false;
            isThrusting = false;
            pierceTimer = 0;
            embedNPCIndex = target.whoAmI;
            embedOffset = Projectile.Center - target.Center;
            embedDirection = Projectile.velocity.SafeNormalize((target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY));
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.35f, Volume = 0.75f }, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Ice);
                d.velocity = Main.rand.NextVector2Circular(4f, 4f);
                d.noGravity = true;
            }
            return true;
        }

        private static int CountEmbeddedSpikes(NPC target, Player player)
        {
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.type != spikeType || p.owner != player.whoAmI)
                    continue;

                if (p.ModProjectile is IceSpikeMinion spike && spike.embedded && spike.embedNPCIndex == target.whoAmI)
                    count++;
            }
            return count;
        }

        public void PierceThrust(Vector2 direction)
        {
            if (!embedded)
                return;

            Vector2 storedDirection = embedDirection.SafeNormalize(-embedOffset.SafeNormalize(Vector2.UnitY));
            Vector2 thrustDirection = direction.SafeNormalize(storedDirection);

            embedded = false;
            flying = true;
            isThrusting = true;
            pierceTimer = 0;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.velocity = thrustDirection * 24f;
            Projectile.localNPCHitCooldown = 8;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.55f, Volume = 0.78f }, Projectile.Center);
        }

        public void AlignForStrike(Vector2 centerLine, Vector2 orthoVec, int index, int totalCount)
        {
            if (hibernating || smashing)
                return;

            embedded = false;
            flying = false;
            isThrusting = false;
            Projectile.velocity = Vector2.Zero;

            bool upperRow = index % 2 == 0;
            float rowOffset = upperRow ? 82f : -82f;
            float normalized = index / 2f - Math.Max(0f, (totalCount - 1) / 4f);
            float lineOffset = normalized * 46f;

            Vector2 startOffset = centerLine * lineOffset + orthoVec * rowOffset;
            Vector2 targetOffset = centerLine * lineOffset - orthoVec * rowOffset;
            Vector2 targetPosition = Main.player[Projectile.owner].Center + startOffset;

            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPosition, 0.28f);
            Projectile.rotation = (targetOffset - startOffset).ToRotation() + MathHelper.PiOver2;

            strikeStartOffset = startOffset;
            strikeTargetOffset = targetOffset;
            strikeWaveOffset = centerLine * lineOffset;
        }

        public void ExecuteSmash()
        {
            if (hibernating || smashing)
                return;

            smashing = true;
            smashProgress = 0f;
            smashWaveFired = false;
            Projectile.velocity = Vector2.Zero;
            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.4f, Volume = 0.55f }, Projectile.Center);
        }

        private void HandleSmashAnimation(Player player)
        {
            Projectile.friendly = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.timeLeft = 2;

            smashProgress += 0.085f;
            Vector2 offset = Vector2.Lerp(strikeStartOffset, strikeTargetOffset, MathHelper.Clamp(smashProgress, 0f, 1f));
            Projectile.Center = player.Center + offset;
            Projectile.rotation = (strikeTargetOffset - strikeStartOffset).ToRotation() + MathHelper.PiOver2;

            if (!smashWaveFired && smashProgress >= 0.5f)
            {
                smashWaveFired = true;
                SpawnShockwave(player, player.Center + strikeWaveOffset, 2.05f, 6f, 2.8f);
            }

            if (smashProgress < 1f)
                return;

            smashing = false;
            hibernating = true;
            hibernateTimer = StrikeHibernateTime;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }

        public void ForceShockwaveAndHibernate(Player player)
        {
            if (hibernating)
                return;

            embedded = false;
            flying = false;
            isThrusting = false;
            smashing = false;
            SpawnShockwave(player, Projectile.Center, 1.55f, 5.5f, 2.4f);
            hibernating = true;
            hibernateTimer = StrikeHibernateTime;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }

        private void SpawnShockwave(Player player, Vector2 position, float damageMultiplier, float knockback, float shake)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                var source = player.GetSource_ItemUse(player.HeldItem);
                int wave = Projectile.NewProjectile(source, position, Vector2.Zero, ProjectileID.SolarWhipSwordExplosion,
                    (int)(Projectile.damage * damageMultiplier), knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(wave))
                {
                    Main.projectile[wave].DamageType = DamageClass.Summon;
                    Main.projectile[wave].friendly = true;
                    Main.projectile[wave].hostile = false;
                }
            }

            for (int i = 0; i < 28; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(7f, 7f);
                Dust d = Dust.NewDustDirect(position, 0, 0, Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric, vel.X, vel.Y);
                d.scale = Main.rand.NextFloat(1.2f, 2.1f);
                d.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f, Pitch = 0.15f }, position);
            player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, shake);
        }

        private void HandleUltimateAssembly(Player player)
        {
            Projectile.minionSlots = 0f;
            Projectile.friendly = true;

            int drillType = ModContent.ProjectileType<GlacialDrillProj>();
            Projectile drill = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == drillType)
                {
                    drill = p;
                    break;
                }
            }

            if (drill == null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            if (modPlayer.CurrentMode == 0 && IsCirclingPlayer())
            {
                float radius = IsLargeSlashSpike ? 32f : 20f;
                Vector2 targetSize = new Vector2(targetHitbox.Width, targetHitbox.Height);
                return Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2()) <= radius + targetSize.Length() * 0.34f;
            }

            if (isThrusting && flying)
            {
                Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 start = Projectile.Center - dir * 24f;
                Vector2 end = Projectile.Center + dir * 82f;
                float collisionPoint = 0f;
                return Collision.CheckAABBvLineCollision(
                    new Vector2(targetHitbox.X, targetHitbox.Y),
                    new Vector2(targetHitbox.Width, targetHitbox.Height),
                    start,
                    end,
                    24f,
                    ref collisionPoint);
            }

            return base.Colliding(projHitbox, targetHitbox);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            if (modPlayer.CurrentMode == 0 && IsCirclingPlayer())
                modifiers.SourceDamage *= IsLargeSlashSpike ? 1.32f : 0.78f;
            else if (modPlayer.CurrentMode == 1 && isThrusting)
                modifiers.SourceDamage *= 1.15f;
            else if (modPlayer.CurrentMode == 2 && smashing)
                modifiers.SourceDamage *= 1.45f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            target.AddBuff(BuffID.Frostburn2, 300);

            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();
            modPlayer.UltimateCharge = Math.Min(GlacialEmbracePlayer.MaxUltimateCharge, modPlayer.UltimateCharge + (isThrusting ? 2 : 1));

            if (modPlayer.CurrentMode == 1 && flying && !isThrusting)
            {
                if (!TryEmbed(target, player))
                    ReturnToOrbit(player);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(embedded);
            writer.Write(embedNPCIndex);
            writer.Write(embedOffset.X);
            writer.Write(embedOffset.Y);
            writer.Write(embedDirection.X);
            writer.Write(embedDirection.Y);
            writer.Write(flying);
            writer.Write(isThrusting);
            writer.Write(pierceTimer);
            writer.Write(hibernating);
            writer.Write(hibernateTimer);
            writer.Write(smashing);
            writer.Write(smashProgress);
            writer.Write(strikeStartOffset.X);
            writer.Write(strikeStartOffset.Y);
            writer.Write(strikeTargetOffset.X);
            writer.Write(strikeTargetOffset.Y);
            writer.Write(strikeWaveOffset.X);
            writer.Write(strikeWaveOffset.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            embedded = reader.ReadBoolean();
            embedNPCIndex = reader.ReadInt32();
            embedOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            embedDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            flying = reader.ReadBoolean();
            isThrusting = reader.ReadBoolean();
            pierceTimer = reader.ReadInt32();
            hibernating = reader.ReadBoolean();
            hibernateTimer = reader.ReadInt32();
            smashing = reader.ReadBoolean();
            smashProgress = reader.ReadSingle();
            strikeStartOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            strikeTargetOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            strikeWaveOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            if (player.active && player.ownedProjectileCounts[ModContent.ProjectileType<GlacialDrillProj>()] == 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Ice);
                    d.velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (IsUltimateActive())
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            Color color = Color.Lerp(new Color(90, 215, 255), Color.White, 0.25f + 0.2f * MathF.Sin(time * 6f));
            float scale = 1f;

            if (hibernating)
            {
                color = new Color(45, 95, 135) * 0.35f;
                scale = 0.85f;
            }
            else if (embedded)
            {
                color = new Color(130, 230, 255) * 0.75f;
                scale = 0.82f;
            }
            else if (IsLargeSlashSpike)
            {
                color = Color.Lerp(new Color(0, 185, 255), Color.White, 0.45f + 0.25f * MathF.Sin(time * 7f));
                scale = 1.32f;
            }
            else if (flying || smashing)
            {
                color = Color.Lerp(new Color(110, 235, 255), Color.White, 0.55f);
                scale = 1.12f;
            }
            else if (Main.player[Projectile.owner].GetModPlayer<GlacialEmbracePlayer>().CurrentMode == 0)
            {
                scale = 0.78f;
            }

            Main.spriteBatch.Draw(texture, drawCenter, null, color * 0.95f, Projectile.rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
