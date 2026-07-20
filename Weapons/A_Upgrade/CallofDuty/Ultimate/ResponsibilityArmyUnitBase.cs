using System;
using System.IO;
using CalamityMod;
using CalamityMod.Sounds;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.Visuals;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty.Ultimate
{
    internal abstract class ResponsibilityArmyUnitBase : ModNPC
    {
        internal const float AmplifierRadius = 990f;
        internal const int FadeFrames = 45;

        protected abstract string SourceTexture { get; }
        protected abstract int FrameCount { get; }
        protected abstract int Width { get; }
        protected abstract int Height { get; }
        protected abstract float LifeMultiplier { get; }
        protected abstract int ChassisDefense { get; }
        protected abstract float DamageCapRatio { get; }
        protected abstract bool Flying { get; }
        protected abstract int NativeHitDustCount { get; }
        protected abstract int NativeDeathDustCount { get; }
        protected abstract string[] NativeGoreNames { get; }

        public override string Texture => SourceTexture;

        public int OwnerIndex { get; private set; } = -1;
        public int Generation { get; private set; }
        public int SlotIndex { get; private set; }
        public int SnapshotDamage { get; private set; }
        public int BaseMaximumLife { get; private set; }
        public int BaseDefense { get; private set; }
        public int RemainingFrames { get; private set; }
        public int Age { get; private set; }
        public bool Amplified { get; private set; }
        public bool Initialized { get; private set; }
        public bool Dismissing { get; private set; }
        public int DismissTimer { get; private set; }
        private int dismissDelay;

        protected Player Owner => Main.player.IndexInRange(OwnerIndex) ? Main.player[OwnerIndex] : null;
        protected bool CanAttack => Initialized && !Dismissing && Owner != null && Owner.active && !Owner.dead;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = FrameCount;
            NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = false;
        }

        public override void SetDefaults()
        {
            NPC.width = Width;
            NPC.height = Height;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 1;
            NPC.friendly = true;
            NPC.noGravity = Flying;
            NPC.noTileCollide = Flying;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.value = 0f;
            NPC.npcSlots = 0f;
            NPC.netAlways = true;
            NPC.HitSound = CalamityMod.NPCs.NormalNPCs.WulfrumAmplifier.Hit;
            NPC.DeathSound = CommonCalamitySounds.WulfrumNPCDeathSound;
        }

        public override bool CheckActive() => false;
        public override bool? CanBeHitByItem(Player player, Item item) => false;
        public override bool? CanBeHitByProjectile(Projectile projectile) => false;
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        internal void InitializeFromOwner(Player owner, int generation, int slotIndex, int snapshotDamage, int remainingFrames)
        {
            OwnerIndex = owner.whoAmI;
            Generation = generation;
            SlotIndex = slotIndex;
            SnapshotDamage = Math.Max(1, snapshotDamage);
            RemainingFrames = Math.Max(1, remainingFrames);

            float difficulty = Main.masterMode ? 1.35f : Main.expertMode ? 1.2f : 1f;
            if (CalamityWorld.revenge)
                difficulty *= 1.1f;
            if (CalamityWorld.death)
                difficulty *= 1.1f;

            BaseMaximumLife = Math.Max(1, (int)MathF.Ceiling(owner.statLifeMax2 * LifeMultiplier * difficulty));
            BaseDefense = Math.Max(0, owner.statDefense + ChassisDefense);
            Initialized = true;
            ApplyAmplifierState(IsInsideAnyAmplifierField(NPC.Center), true);
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
        }

        public override void AI()
        {
            if (!Initialized)
            {
                NPC.velocity *= 0.9f;
                return;
            }

            NPC.timeLeft = 10;
            Age++;
            if (RemainingFrames > 0)
                RemainingFrames--;

            if (Owner == null || !Owner.active || Owner.dead || RemainingFrames <= 0)
                BeginDismiss();

            if (Dismissing)
            {
                UpdateDismissal();
                return;
            }

            ApplyAmplifierState(IsInsideAnyAmplifierField(NPC.Center));
            UpdateUnitAI();
        }

        protected abstract void UpdateUnitAI();

        protected virtual void DrawUnitExtras(SpriteBatch spriteBatch, Vector2 screenPosition, Color drawColor)
        {
        }

        private void ApplyAmplifierState(bool amplified, bool force = false)
        {
            if (!force && Amplified == amplified)
                return;

            bool newlyAmplified = amplified && !Amplified;
            int oldMaximum = Math.Max(1, NPC.lifeMax);
            float lifeRatio = NPC.life <= 0 ? 0f : NPC.life / (float)oldMaximum;
            Amplified = amplified;
            NPC.lifeMax = Math.Max(1, amplified ? (int)MathF.Ceiling(BaseMaximumLife * 1.5f) : BaseMaximumLife);
            NPC.defense = Math.Max(0, amplified ? (int)MathF.Ceiling(BaseDefense * 1.5f) : BaseDefense);
            NPC.life = Math.Clamp((int)MathF.Ceiling(lifeRatio * NPC.lifeMax), 1, NPC.lifeMax);
            NPC.netUpdate = true;

            // Match the enemy Amplifier's supercharge handoff effect exactly before applying
            // the friendly-only blue outline in PreDraw.
            if (newlyAmplified && !Main.dedServ)
            {
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Electric);
            }
        }

        internal int GetAttackDamage(float multiplier)
        {
            float amplification = Amplified ? 1.5f : 1f;
            return Math.Max(1, (int)MathF.Round(SnapshotDamage * multiplier * amplification));
        }

        protected NPC AcquireTarget(float maximumDistance = 1800f)
        {
            if (Owner == null)
                return null;

            CallofDutyPlayer phonePlayer = Owner.GetModPlayer<CallofDutyPlayer>();
            if (phonePlayer.CommandMode == ResponsibilityCommandMode.Attack && Main.npc.IndexInRange(phonePlayer.CommandTarget))
            {
                NPC commanded = Main.npc[phonePlayer.CommandTarget];
                if (commanded.CanBeChasedBy() && commanded.Distance(NPC.Center) <= maximumDistance)
                    return commanded;
            }

            // A manually marked target or a Wulfrum hat remains a deliberate focus-fire order.
            NPC focusTarget = CallofDutyGlobalNPC.FindPriorityTarget(OwnerIndex, NPC.Center, maximumDistance, includeNearest: false);
            if (focusTarget != null)
                return focusTarget;

            // Without a direct order, each chassis occupies a stable squad slot. The slots cycle
            // over the available enemies, turning a spread-out pack into several small fights
            // instead of making all sixteen machines chase the closest slime.
            return CallofDutyGlobalNPC.FindDistributedTarget(OwnerIndex, GetSquadOrder(), NPC.Center, maximumDistance);
        }

        protected bool IsRamLaunchWindow(int cycleFrames = 120, int windowFrames = 9)
        {
            int squadOrder = GetSquadOrder();
            if (squadOrder < 0 || squadOrder >= 10)
                return false;

            int phase = squadOrder * (cycleFrames / 10);
            int cyclePosition = Age % cycleFrames;
            return cyclePosition >= phase && cyclePosition < phase + windowFrames;
        }

        private int GetSquadOrder()
        {
            return this switch
            {
                ResponsibilityArmyRover => SlotIndex,
                ResponsibilityArmyGyrator => 6 + SlotIndex,
                ResponsibilityArmyDrone => 10 + SlotIndex,
                ResponsibilityArmyHovercraft => 14 + SlotIndex,
                _ => -1
            };
        }

        protected Vector2 GetIdleAnchor(Vector2 formationOffset)
        {
            if (Owner == null)
                return NPC.Center;

            CallofDutyPlayer phonePlayer = Owner.GetModPlayer<CallofDutyPlayer>();
            if (phonePlayer.CommandMode == ResponsibilityCommandMode.Move)
                return phonePlayer.CommandPosition + formationOffset;
            return Owner.Center + formationOffset;
        }

        protected void SpawnRamHitbox(NPC target, float damageMultiplier, int size = 48)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || target == null || Owner == null)
                return;

            int hitbox = Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<ResponsibilityUnitRamHitbox>(),
                GetAttackDamage(damageMultiplier),
                4f,
                OwnerIndex,
                target.whoAmI,
                size);
            if (Main.projectile.IndexInRange(hitbox))
                Main.projectile[hitbox].originalDamage = GetAttackDamage(damageMultiplier);
        }

        internal bool TryInterceptProjectile(Projectile projectile)
        {
            if (!Initialized || Dismissing || !projectile.active || !projectile.hostile || projectile.damage <= 0)
                return false;

            NPC.HitInfo hit = NPC.CalculateHitInfo(projectile.damage, Math.Sign(NPC.Center.X - projectile.Center.X), false, 0f, DamageClass.Default, false);
            int damage = Math.Max(1, hit.Damage);
            damage = ModifyInterceptedProjectileDamage(projectile, damage);
            int damageCap = Math.Max(1, (int)MathF.Ceiling(NPC.lifeMax * DamageCapRatio));
            damage = Math.Min(damage, damageCap);
            hit.Damage = damage;
            hit.SourceDamage = projectile.damage;
            hit.Knockback = 0f;

            NPC.StrikeNPC(hit);
            NPC.netUpdate = true;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            return true;
        }

        protected virtual int ModifyInterceptedProjectileDamage(Projectile projectile, int damage) => damage;

        internal void BeginDismiss(int delay = 0)
        {
            if (Dismissing)
                return;
            Dismissing = true;
            dismissDelay = Math.Max(0, delay);
            DismissTimer = 0;
            NPC.damage = 0;
            NPC.netUpdate = true;
        }

        private void UpdateDismissal()
        {
            if (dismissDelay > 0)
            {
                dismissDelay--;
                return;
            }

            DismissTimer++;
            if (Flying)
                NPC.velocity *= 0.88f;
            else
                NPC.velocity *= new Vector2(0.78f, 1f);
            if (Flying)
                NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, 0.6f, 0.08f);

            if (DismissTimer == 1)
                EmitSmoke(18);
            else if (DismissTimer % 9 == 0)
                EmitSmoke(3);

            if (DismissTimer >= FadeFrames && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.active = false;
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }

        protected void EmitSmoke(int count)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                int type = Main.rand.NextBool(3) ? DustID.Smoke : DustID.Electric;
                Color color = type == DustID.Smoke ? new Color(105, 119, 111) : new Color(72, 199, 255);
                Dust dust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(NPC.width * 0.35f, NPC.height * 0.35f), type, Main.rand.NextVector2Circular(2.5f, 2f) - Vector2.UnitY, 120, color, Main.rand.NextFloat(0.8f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < NativeHitDustCount; i++)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GrassBlades, hit.HitDirection, -1f, 0, default, 1f);

            if (NPC.life > 0)
                return;

            for (int i = 0; i < NativeDeathDustCount; i++)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GrassBlades, hit.HitDirection, -1f, 0, default, 1f);

            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamityMod))
                return;

            foreach (string goreName in NativeGoreNames)
            {
                if (calamityMod.TryFind(goreName, out ModGore gore))
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, gore.Type, 1f);
            }

            int randomGoreCount = Main.rand.Next(1, 4);
            for (int i = 0; i < randomGoreCount; i++)
            {
                string goreName = "WulfrumEnemyGore" + Main.rand.Next(1, 11);
                if (calamityMod.TryFind(goreName, out ModGore gore))
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, gore.Type, 1f);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((short)OwnerIndex);
            writer.Write(Generation);
            writer.Write((byte)SlotIndex);
            writer.Write(SnapshotDamage);
            writer.Write(BaseMaximumLife);
            writer.Write(BaseDefense);
            writer.Write(RemainingFrames);
            writer.Write(Age);
            writer.Write(Amplified);
            writer.Write(Initialized);
            writer.Write(Dismissing);
            writer.Write((byte)DismissTimer);
            writer.Write((byte)dismissDelay);
            WriteUnitExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            OwnerIndex = reader.ReadInt16();
            Generation = reader.ReadInt32();
            SlotIndex = reader.ReadByte();
            SnapshotDamage = reader.ReadInt32();
            BaseMaximumLife = reader.ReadInt32();
            BaseDefense = reader.ReadInt32();
            RemainingFrames = reader.ReadInt32();
            Age = reader.ReadInt32();
            Amplified = reader.ReadBoolean();
            Initialized = reader.ReadBoolean();
            Dismissing = reader.ReadBoolean();
            DismissTimer = reader.ReadByte();
            dismissDelay = reader.ReadByte();
            ReadUnitExtraAI(reader);

            if (Initialized)
            {
                NPC.lifeMax = Math.Max(1, Amplified ? (int)MathF.Ceiling(BaseMaximumLife * 1.5f) : BaseMaximumLife);
                NPC.defense = Math.Max(0, Amplified ? (int)MathF.Ceiling(BaseDefense * 1.5f) : BaseDefense);
            }
        }

        protected virtual void WriteUnitExtraAI(BinaryWriter writer)
        {
        }

        protected virtual void ReadUnitExtraAI(BinaryReader reader)
        {
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;
            Rectangle frame = NPC.frame;
            if (frame.Width <= 0 || frame.Height <= 0)
                frame = texture.Frame(1, FrameCount, 0, 0);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 position = NPC.Center - screenPos + Vector2.UnitY * NPC.gfxOffY;
            // This is Terraria's native NPC flip convention and is the convention used by all
            // five enemy Wulfrum implementations. The previous shared rule was reversed.
            SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float opacity = Dismissing ? MathHelper.Clamp(1f - DismissTimer / (float)FadeFrames, 0f, 1f) : 1f;
            float time = (float)Main.GlobalTimeWrappedHourly + NPC.whoAmI * 0.13f;
            float pulse = 0.5f + 0.5f * MathF.Sin(time * 5.2f);
            Color wulfrumGreen = new(194, 255, 67);

            // Reuse the SHPC holdout glow language instead of drawing eight flat sprite copies:
            // five moving layers contract toward the chassis and brighten toward white, giving
            // every friendly machine a continuous luminous Wulfrum-green shell.
            HoldoutOutlineHelper.DrawSolidOutline(
                texture,
                frame,
                position,
                NPC.rotation,
                origin,
                Vector2.One * NPC.scale,
                effects,
                wulfrumGreen,
                2.8f + pulse * 1.5f,
                (0.5f + pulse * 0.2f) * opacity,
                time,
                20,
                manageBlendState: false);

            spriteBatch.Draw(texture, position, frame, drawColor * opacity, NPC.rotation, origin, NPC.scale, effects, 0f);
            if (Amplified && !Dismissing)
            {
                Color innerGlow = wulfrumGreen * (0.22f + pulse * 0.12f);
                spriteBatch.Draw(texture, position, frame, innerGlow, NPC.rotation, origin, NPC.scale * (1.035f + pulse * 0.015f), effects, 0f);
            }

            DrawUnitExtras(spriteBatch, screenPos, drawColor * opacity);
            return false;
        }

        internal static bool IsInsideAnyAmplifierField(Vector2 position)
        {
            float radiusSquared = AmplifierRadius * AmplifierRadius;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.ModNPC is ResponsibilityArmyAmplifier amplifier && amplifier.FieldOnline && Vector2.DistanceSquared(position, npc.Center) <= radiusSquared)
                    return true;
            }
            return false;
        }

        internal static bool AnyActiveUnitFor(int owner, int generation)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.ModNPC is ResponsibilityArmyUnitBase unit && unit.OwnerIndex == owner && unit.Generation == generation)
                    return true;
            }
            return false;
        }

        internal static void DismissAllFor(int owner, int generation)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.ModNPC is not ResponsibilityArmyUnitBase unit || unit.OwnerIndex != owner || unit.Generation != generation)
                    continue;
                unit.BeginDismiss(unit is ResponsibilityArmyAmplifier ? 15 : Main.rand.Next(0, 16));
            }
        }
    }

    internal sealed class ResponsibilityArmyProjectileInterceptor : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        private bool intercepted;

        public override void PostAI(Projectile projectile)
        {
            if (intercepted || Main.netMode == NetmodeID.MultiplayerClient || !IsSafeToIntercept(projectile))
                return;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.ModNPC is not ResponsibilityArmyUnitBase unit || !projectile.Colliding(projectile.Hitbox, npc.Hitbox))
                    continue;

                if (unit.TryInterceptProjectile(projectile))
                {
                    intercepted = true;
                    projectile.Kill();
                    break;
                }
            }
        }

        private static bool IsSafeToIntercept(Projectile projectile)
        {
            if (!projectile.active || !projectile.hostile || projectile.friendly || projectile.damage <= 0)
                return false;
            if (!projectile.tileCollide || projectile.hide || projectile.timeLeft <= 2)
                return false;
            if (projectile.velocity.LengthSquared() < 0.04f || projectile.width > 160 || projectile.height > 160)
                return false;
            return ProjectileLoader.CanDamage(projectile) != false;
        }
    }

    internal sealed class ResponsibilityUnitRamHitbox : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)Projectile.ai[0];

        public override void AI()
        {
            int size = Math.Max(16, (int)Projectile.ai[1]);
            Projectile.Resize(size, size);
            if (Main.npc.IndexInRange((int)Projectile.ai[0]) && Main.npc[(int)Projectile.ai[0]].active)
                Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center;
        }
    }
}
