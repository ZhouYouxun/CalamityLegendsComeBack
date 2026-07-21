using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.P90
{
    internal sealed class NewLegendP90Player : ModPlayer
    {
        private ReloadData pendingReload;

        public int Magazine { get; private set; }
        public int LoadedProjectileType { get; private set; }
        public int LoadedAmmoItemType { get; private set; }
        public int LoadedDamageOffset { get; private set; }
        public float LoadedShootSpeed { get; private set; }
        public float LoadedKnockback { get; private set; }
        public int ReloadTimer { get; private set; }
        public int RollTimer { get; private set; }
        public int RollDirection { get; private set; } = 1;
        public int DashCooldownTimer { get; private set; }
        public int DodgeEmpowerTimer { get; private set; }
        public bool HoldingP90 { get; private set; }

        private bool dodgedHostileProjectileThisRoll;

        public bool IsReloading => ReloadTimer > 0;
        public bool IsRolling => RollTimer > 0;
        public bool DodgeEmpowered => DodgeEmpowerTimer > 0;
        public float ReloadCompletion => IsReloading ? 1f - ReloadTimer / (float)NewLegendP90.ReloadFrames : 0f;
        public float RollCompletion => IsRolling ? 1f - RollTimer / (float)NewLegendP90.RollFrames : 0f;
        public float DashCooldownCompletion => DashCooldownTimer <= 0 ? 1f : 1f - DashCooldownTimer / (float)NewLegendP90.RollCooldownFrames;

        public override void Initialize()
        {
            ResetMagazineToDefault();
        }

        public override void ResetEffects()
        {
            HoldingP90 = false;
        }

        public override void UpdateDead()
        {
            ReloadTimer = 0;
            RollTimer = 0;
            DashCooldownTimer = 0;
            DodgeEmpowerTimer = 0;
            dodgedHostileProjectileThisRoll = false;
            Player.fullRotation = 0f;
            ResetMagazineToDefault();
        }

        public override void PostUpdate()
        {
            if (Magazine > NewLegendP90.MagazineCapacity)
                Magazine = NewLegendP90.MagazineCapacity;

            if (ReloadTimer > 0)
            {
                ReloadTimer--;
                if (ReloadTimer <= 0)
                    CompleteReload();
            }

            if (DodgeEmpowerTimer > 0)
            {
                DodgeEmpowerTimer--;
                if (DodgeEmpowerTimer <= 0)
                    SpawnEmpowerEndContraction();
            }

            if (DashCooldownTimer > 0)
            {
                DashCooldownTimer--;
                EnsureCooldownBar();
            }

            UpdateRoll();
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!IsRolling)
                return;

            modifiers.SourceDamage *= 0f;
            modifiers.Knockback *= 0f;
        }

        public void SetHoldingP90()
        {
            HoldingP90 = true;
        }

        public int GetLoadedShotDamage(Player player, Item weapon)
        {
            return Math.Max(1, player.GetWeaponDamage(weapon) + LoadedDamageOffset);
        }

        public bool ConsumeMagazineShot()
        {
            if (Magazine <= 0 || IsReloading)
                return false;

            Magazine--;
            return true;
        }

        public bool TryStartReload(Player player, Item weapon)
        {
            if (IsReloading)
                return true;

            if (!TryBuildReloadData(player, weapon, out ReloadData data))
                return false;

            if (!data.Infinite && !ConsumeAmmoForReload(player, data.AmmoItemType, NewLegendP90.ReloadConsumeCount))
                return false;

            pendingReload = data;
            ReloadTimer = NewLegendP90.ReloadFrames;
            Magazine = 0;

            SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.72f, Pitch = -0.22f }, player.Center);
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    player.Center + Main.rand.NextVector2Circular(18f, 14f),
                    DustID.GoldCoin,
                    Main.rand.NextVector2Circular(1.2f, 1.2f) - Vector2.UnitY * Main.rand.NextFloat(0.8f, 2.2f),
                    120,
                    Color.Gold,
                    Main.rand.NextFloat(0.75f, 1.15f));
                dust.noGravity = true;
            }

            return true;
        }

        public bool TryStartRoll(Item weapon, int inputDirection)
        {
            if (DashCooldownTimer > 0 || IsRolling || Player.dead || Player.CCed || Player.mount.Active)
                return false;

            // 向后转移：如果玩家面向右(1)，则向左(-1)退；如果面向左(-1)，则向右(1)退
            int rollDir = -Player.direction;
            if (rollDir == 0)
                rollDir = -1;

            RollDirection = rollDir;
            RollTimer = NewLegendP90.RollFrames;
            DashCooldownTimer = NewLegendP90.RollCooldownFrames;
            dodgedHostileProjectileThisRoll = false;
            Player.fullRotationOrigin = Player.Size * 0.5f;
            Player.Calamity().GeneralScreenShakePower = Math.Max(Player.Calamity().GeneralScreenShakePower, 2.4f);

            int damage = Math.Max(1, (int)(Player.GetWeaponDamage(weapon) * 7.5f));
            int hitbox = Projectile.NewProjectile(
                Player.GetSource_ItemUse(weapon),
                Player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<P90RollHitbox>(),
                damage,
                Player.GetWeaponKnockback(weapon) * 4.5f,
                Player.whoAmI);

            if (Main.projectile.IndexInRange(hitbox))
                Main.projectile[hitbox].CritChance = Player.GetWeaponCrit(weapon);

            EnsureCooldownBar();
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.78f, Pitch = 0.18f }, Player.Center);
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.52f, Pitch = 0.35f }, Player.Center);
            SpawnRollDust(18, 3.8f);
            return true;
        }

        public void StopRollOnHit()
        {
            if (IsRolling)
            {
                RollTimer = 0;
                Player.fullRotation = 0f;
            }
        }

        private void UpdateRoll()
        {
            if (RollTimer <= 0)
            {
                Player.fullRotation = 0f;
                return;
            }

            int elapsed = NewLegendP90.RollFrames - RollTimer;
            float progress = elapsed / (float)NewLegendP90.RollFrames;
            Player.fullRotationOrigin = Player.Size * 0.5f;
            // 保持原本朝向，旋转方向跟着退防方向
            Player.fullRotation = RollDirection * MathHelper.TwoPi * 2f * progress * Player.gravDir;
            Player.noKnockback = true;
            Player.immune = true;
            Player.immuneNoBlink = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 2);
            Player.GiveUniversalIFrames(2, false);
            Player.fallStart = (int)(Player.position.Y / 16f);

            // 迅速向后突进
            Player.velocity.X = RollDirection * 18.5f;
            if (Player.velocity.Y > 1.2f)
                Player.velocity.Y = 1.2f;

            TryRegisterHostileProjectileDodge();

            if (Main.rand.NextBool(2))
            {
                Dust trail = Dust.NewDustPerfect(
                    Player.Center - Vector2.UnitX * RollDirection * Main.rand.NextFloat(10f, 26f) + Main.rand.NextVector2Circular(12f, 16f),
                    Main.rand.NextBool() ? DustID.GoldCoin : DustID.FireworkFountain_Yellow,
                    -Vector2.UnitX * RollDirection * Main.rand.NextFloat(1.4f, 4.4f),
                    110,
                    Main.rand.NextBool() ? Color.Gold : new Color(255, 84, 84),
                    Main.rand.NextFloat(0.8f, 1.35f));
                trail.noGravity = true;
            }

            RollTimer--;
            if (RollTimer <= 0)
            {
                Player.fullRotation = 0f;
                if (dodgedHostileProjectileThisRoll)
                {
                    DodgeEmpowerTimer = NewLegendP90.DodgeEmpowerFrames;
                    dodgedHostileProjectileThisRoll = false;
                }

                SpawnRollDust(12, 2.8f);
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.42f, Pitch = 0.24f }, Player.Center);
            }
        }

        private void TryRegisterHostileProjectileDodge()
        {
            if (dodgedHostileProjectileThisRoll || Main.myPlayer != Player.whoAmI)
                return;

            Rectangle playerHitbox = Player.Hitbox;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                TryRegisterHostileProjectileDodge(projectile, playerHitbox);
                if (dodgedHostileProjectileThisRoll)
                    break;
            }
        }

        internal void TryRegisterHostileProjectileDodge(Projectile projectile)
        {
            TryRegisterHostileProjectileDodge(projectile, Player.Hitbox);
        }

        private void TryRegisterHostileProjectileDodge(Projectile projectile, Rectangle playerHitbox)
        {
            if (!IsRolling || dodgedHostileProjectileThisRoll || Main.myPlayer != Player.whoAmI)
                return;
            if (!projectile.active || !projectile.hostile || projectile.damage <= 0)
                return;
            if (ProjectileLoader.CanDamage(projectile) == false || !ProjectileLoader.CanHitPlayer(projectile, Player))
                return;
            if (!projectile.Colliding(projectile.Hitbox, playerHitbox))
                return;

            dodgedHostileProjectileThisRoll = true;
        }

        private void SpawnEmpowerEndContraction()
        {
            if (Main.dedServ || Main.myPlayer != Player.whoAmI)
                return;

            const int dustCount = 12;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 outward = (MathHelper.TwoPi * i / dustCount).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(
                    Player.Center + outward * 30f,
                    DustID.GoldFlame,
                    -outward * 3.4f,
                    90,
                    Color.Gold,
                    Main.rand.NextFloat(0.65f, 0.9f));
                dust.noGravity = true;
            }
        }

        private void CompleteReload()
        {
            LoadedProjectileType = pendingReload.ProjectileType;
            LoadedAmmoItemType = pendingReload.AmmoItemType;
            LoadedDamageOffset = pendingReload.DamageOffset;
            LoadedShootSpeed = pendingReload.ShootSpeed;
            LoadedKnockback = pendingReload.Knockback;
            Magazine = NewLegendP90.MagazineCapacity;
            SoundEngine.PlaySound(SoundID.Grab with { Volume = 0.55f, Pitch = 0.28f }, Player.Center);
        }

        private void ResetMagazineToDefault()
        {
            Magazine = NewLegendP90.MagazineCapacity;
            LoadedProjectileType = ModContent.ProjectileType<P90DefenseRound>();
            LoadedAmmoItemType = ItemID.MusketBall;
            LoadedDamageOffset = 0;
            LoadedShootSpeed = 9f;
            LoadedKnockback = 1.5f;
            pendingReload = new ReloadData(LoadedProjectileType, LoadedAmmoItemType, LoadedDamageOffset, LoadedShootSpeed, LoadedKnockback, true);
        }

        private bool TryBuildReloadData(Player player, Item weapon, out ReloadData data)
        {
            data = default;

            if (!player.PickAmmo(weapon, out int projectileType, out float speed, out int damage, out float knockback, out int ammoItemType, true))
                return false;

            Item ammo = FindAmmoItem(player, ammoItemType);
            if (ammo == null)
                return false;

            bool infinite = IsInfiniteAmmo(ammo);
            bool musketAmmo = IsMusketAmmo(ammo.type);
            int finalProjectileType = musketAmmo ? ModContent.ProjectileType<P90DefenseRound>() : projectileType;
            if (finalProjectileType <= ProjectileID.None)
                finalProjectileType = ProjectileID.Bullet;

            int damageOffset = Math.Max(-player.GetWeaponDamage(weapon) + 1, damage - player.GetWeaponDamage(weapon));
            data = new ReloadData(
                finalProjectileType,
                ammo.type,
                damageOffset,
                Math.Max(4f, speed),
                Math.Max(0f, knockback),
                infinite);
            return true;
        }

        private static Item FindAmmoItem(Player player, int ammoItemType)
        {
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item != null && !item.IsAir && item.type == ammoItemType && item.ammo == AmmoID.Bullet)
                    return item;
            }

            return null;
        }

        private static bool ConsumeAmmoForReload(Player player, int ammoItemType, int amount)
        {
            int available = 0;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item != null && !item.IsAir && item.type == ammoItemType)
                    available += item.stack;
            }

            if (available < amount)
                return false;

            int remaining = amount;
            for (int i = 0; i < player.inventory.Length && remaining > 0; i++)
            {
                Item item = player.inventory[i];
                if (item == null || item.IsAir || item.type != ammoItemType)
                    continue;

                int taken = Math.Min(item.stack, remaining);
                item.stack -= taken;
                remaining -= taken;
                if (item.stack <= 0)
                    item.TurnToAir();
            }

            return true;
        }

        private static bool IsInfiniteAmmo(Item ammo)
        {
            return ammo.type == ItemID.EndlessMusketPouch || !ammo.consumable;
        }

        private static bool IsMusketAmmo(int ammoItemType)
        {
            return ammoItemType == ItemID.MusketBall || ammoItemType == ItemID.EndlessMusketPouch;
        }

        private void EnsureCooldownBar()
        {
            if (Main.myPlayer != Player.whoAmI || DashCooldownTimer <= 0)
                return;

            int barType = ModContent.ProjectileType<P90CooldownBar>();
            if (Player.ownedProjectileCounts[barType] > 0)
                return;

            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, barType, 0, 0f, Player.whoAmI);
        }

        private void SpawnRollDust(int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(speed * 0.35f, speed);
                Dust dust = Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(14f, 18f),
                    Main.rand.NextBool() ? DustID.GoldFlame : DustID.FireworkFountain_Red,
                    velocity,
                    100,
                    Main.rand.NextBool() ? Color.Gold : new Color(255, 84, 84),
                    Main.rand.NextFloat(0.8f, 1.35f));
                dust.noGravity = true;
            }
        }

        private readonly struct ReloadData
        {
            public readonly int ProjectileType;
            public readonly int AmmoItemType;
            public readonly int DamageOffset;
            public readonly float ShootSpeed;
            public readonly float Knockback;
            public readonly bool Infinite;

            public ReloadData(int projectileType, int ammoItemType, int damageOffset, float shootSpeed, float knockback, bool infinite)
            {
                ProjectileType = projectileType;
                AmmoItemType = ammoItemType;
                DamageOffset = damageOffset;
                ShootSpeed = shootSpeed;
                Knockback = knockback;
                Infinite = infinite;
            }
        }
    }
}
