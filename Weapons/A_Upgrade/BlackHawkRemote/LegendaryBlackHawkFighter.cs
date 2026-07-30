using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.BlackHawkRemote
{
    internal enum BlackHawkFlightState : byte
    {
        Cruise,
        Align,
        AttackRun,
        Egress,
        Return,
        Resupply
    }

    public sealed class LegendaryBlackHawkFighter : ModProjectile, ILocalizedModType
    {
        private const float CruiseRadius = 170f;
        private const float AttackRunStartDistance = 420f;
        private const float MaxTargetRange = 1600f;

        private BlackHawkLoadout loadedWeapon = BlackHawkLoadout.MachineGun;
        private int ammoRemaining = 5;
        private int targetIndex = -1;
        private int sortieCooldown;
        private int observedCommandRevision = -1;
        private int weaponCounter;
        private int resupplyDuration = 180;
        private Vector2 attackDirection = Vector2.UnitX;
        private Vector2 attackPoint;
        private bool payloadReleased;
        private bool returnAfterSortie;

        public new string LocalizationCategory => "Projectiles.BlackHawk";
        public override string Texture => "CalamityMod/Projectiles/Summon/BlackHawkSummon";

        private BlackHawkFlightState State
        {
            get => (BlackHawkFlightState)(byte)Projectile.ai[0];
            set => Projectile.ai[0] = (byte)value;
        }

        private ref float StateTimer => ref Projectile.ai[1];

        internal BlackHawkLoadout LoadedWeapon => loadedWeapon;
        internal int TargetIndex => targetIndex;
        internal BlackHawkFlightState CurrentState => State;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 30;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            owner.AddBuff(ModContent.BuffType<LegendaryBlackHawkBuff>(), 3600);
            Projectile.timeLeft = 2;

            BlackHawkCommandPlayer commandPlayer = owner.GetModPlayer<BlackHawkCommandPlayer>();
            if (Projectile.localAI[0] == 0f)
                Initialize(owner, commandPlayer);

            if (sortieCooldown > 0)
                sortieCooldown--;

            if (Main.myPlayer == Projectile.owner && observedCommandRevision != commandPlayer.CommandRevision)
                HandleCommandChange(owner, commandPlayer);

            if (Vector2.DistanceSquared(owner.Center, Projectile.Center) > 2800f * 2800f)
            {
                Projectile.Center = owner.Center;
                Projectile.velocity = Vector2.Zero;
                State = BlackHawkFlightState.Cruise;
                StateTimer = 0f;
                targetIndex = -1;
                Projectile.netUpdate = true;
            }

            switch (State)
            {
                case BlackHawkFlightState.Cruise:
                    UpdateCruise(owner, commandPlayer);
                    break;
                case BlackHawkFlightState.Align:
                    UpdateAlign(owner);
                    break;
                case BlackHawkFlightState.AttackRun:
                    UpdateAttackRun(owner);
                    break;
                case BlackHawkFlightState.Egress:
                    UpdateEgress(owner);
                    break;
                case BlackHawkFlightState.Return:
                    UpdateReturn(owner, commandPlayer);
                    break;
                case BlackHawkFlightState.Resupply:
                    UpdateResupply(owner);
                    break;
            }

            Projectile.rotation = Projectile.velocity.LengthSquared() > 0.04f
                ? Projectile.velocity.ToRotation() + MathHelper.Pi
                : Projectile.rotation;
            EmitFlightVFX();
        }

        private void Initialize(Player owner, BlackHawkCommandPlayer commandPlayer)
        {
            Projectile.localAI[0] = 1f;
            observedCommandRevision = commandPlayer.CommandRevision;
            loadedWeapon = commandPlayer.Command == BlackHawkLoadout.Auto
                ? ChooseAutoLoadout(owner)
                : commandPlayer.Command;
            ammoRemaining = BlackHawkLoadoutInfo.Ammo(loadedWeapon);
            State = BlackHawkFlightState.Cruise;
            StateTimer = 0f;
            attackDirection = Vector2.UnitX * owner.direction;

            if (Main.myPlayer == Projectile.owner)
            {
                BlackHawkVFX.SpawnPulse(Projectile.Center, BlackHawkLoadoutInfo.Color(loadedWeapon), 0.08f, 0.48f, 14,
                    new Vector2(1f, 0.65f));
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.38f, Pitch = 0.18f }, Projectile.Center);
            }
            Projectile.netUpdate = true;
        }

        private void HandleCommandChange(Player owner, BlackHawkCommandPlayer commandPlayer)
        {
            observedCommandRevision = commandPlayer.CommandRevision;
            if (State == BlackHawkFlightState.AttackRun)
            {
                returnAfterSortie = true;
            }
            else if (State == BlackHawkFlightState.Egress)
            {
                returnAfterSortie = true;
            }
            else if (State == BlackHawkFlightState.Resupply)
            {
                SelectResupplyLoadout(owner, commandPlayer);
                StateTimer = 0f;
            }
            else if (State != BlackHawkFlightState.Return)
            {
                BeginReturn();
            }
            Projectile.netUpdate = true;
        }

        private void UpdateCruise(Player owner, BlackHawkCommandPlayer commandPlayer)
        {
            StateTimer++;
            targetIndex = -1;
            MoveToward(FormationPosition(owner, CruiseRadius), 16f, 22f);

            if (sortieCooldown > 0 || Main.myPlayer != Projectile.owner)
                return;

            if (!TryFindTarget(owner, loadedWeapon, out NPC target, out Vector2 predictedPoint))
            {
                if (commandPlayer.Command == BlackHawkLoadout.Auto && loadedWeapon != BlackHawkLoadout.MachineGun && StateTimer >= 180f)
                    BeginReturn();
                return;
            }

            if (!commandPlayer.TryClaimDispatch(target.whoAmI))
                return;

            targetIndex = target.whoAmI;
            attackPoint = predictedPoint;
            attackDirection = (attackPoint - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);
            State = BlackHawkFlightState.Align;
            StateTimer = 0f;
            weaponCounter = 0;
            payloadReleased = false;
            Projectile.netUpdate = true;
        }

        private void UpdateAlign(Player owner)
        {
            StateTimer++;
            if (!TryGetTarget(out NPC target))
            {
                BeginCruise();
                return;
            }

            Vector2 refinedPoint = PredictTargetPoint(target, loadedWeapon, owner);
            attackPoint = Vector2.Lerp(attackPoint, refinedPoint, 0.075f);
            Vector2 stagingPoint = attackPoint - attackDirection * AttackRunStartDistance;
            MoveToward(stagingPoint, 19f, 13f);

            if (Vector2.DistanceSquared(Projectile.Center, stagingPoint) <= 46f * 46f || StateTimer >= 120f)
            {
                attackDirection = (attackPoint - Projectile.Center).SafeNormalize(attackDirection);
                BeginAttackRun();
            }
        }

        private void UpdateAttackRun(Player owner)
        {
            StateTimer++;
            bool targetAlive = TryGetTarget(out _);
            if (!targetAlive && !payloadReleased)
            {
                BeginEgress(weaponCounter > 0);
                return;
            }

            float speed = StateTimer < 12f ? 18f : StateTimer < 61f ? 10.5f : 27f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, attackDirection * speed, 0.22f);
            TryReleaseWeapon(owner);

            if (StateTimer >= 80f)
                BeginEgress(payloadReleased);
        }

        private void UpdateEgress(Player owner)
        {
            StateTimer++;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, attackDirection * 28f, 0.18f);
            if (StateTimer < 48f)
                return;

            if (ammoRemaining <= 0 || returnAfterSortie)
                BeginReturn();
            else
                BeginCruise();
        }

        private void UpdateReturn(Player owner, BlackHawkCommandPlayer commandPlayer)
        {
            StateTimer++;
            Vector2 desired = FormationPosition(owner, 58f);
            MoveToward(desired, 22f, 10f);
            if (Vector2.DistanceSquared(Projectile.Center, desired) > 72f * 72f)
                return;

            SelectResupplyLoadout(owner, commandPlayer);
            State = BlackHawkFlightState.Resupply;
            StateTimer = 0f;
            Projectile.velocity *= 0.55f;
            returnAfterSortie = false;
            Projectile.netUpdate = true;

            if (Main.myPlayer == Projectile.owner)
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.28f, Pitch = -0.16f }, Projectile.Center);
        }

        private void UpdateResupply(Player owner)
        {
            StateTimer++;
            MoveToward(FormationPosition(owner, 58f), 9f, 28f);
            if (StateTimer < resupplyDuration)
                return;

            ammoRemaining = BlackHawkLoadoutInfo.Ammo(loadedWeapon);
            sortieCooldown = 18;
            returnAfterSortie = false;
            BeginCruise();
            Projectile.netUpdate = true;

            if (Main.myPlayer == Projectile.owner)
            {
                BlackHawkVFX.SpawnPulse(Projectile.Center, BlackHawkLoadoutInfo.Color(loadedWeapon), 0.05f, 0.30f, 10,
                    new Vector2(1f, 0.62f));
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.34f, Pitch = 0.24f }, Projectile.Center);
            }
        }

        private void SelectResupplyLoadout(Player owner, BlackHawkCommandPlayer commandPlayer)
        {
            loadedWeapon = commandPlayer.Command == BlackHawkLoadout.Auto
                ? ChooseAutoLoadout(owner)
                : commandPlayer.Command;
            ammoRemaining = 0;
            resupplyDuration = BlackHawkLoadoutInfo.ResupplyTime(loadedWeapon);
        }

        private void BeginAttackRun()
        {
            State = BlackHawkFlightState.AttackRun;
            StateTimer = 0f;
            weaponCounter = 0;
            payloadReleased = false;
            sortieCooldown = BlackHawkLoadoutInfo.SortieCooldown(loadedWeapon);
            Projectile.netUpdate = true;
        }

        private void BeginEgress(bool consumedSortie)
        {
            if (consumedSortie)
                ammoRemaining = Math.Max(0, ammoRemaining - 1);
            State = BlackHawkFlightState.Egress;
            StateTimer = 0f;
            targetIndex = -1;
            Projectile.netUpdate = true;
        }

        private void BeginReturn()
        {
            State = BlackHawkFlightState.Return;
            StateTimer = 0f;
            targetIndex = -1;
            Projectile.netUpdate = true;
        }

        private void BeginCruise()
        {
            State = BlackHawkFlightState.Cruise;
            StateTimer = 0f;
            targetIndex = -1;
            Projectile.netUpdate = true;
        }

        private void TryReleaseWeapon(Player owner)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            switch (loadedWeapon)
            {
                case BlackHawkLoadout.MachineGun:
                    if (StateTimer >= 14f && weaponCounter < 12 && ((int)StateTimer - 14) % 4 == 0)
                    {
                        FireMachineGunRound(owner);
                        weaponCounter++;
                        payloadReleased = weaponCounter >= 12;
                    }
                    break;

                case BlackHawkLoadout.GuidedMissiles:
                    if ((weaponCounter == 0 && StateTimer >= 24f) || (weaponCounter == 1 && StateTimer >= 36f))
                    {
                        FireGuidedMissile(weaponCounter == 0 ? -1f : 1f);
                        weaponCounter++;
                        payloadReleased = weaponCounter >= 2;
                    }
                    break;

                default:
                    if (!payloadReleased && StateTimer >= 30f)
                    {
                        DropPayload();
                        payloadReleased = true;
                    }
                    break;
            }
        }

        private void FireMachineGunRound(Player owner)
        {
            Vector2 side = attackDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 spawn = Projectile.Center + attackDirection * 25f + side * (weaponCounter % 2 == 0 ? -11f : 11f);
            Vector2 aimPoint = attackPoint;
            if (TryGetTarget(out NPC target))
                aimPoint = PredictTargetPoint(target, loadedWeapon, owner);
            Vector2 velocity = (aimPoint - spawn).SafeNormalize(attackDirection)
                .RotatedBy(Main.rand.NextFloat(-0.026f, 0.026f)) * 23f;

            int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, velocity,
                ModContent.ProjectileType<BlackHawkMachineGunRound>(), ScaleDamage(0.35f), Projectile.knockBack * 0.25f,
                Projectile.owner);
            PrepareChildProjectile(index);

            if (weaponCounter == 0 || weaponCounter == 6)
                SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.28f, Pitch = 0.34f }, spawn);
        }

        private void FireGuidedMissile(float sideSign)
        {
            Vector2 side = attackDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 spawn = Projectile.Center + attackDirection * 8f + side * 18f * sideSign;
            int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, attackDirection * 7.2f,
                ModContent.ProjectileType<BlackHawkGuidedMissile>(), ScaleDamage(1.40f), Projectile.knockBack,
                Projectile.owner, targetIndex);
            PrepareChildProjectile(index);
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.40f, Pitch = 0.16f }, spawn);
        }

        private void DropPayload()
        {
            Vector2 velocity = attackDirection * (loadedWeapon == BlackHawkLoadout.HeavyBomb ? 2.8f : 4.2f);
            int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                ModContent.ProjectileType<BlackHawkPayload>(),
                ScaleDamage(BlackHawkLoadoutInfo.MainDamageMultiplier(loadedWeapon)), Projectile.knockBack,
                Projectile.owner, (float)loadedWeapon, attackDirection.ToRotation());
            PrepareChildProjectile(index);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.30f, Pitch = -0.32f }, Projectile.Center);
        }

        private void PrepareChildProjectile(int projectileIndex)
        {
            if (!Main.projectile.IndexInRange(projectileIndex))
                return;

            Projectile child = Main.projectile[projectileIndex];
            child.originalDamage = Math.Max(1, Projectile.damage);
            child.CritChance = Projectile.CritChance;
            child.netUpdate = true;
        }

        private int ScaleDamage(float multiplier) => Math.Max(1, (int)Math.Round(Projectile.damage * multiplier));

        private void MoveToward(Vector2 destination, float speed, float inertia)
        {
            Vector2 desiredVelocity = (destination - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
            float distance = Vector2.Distance(destination, Projectile.Center);
            if (distance < speed * 3f)
                desiredVelocity *= MathHelper.Clamp(distance / (speed * 3f), 0.22f, 1f);
            Projectile.velocity = (Projectile.velocity * (inertia - 1f) + desiredVelocity) / inertia;
        }

        private Vector2 FormationPosition(Player owner, float radius)
        {
            int count = 0;
            int index = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != Type)
                    continue;
                if (other.whoAmI < Projectile.whoAmI)
                    index++;
                count++;
            }

            count = Math.Max(1, count);
            float angle = Main.GameUpdateCount * 0.011f + MathHelper.TwoPi * index / count;
            return owner.Center + angle.ToRotationVector2() * radius;
        }

        private bool TryGetTarget(out NPC target)
        {
            target = null;
            if (!Main.npc.IndexInRange(targetIndex))
                return false;
            NPC candidate = Main.npc[targetIndex];
            if (!candidate.CanBeChasedBy(Projectile, false))
                return false;
            target = candidate;
            return true;
        }

        private bool TryFindTarget(Player owner, BlackHawkLoadout loadout, out NPC target, out Vector2 predictedPoint)
        {
            target = null;
            predictedPoint = Vector2.Zero;
            float bestScore = float.MinValue;

            foreach (NPC candidate in Main.ActiveNPCs)
            {
                if (!candidate.CanBeChasedBy(Projectile, false) ||
                    Vector2.DistanceSquared(owner.Center, candidate.Center) > MaxTargetRange * MaxTargetRange)
                {
                    continue;
                }

                if (!CanAssign(loadout, candidate))
                    continue;

                float score = ScoreTarget(owner, candidate, loadout);
                if (score == float.MinValue)
                    continue;

                bool nearTie = bestScore > 0f && score >= bestScore * 0.92f;
                if (score > bestScore || nearTie && Main.rand.NextBool(4))
                {
                    bestScore = score;
                    target = candidate;
                }
            }

            if (target is null)
                return false;

            predictedPoint = PredictTargetPoint(target, loadout, owner);
            return true;
        }

        private BlackHawkLoadout ChooseAutoLoadout(Player owner)
        {
            BlackHawkLoadout bestLoadout = BlackHawkLoadout.MachineGun;
            float bestScore = float.MinValue;
            foreach (NPC candidate in Main.ActiveNPCs)
            {
                if (!candidate.CanBeChasedBy(Projectile, false) ||
                    Vector2.DistanceSquared(owner.Center, candidate.Center) > MaxTargetRange * MaxTargetRange)
                {
                    continue;
                }

                for (int raw = BlackHawkLoadoutInfo.FirstWeapon; raw <= BlackHawkLoadoutInfo.LastWeapon; raw++)
                {
                    BlackHawkLoadout loadout = (BlackHawkLoadout)raw;
                    if (!CanAssign(loadout, candidate))
                        continue;

                    float score = ScoreTarget(owner, candidate, loadout);
                    score -= LoadedWeaponSaturationPenalty(loadout);
                    bool nearTie = bestScore > 0f && score >= bestScore * 0.94f;
                    if (score > bestScore || nearTie && Main.rand.NextBool(5))
                    {
                        bestScore = score;
                        bestLoadout = loadout;
                    }
                }
            }
            return bestLoadout;
        }

        private float ScoreTarget(Player owner, NPC target, BlackHawkLoadout loadout)
        {
            BlackHawkTargetStatusNPC status = target.GetGlobalNPC<BlackHawkTargetStatusNPC>();
            int density = CountNearbyEnemies(target.Center, 245f);
            float speed = target.velocity.Length();
            float size = Math.Max(target.width, target.height);
            float lifeWeight = Math.Min(70f, target.lifeMax / 350f);
            float playerThreat = MathHelper.Clamp(620f - Vector2.Distance(owner.Center, target.Center), 0f, 620f) * 0.055f;
            float score = 18f + lifeWeight + playerThreat;

            if (target.boss)
                score += 115f;
            if (owner.HasMinionAttackTargetNPC && owner.MinionAttackTargetNPC == target.whoAmI)
                score += 42f;
            if (status.IsIlluminated(owner.whoAmI))
                score += 56f;

            switch (loadout)
            {
                case BlackHawkLoadout.MachineGun:
                    score += speed * 7f + (target.lifeMax < 2400 ? 36f : 0f) - Math.Max(0, density - 2) * 8f;
                    break;

                case BlackHawkLoadout.GuidedMissiles:
                    score += (target.boss ? 72f : 16f) + speed * 8f + size * 0.18f;
                    break;

                case BlackHawkLoadout.ClusterBomb:
                    if (density < 4 && target.realLife < 0)
                        return float.MinValue;
                    score += density * 27f + size * 0.16f;
                    break;

                case BlackHawkLoadout.Napalm:
                    if ((density < 2 && !target.boss) || speed > 5.5f)
                        return float.MinValue;
                    score += density * 23f + (target.collideY ? 34f : 0f) - speed * 8f;
                    break;

                case BlackHawkLoadout.Cryogenic:
                    if (status.IsCryogenic(owner.whoAmI) || speed < 2.2f)
                        return float.MinValue;
                    score += speed * 15f + density * 12f + playerThreat;
                    break;

                case BlackHawkLoadout.EMP:
                    if (status.IsEMPd(owner.whoAmI))
                        return float.MinValue;
                    score += (target.boss ? 92f : 0f) + speed * 13f + playerThreat * 1.25f;
                    break;

                case BlackHawkLoadout.HolyPayload:
                    if (!target.boss && target.lifeMax < 2200 && density < 2)
                        return float.MinValue;
                    score += (target.boss ? 46f : 20f) + Math.Min(density, 4) * 18f + size * 0.12f;
                    break;

                case BlackHawkLoadout.DirtyBomb:
                    if (density < 5 || speed > 4.2f)
                        return float.MinValue;
                    score += density * 32f - speed * 10f;
                    break;

                case BlackHawkLoadout.HeavyBomb:
                    if ((!target.boss && target.lifeMax < 8000) || speed > 7.5f)
                        return float.MinValue;
                    score += (target.boss ? 135f : 70f) + size * 0.24f - speed * 9f;
                    break;
            }

            return score;
        }

        private float LoadedWeaponSaturationPenalty(BlackHawkLoadout loadout)
        {
            int sameLoadout = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != Type ||
                    projectile.whoAmI == Projectile.whoAmI ||
                    projectile.ModProjectile is not LegendaryBlackHawkFighter fighter || fighter.loadedWeapon != loadout)
                {
                    continue;
                }
                sameLoadout++;
            }

            float perAircraft = loadout is BlackHawkLoadout.Napalm or BlackHawkLoadout.Cryogenic or
                BlackHawkLoadout.EMP or BlackHawkLoadout.DirtyBomb or BlackHawkLoadout.HeavyBomb
                ? 62f
                : loadout == BlackHawkLoadout.ClusterBomb ? 34f : 18f;
            return sameLoadout * perAircraft;
        }

        private bool CanAssign(BlackHawkLoadout loadout, NPC target)
        {
            int concurrent = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != Projectile.owner)
                    continue;

                if (projectile.type == Type && projectile.ModProjectile is LegendaryBlackHawkFighter fighter &&
                    fighter != this && fighter.loadedWeapon == loadout &&
                    fighter.State is BlackHawkFlightState.Align or BlackHawkFlightState.AttackRun)
                {
                    bool sameAssignment = fighter.targetIndex == target.whoAmI;
                    if (loadout == BlackHawkLoadout.HeavyBomb)
                        sameAssignment = true;
                    else if (loadout == BlackHawkLoadout.ClusterBomb || BlackHawkLoadoutInfo.UsesPersistentArea(loadout))
                        sameAssignment |= Vector2.DistanceSquared(fighter.attackPoint, target.Center) <= 240f * 240f;

                    if (sameAssignment)
                        concurrent++;
                }

                if (loadout == BlackHawkLoadout.HeavyBomb &&
                    ((projectile.type == ModContent.ProjectileType<BlackHawkPayload>() &&
                      (BlackHawkLoadout)(sbyte)projectile.ai[0] == BlackHawkLoadout.HeavyBomb) ||
                     (projectile.type == ModContent.ProjectileType<BlackHawkCompactBlast>() &&
                      (BlackHawkLoadout)(sbyte)projectile.ai[0] == BlackHawkLoadout.HeavyBomb)))
                {
                    return false;
                }

                if (BlackHawkLoadoutInfo.UsesPersistentArea(loadout) &&
                    projectile.type == ModContent.ProjectileType<BlackHawkPersistentZone>() &&
                    (BlackHawkLoadout)(sbyte)projectile.ai[0] == loadout &&
                    Vector2.DistanceSquared(projectile.Center, target.Center) <= 230f * 230f)
                {
                    return false;
                }
            }

            BlackHawkTargetStatusNPC status = target.GetGlobalNPC<BlackHawkTargetStatusNPC>();
            if (loadout == BlackHawkLoadout.EMP && status.IsEMPd(Projectile.owner))
                return false;
            if (loadout == BlackHawkLoadout.Cryogenic && status.IsCryogenic(Projectile.owner))
                return false;

            return concurrent < BlackHawkLoadoutInfo.ConcurrentLimit(loadout);
        }

        private static int CountNearbyEnemies(Vector2 center, float radius)
        {
            int count = 0;
            float radiusSquared = radius * radius;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.CanBeChasedBy() && Vector2.DistanceSquared(center, npc.Center) <= radiusSquared)
                    count++;
            }
            return count;
        }

        private Vector2 PredictTargetPoint(NPC target, BlackHawkLoadout loadout, Player owner)
        {
            float distance = Vector2.Distance(Projectile.Center, target.Center);
            float leadFrames = MathHelper.Clamp(distance / (loadout == BlackHawkLoadout.GuidedMissiles ? 20f : 15f), 5f, 32f);
            BlackHawkTargetStatusNPC status = target.GetGlobalNPC<BlackHawkTargetStatusNPC>();
            float accuracy = status.IsIlluminated(owner.whoAmI) || status.IsEMPd(owner.whoAmI) ||
                status.IsCryogenic(owner.whoAmI) ? 1f : 0.72f;
            return target.Center + target.velocity * leadFrames * accuracy;
        }

        private void EmitFlightVFX()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 enginePosition = Projectile.Center - forward * 31f;
            Color color = BlackHawkLoadoutInfo.Color(loadedWeapon);
            BlackHawkVFX.SpawnEnginePoint(enginePosition, -forward, color);

            if (Projectile.velocity.Length() > 9f && Main.GameUpdateCount % 3 == Projectile.identity % 3)
                BlackHawkVFX.SpawnSmokePoint(enginePosition, -forward * 0.8f, color, new Color(48, 55, 60), 0.38f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/BlackHawkGlow").Value;
            int frameIndex = Math.Clamp((int)(Projectile.velocity.Length() / 5.2f), 0, Main.projFrames[Type] - 1);
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, frameIndex);
            Rectangle glowFrame = glow.Frame(1, Main.projFrames[Type], 0, frameIndex);

            if (State is BlackHawkFlightState.AttackRun or BlackHawkFlightState.Egress)
            {
                for (int i = Projectile.oldPos.Length - 1; i >= 2; i -= 2)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float fade = (1f - i / (float)Projectile.oldPos.Length) * 0.16f;
                    Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(texture, oldCenter, frame, lightColor * fade, Projectile.oldRot[i],
                        frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
                }
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation,
                frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, drawPosition, glowFrame, Color.White, Projectile.rotation,
                glowFrame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Color loadoutColor = BlackHawkLoadoutInfo.Color(loadedWeapon);
            BlackHawkVFX.DrawBloom(Projectile.Center - forward * 28f, loadoutColor, 9f, 0.42f);
            BlackHawkVFX.DrawBloom(Projectile.Center + side * 20f - forward * 2f, loadoutColor, 4f, 0.30f);
            BlackHawkVFX.DrawBloom(Projectile.Center - side * 20f - forward * 2f, loadoutColor, 4f, 0.30f);
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((sbyte)loadedWeapon);
            writer.Write((short)ammoRemaining);
            writer.Write(targetIndex);
            writer.Write((short)sortieCooldown);
            writer.Write(observedCommandRevision);
            writer.Write((byte)weaponCounter);
            writer.Write((short)resupplyDuration);
            writer.Write(attackDirection.X);
            writer.Write(attackDirection.Y);
            writer.Write(attackPoint.X);
            writer.Write(attackPoint.Y);
            writer.Write(payloadReleased);
            writer.Write(returnAfterSortie);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            loadedWeapon = BlackHawkLoadoutInfo.Sanitize(reader.ReadSByte());
            ammoRemaining = reader.ReadInt16();
            targetIndex = reader.ReadInt32();
            sortieCooldown = reader.ReadInt16();
            observedCommandRevision = reader.ReadInt32();
            weaponCounter = reader.ReadByte();
            resupplyDuration = reader.ReadInt16();
            attackDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            attackPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            payloadReleased = reader.ReadBoolean();
            returnAfterSortie = reader.ReadBoolean();
        }
    }
}
