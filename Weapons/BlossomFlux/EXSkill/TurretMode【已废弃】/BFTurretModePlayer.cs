//using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
//using CalamityMod;
//using Microsoft.Xna.Framework;
//using Terraria;
//using Terraria.Audio;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill.TurretMode
//{
//    internal sealed class BFTurretModePlayer : ModPlayer
//    {
//        public const float TurretDamageMultiplier = 1.75f;
//        public const int MinimumExitLockFrames = BFEXPlayer.UltimateCooldownFrames;
//        private const int VineReleaseRate = 42;

//        private bool holdingBlossomFlux;
//        private int vineReleaseTimer;
//        private int lockedItemSlot = -1;
//        private Vector2 turretAnchorCenter;
//        private float auraStrength;

//        public bool TurretModeActive { get; private set; }
//        public int FramesInTurretMode { get; private set; }
//        public float AuraStrength => auraStrength;
//        public bool CanExitTurretMode => TurretModeActive && FramesInTurretMode >= MinimumExitLockFrames && Player.GetModPlayer<BFEXPlayer>().CanTriggerUltimate;

//        public override void ResetEffects()
//        {
//            holdingBlossomFlux = false;
//        }

//        public override void UpdateDead()
//        {
//            TurretModeActive = false;
//            FramesInTurretMode = 0;
//            vineReleaseTimer = 0;
//            lockedItemSlot = -1;
//            auraStrength = 0f;
//            turretAnchorCenter = Vector2.Zero;
//        }

//        public override void PreUpdate()
//        {
//            if (!TurretModeActive)
//                return;

//            if (!EnsureWeaponLock())
//            {
//                ExitTurretMode(false);
//                return;
//            }

//            FreezePlayerAtAnchor();
//        }

//        public override void PostUpdate()
//        {
//            float targetAuraStrength = TurretModeActive ? 1f : 0f;
//            auraStrength = MathHelper.Lerp(auraStrength, targetAuraStrength, TurretModeActive ? 0.14f : 0.08f);

//            if (!TurretModeActive)
//                return;

//            if (!holdingBlossomFlux && Player.HeldItem.type != ModContent.ItemType<NewLegendBlossomFlux>())
//            {
//                ExitTurretMode(false);
//                return;
//            }

//            FramesInTurretMode++;
//            FreezePlayerAtAnchor();
//            MaintainAimBeam();
//            EmitTurretDust();

//            if (Player.whoAmI != Main.myPlayer)
//                return;

//            if (++vineReleaseTimer >= VineReleaseRate)
//            {
//                vineReleaseTimer = Main.rand.Next(8, 18);
//                ReleaseTurretVines();
//            }
//        }

//        public void SetHoldingBlossomFlux()
//        {
//            holdingBlossomFlux = true;
//        }

//        public bool EnterTurretMode()
//        {
//            if (TurretModeActive)
//                return false;

//            int weaponSlot = FindWeaponSlot();
//            if (!weaponSlot.WithinBounds(Player.inventory.Length))
//                return false;

//            TurretModeActive = true;
//            FramesInTurretMode = 0;
//            lockedItemSlot = weaponSlot;
//            turretAnchorCenter = Player.Center;
//            vineReleaseTimer = 10;
//            Player.selectedItem = lockedItemSlot;
//            Player.velocity = Vector2.Zero;

//            SpawnBurstDust(new Color(122, 255, 150), 18, 1.4f, 5.6f);
//            SoundEngine.PlaySound(SoundID.Item125 with { Volume = 0.75f, Pitch = -0.1f }, Player.Center);
//            return true;
//        }

//        public bool ExitTurretMode(bool playSound = true)
//        {
//            if (!TurretModeActive)
//                return false;

//            TurretModeActive = false;
//            FramesInTurretMode = 0;
//            vineReleaseTimer = 0;
//            lockedItemSlot = -1;

//            SpawnBurstDust(new Color(170, 255, 170), 14, 1f, 4.6f);
//            if (playSound)
//                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.75f, Pitch = -0.15f }, Player.Center);

//            return true;
//        }

//        private void FreezePlayerAtAnchor()
//        {
//            Player.Center = turretAnchorCenter;
//            Player.velocity = Vector2.Zero;
//            Player.maxRunSpeed = 0f;
//            Player.accRunSpeed = 0f;
//            Player.runAcceleration = 0f;
//            Player.runSlowdown = 1f;

//            Player.controlLeft = false;
//            Player.controlRight = false;
//            Player.controlUp = false;
//            Player.controlDown = false;
//            Player.controlJump = false;
//            Player.controlHook = false;
//            Player.controlMount = false;
//            Player.controlQuickHeal = false;
//            Player.controlQuickMana = false;
//            Player.controlUseTile = false;
//        }

//        private bool EnsureWeaponLock()
//        {
//            if (!lockedItemSlot.WithinBounds(Player.inventory.Length) || Player.inventory[lockedItemSlot].type != ModContent.ItemType<NewLegendBlossomFlux>())
//                lockedItemSlot = FindWeaponSlot();

//            if (!lockedItemSlot.WithinBounds(Player.inventory.Length))
//                return false;

//            Player.selectedItem = lockedItemSlot;
//            return Player.inventory[lockedItemSlot].type == ModContent.ItemType<NewLegendBlossomFlux>();
//        }

//        private int FindWeaponSlot()
//        {
//            for (int i = 0; i < Player.inventory.Length; i++)
//            {
//                if (Player.inventory[i].type == ModContent.ItemType<NewLegendBlossomFlux>())
//                    return i;
//            }

//            return -1;
//        }

//        private void MaintainAimBeam()
//        {
//            if (Player.whoAmI != Main.myPlayer ||
//                Player.ownedProjectileCounts[ModContent.ProjectileType<BFTurretAimBeam>()] > 0)
//            {
//                return;
//            }

//            Projectile.NewProjectile(
//                Player.GetSource_FromThis(),
//                Player.MountedCenter + Vector2.UnitX * Player.direction * 18f,
//                Vector2.UnitX * Player.direction,
//                ModContent.ProjectileType<BFTurretAimBeam>(),
//                0,
//                0f,
//                Player.whoAmI);
//        }

//        private void EmitTurretDust()
//        {
//            if (Main.dedServ || !Main.rand.NextBool(3))
//                return;

//            float orbitAngle = Main.GlobalTimeWrappedHourly * 2.4f + Player.whoAmI * 0.6f;
//            Vector2 orbitOffset = new Vector2(0f, -34f).RotatedBy(orbitAngle);
//            orbitOffset.X *= 0.8f;

//            Dust dust = Dust.NewDustPerfect(
//                Player.Center + orbitOffset,
//                Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GemEmerald,
//                (-orbitOffset).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.35f, 1.2f),
//                100,
//                Color.Lerp(new Color(96, 255, 150), new Color(220, 255, 220), auraStrength),
//                MathHelper.Lerp(0.8f, 1.15f, auraStrength));
//            dust.noGravity = true;
//        }

//        private void ReleaseTurretVines()
//        {
//            // 炮台期间持续向外甩出藤蔓，用来补足“森林活化”的氛围反馈。
//            Vector2 origin = Player.Center;
//            Vector2 toMouse = GetAimDirection();
//            NPC nearestTarget = FindNearestTarget(origin, 760f);
//            Vector2 mainDirection = nearestTarget != null
//                ? (nearestTarget.Center - origin).SafeNormalize(toMouse)
//                : toMouse;

//            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.18f, Pitch = 0.45f }, origin);
//            SpawnSingleVine(origin, mainDirection);

//            if (Main.rand.NextBool(2))
//            {
//                Vector2 sideDirection = mainDirection.RotatedBy(Main.rand.NextBool() ? 0.45f : -0.45f);
//                SpawnSingleVine(origin, sideDirection);
//            }
//        }

//        private void SpawnSingleVine(Vector2 origin, Vector2 direction)
//        {
//            int damage = (int)(Player.GetWeaponDamage(Player.HeldItem) * 0.48f);
//            Projectile.NewProjectile(
//                Player.GetSource_FromThis(),
//                origin + direction * 18f,
//                direction * 22f,
//                ModContent.ProjectileType<BFTurretVine>(),
//                damage,
//                1.2f,
//                Player.whoAmI);
//        }

//        private Vector2 GetAimDirection()
//        {
//            Vector2 mouseWorld = Player.Calamity().mouseWorld;
//            if (mouseWorld == Vector2.Zero)
//                mouseWorld = Main.MouseWorld;

//            Vector2 aimDirection = mouseWorld - Player.MountedCenter;
//            if (aimDirection == Vector2.Zero)
//                aimDirection = Vector2.UnitX * Player.direction;

//            return aimDirection.SafeNormalize(Vector2.UnitX * Player.direction);
//        }

//        private NPC FindNearestTarget(Vector2 source, float maxDistance)
//        {
//            NPC nearest = null;
//            float maxDistanceSquared = maxDistance * maxDistance;

//            for (int i = 0; i < Main.maxNPCs; i++)
//            {
//                NPC npc = Main.npc[i];
//                if (!npc.CanBeChasedBy())
//                    continue;

//                float distanceSquared = Vector2.DistanceSquared(source, npc.Center);
//                if (distanceSquared > maxDistanceSquared)
//                    continue;

//                maxDistanceSquared = distanceSquared;
//                nearest = npc;
//            }

//            return nearest;
//        }

//        private void SpawnBurstDust(Color color, int amount, float speedMin, float speedMax)
//        {
//            for (int i = 0; i < amount; i++)
//            {
//                Dust dust = Dust.NewDustPerfect(
//                    Player.Center,
//                    Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GemEmerald,
//                    Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(speedMin, speedMax),
//                    100,
//                    color,
//                    Main.rand.NextFloat(0.95f, 1.35f));
//                dust.noGravity = true;
//            }
//        }
//    }
//}
