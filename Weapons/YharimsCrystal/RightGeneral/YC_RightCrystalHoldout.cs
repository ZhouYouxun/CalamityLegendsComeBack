using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    internal sealed class YC_RightCrystalHoldout : YC_BaseHoldout
    {
        private const int CoordinatedShutdownFrames = 120;
        private const float OmicronFireRate = 15f;
        private const float OmicronStarterWindup = 60f;

        private readonly BalanceYharimsCrystal balance = new();
        private int shardIndex = -1;
        private bool heavyAttackQueued;
        private bool leftHeldLastFrame;
        private int coordinatedCooldown;
        private int rightInputGraceFrames;
        private float shootingTimer;
        private float postFireCooldown;
        private float maxFireRateShots;
        private float windup = OmicronStarterWindup;

        public float ChargeRatio => MathHelper.Clamp(HoldFrameCounter / balance.GetRightChargeFrames(), 0f, 1f);
        public bool Charged => HoldFrameCounter >= balance.GetRightChargeFrames();
        public Vector2 Muzzle => Projectile.Center + ForwardDirection * 32f;

        protected override float HoldoutDistance => 12f;
        protected override float SoundPitch => 0.14f;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Owner.GetModPlayer<YharimsCrystalStatePlayer>().SetLastWeapon(YCWeaponForm.Crystal);
            Projectile.scale = 1.06f;
            SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.5f, Pitch = -0.25f }, Owner.Center);
        }

        protected override void OnHoldoutAI()
        {
            Projectile.damage = Owner.HeldItem.ModItem is NewLegendYharimsCrystal item
                ? item.GetScaledDamage(Owner, balance.GetRightClickBaseDamage())
                : Projectile.damage;

            EmitChargeFX();
            MaintainEmpoweredShard();

            if (Projectile.owner == Main.myPlayer)
            {
                bool leftHeld = WantsHeavyAttack();
                if (leftHeld && !leftHeldLastFrame)
                {
                    heavyAttackQueued = true;
                    Projectile.netUpdate = true;
                }
                leftHeldLastFrame = leftHeld;
            }

            if (postFireCooldown > 0f)
                PostFiringCooldown();

            if (Projectile.owner == Main.myPlayer && heavyAttackQueued && postFireCooldown <= 0f && coordinatedCooldown <= 0)
            {
                if (Owner.CheckMana(Owner.HeldItem, (int)(Owner.HeldItem.mana * Owner.manaCost) * 13, true))
                {
                    postFireCooldown = 100f;
                    ShootOmicronStyle(true);
                    CommandDrones();
                    shootingTimer = 0f;
                    coordinatedCooldown = CoordinatedShutdownFrames;
                    Projectile.netUpdate = true;
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.5f }, Projectile.Center);
                    shootingTimer = 0f;
                }

                heavyAttackQueued = false;
            }
            else if (shootingTimer >= OmicronFireRate && postFireCooldown <= 0f)
            {
                if (Owner.CheckMana(Owner.HeldItem, -1, true))
                {
                    maxFireRateShots++;
                    if (maxFireRateShots == 5f)
                    {
                        windup = OmicronStarterWindup;
                        maxFireRateShots = 1f;
                    }

                    ShootOmicronStyle(false);
                    shootingTimer = 0f;

                    if (windup > 10f && maxFireRateShots > 0f)
                        windup -= 12f;
                    else
                        windup = 10f;
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.5f }, Owner.Center);
                    Projectile.Kill();
                    return;
                }
            }

            shootingTimer++;
            if (coordinatedCooldown > 0)
                coordinatedCooldown--;

            if (Projectile.owner == Main.myPlayer)
            {
                if (HoldFrameCounter == 1 || HoldFrameCounter == 13)
                {
                    int slot = HoldFrameCounter == 1 ? 0 : 1;
                    int drone = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        ForwardDirection,
                        ModContent.ProjectileType<YC_RightDrone>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        slot,
                        Projectile.whoAmI,
                        0f);
                    if (Main.projectile.IndexInRange(drone))
                    {
                        YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[drone], YCWeaponForm.Crystal);
                    }
                    SoundEngine.PlaySound(SoundID.Item76 with { Volume = 0.35f, Pitch = 0.2f }, Projectile.Center);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            KillProjectile(shardIndex);

            // Kill all associated drones
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == Projectile.owner && other.type == ModContent.ProjectileType<YC_RightDrone>() && (int)other.ai[1] == Projectile.whoAmI)
                {
                    other.Kill();
                }
            }

            // 右键holdout结束后短暂阻止左键holdout生成，防止autoReuse立刻切回左键形态
            if (Projectile.owner == Main.myPlayer)
            {
                YharimsCrystalStatePlayer state = Main.player[Projectile.owner].GetModPlayer<YharimsCrystalStatePlayer>();
                state.LeftClickCooldown = Math.Max(state.LeftClickCooldown, 20);
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.42f, Pitch = -0.15f }, Projectile.Center);
        }

        protected override bool IsRightHeld()
        {
            bool rightHeld = (Main.mouseRight || Owner.Calamity().mouseRight || Owner.controlUseTile) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;

            if (rightHeld)
            {
                rightInputGraceFrames = 18;
                return true;
            }

            if (WantsHeavyAttack())
            {
                rightInputGraceFrames = Math.Max(rightInputGraceFrames, 12);
                return true;
            }

            if (rightInputGraceFrames > 0)
            {
                rightInputGraceFrames--;
                return true;
            }

            return false;
        }

        private bool WantsHeavyAttack()
        {
            return Main.mouseLeft &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;
        }

        private void ShootOmicronStyle(bool yBeam)
        {
            Vector2 shootDirection = ForwardDirection;
            Vector2 firingVelocity = shootDirection * 10f;

            if (yBeam)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/OmicronBeam") { Volume = 0.9f }, Projectile.Center);

                if (!Main.dedServ)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        Particle pulse = new GlowSparkParticle(Muzzle, shootDirection * 28f, false, 8, 0.087f,
                            Color.MediumVioletRed, new Vector2(2.3f, 0.9f), true);
                        GeneralParticleHandler.SpawnParticle(pulse);
                    }
                }

                Owner.SetScreenshake(6.5f);
                if (Main.myPlayer == Projectile.owner)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Muzzle, firingVelocity,
                        ModContent.ProjectileType<OmicronBeam>(), Projectile.damage * 32,
                        Projectile.knockBack, Projectile.owner);
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserBigShot")
                { Volume = 0.2f, Pitch = 0.9f }, Projectile.Center);

                if (Main.myPlayer == Projectile.owner)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 shotVelocity = (shootDirection * 10f).RotatedBy((0.035f * (i + 1)) * Utils.GetLerpValue(0f, 55f, windup, true));
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Muzzle, shotVelocity * (1f - i * 0.1f),
                            ModContent.ProjectileType<WingmanShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 2f);
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 shotVelocity = (shootDirection * 10f).RotatedBy((-0.035f * (i + 1)) * Utils.GetLerpValue(0f, 55f, windup, true));
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Muzzle, shotVelocity * (1f - i * 0.1f),
                            ModContent.ProjectileType<WingmanShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 2f);
                    }
                }

                if (!Main.dedServ)
                {
                    Particle pulse = new GlowSparkParticle(Muzzle, shootDirection * 18f, false, 6, 0.057f,
                        Color.MediumVioletRed, new Vector2(1.7f, 0.8f), true);
                    GeneralParticleHandler.SpawnParticle(pulse);
                }
            }

            if (Main.dedServ)
                return;

            for (int k = 0; k < 10; k++)
            {
                Vector2 shootVel = (shootDirection * 15f).RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 1.8f);
                Dust dust = Dust.NewDustPerfect(Muzzle, Main.rand.NextBool(4) ? 267 : 66, shootVel);
                dust.scale = Main.rand.NextFloat(1.15f, 1.45f);
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.Lerp(Color.MediumVioletRed, Color.White, 0.5f) : Color.MediumVioletRed;
            }
        }

        private void CommandDrones()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active &&
                    other.owner == Projectile.owner &&
                    other.type == ModContent.ProjectileType<YC_RightDrone>() &&
                    (int)other.ai[1] == Projectile.whoAmI)
                {
                    other.ai[2] = 1f;
                    other.netUpdate = true;
                }
            }
        }

        private void PostFiringCooldown()
        {
            Owner.channel = true;
            if (Main.dedServ || !Main.rand.NextBool())
            {
                shootingTimer = 0f;
                postFireCooldown--;
                return;
            }

            Vector2 smokeVel = new Vector2(0f, -8f) * Main.rand.NextFloat(0.1f, 1.1f);
            GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Muzzle, smokeVel, Color.MediumVioletRed,
                Main.rand.Next(30, 51), Main.rand.NextFloat(0.1f, 0.4f), 0.5f,
                Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextBool(), required: true));

            Dust dust = Dust.NewDustPerfect(Muzzle, DustID.SteampunkSteam, smokeVel.RotatedByRandom(0.1f), 80,
                default, Main.rand.NextFloat(0.2f, 0.8f));
            dust.noGravity = false;
            dust.color = Color.MediumVioletRed;

            shootingTimer = 0f;
            postFireCooldown--;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);

            if (Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 drawPosition = Muzzle - Main.screenPosition;
            Color orange = new Color(255, 112, 34, 0);
            Color gold = new Color(255, 218, 94, 0);
            float charge = ChargeRatio;
            float pulse = 1f + (float)Math.Sin(HoldFrameCounter * 0.22f) * 0.08f;

            Main.EntitySpriteDraw(bloom, drawPosition, null, orange * (0.28f + charge * 0.56f), Projectile.rotation, bloom.Size() * 0.5f, (0.12f + charge * 0.24f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, Color.White with { A = 0 } * (0.08f + charge * 0.28f), Projectile.rotation, bloom.Size() * 0.5f, (0.04f + charge * 0.08f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, gold * charge * 0.58f, Main.GlobalTimeWrappedHourly * 1.7f, ring.Size() * 0.5f, (0.12f + charge * 0.22f) * pulse, SpriteEffects.None);
            return false;
        }

        private void MaintainEmpoweredShard()
        {
            bool empowered = Owner.GetModPlayer<YharimsCrystalStatePlayer>().CrystalEmpowered;
            int shardType = ModContent.ProjectileType<YC_BurningShard>();

            if (!empowered)
            {
                KillProjectile(shardIndex);
                shardIndex = -1;
                return;
            }

            if (Projectile.owner != Main.myPlayer || IsProjectileActive(shardIndex, shardType))
                return;

            shardIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Muzzle + ForwardDirection * 22f,
                Vector2.Zero,
                shardType,
                Math.Max(1, (int)(Projectile.damage * 0.38f)),
                Projectile.knockBack * 0.2f,
                Projectile.owner,
                1f,
                Projectile.whoAmI);

            if (Main.projectile.IndexInRange(shardIndex))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[shardIndex], YCWeaponForm.Crystal);
                Main.projectile[shardIndex].CritChance = Projectile.CritChance;
            }
        }

        private void EmitChargeFX()
        {
            if (Main.dedServ)
                return;

            float charge = ChargeRatio;
            int interval = Math.Max(2, (int)MathHelper.Lerp(8f, 2f, charge));
            if (Main.GameUpdateCount % interval == 0)
            {
                Vector2 direction = ForwardDirection;
                Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
                Vector2 spawn = Muzzle + normal * Main.rand.NextFloat(-36f, 36f) - direction * Main.rand.NextFloat(18f, 74f);
                Vector2 velocity = direction * Main.rand.NextFloat(2f, 6f + charge * 6f) + normal * Main.rand.NextFloat(-1.1f, 1.1f);
                Color color = Main.rand.NextBool(4) ? Color.White : Color.Lerp(new Color(255, 72, 34), new Color(255, 222, 94), Main.rand.NextFloat(0.1f, 0.9f));

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    spawn,
                    velocity,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.22f, 0.46f) * (0.55f + charge),
                    color,
                    new Vector2(0.72f, 1.15f),
                    shrinkSpeed: 0.85f));
            }

            if ((int)HoldFrameCounter % 24 == 0)
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.16f + charge * 0.14f, Pitch = -0.28f, PitchVariance = 0.18f, MaxInstances = 5 }, Muzzle);
        }

        private static bool IsProjectileActive(int index, int type)
        {
            return index >= 0 &&
                index < Main.maxProjectiles &&
                Main.projectile[index].active &&
                Main.projectile[index].type == type;
        }

        private static void KillProjectile(int index)
        {
            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].active)
                Main.projectile[index].Kill();
        }

        protected override void UpdateHoldout()
        {
            Vector2 holdoutCenter = Owner.RotatedRelativePoint(Owner.MountedCenter, true);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 targetAim = (NewLegendYharimsCrystal.GetMouseWorld(Owner) - holdoutCenter).SafeNormalize(Vector2.UnitX * Owner.direction);

                if (Projectile.velocity == Vector2.Zero)
                {
                    Projectile.velocity = targetAim;
                }
                else
                {
                    // 2 degrees per frame turn rate limit
                    float maxTurn = MathHelper.ToRadians(2f);
                    float currentAngle = Projectile.velocity.ToRotation();
                    float targetAngle = targetAim.ToRotation();
                    float newAngle = currentAngle.AngleTowards(targetAngle, maxTurn);
                    Vector2 newAim = newAngle.ToRotationVector2();

                    if (newAim != Projectile.velocity)
                        Projectile.netUpdate = true;

                    Projectile.velocity = newAim;
                }
            }

            Projectile.Center = holdoutCenter + ForwardDirection * HoldoutDistance;
            Projectile.rotation = ForwardDirection.ToRotation() + MathHelper.PiOver2;
            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;

            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Projectile.timeLeft = 2;

            if (Projectile.soundDelay <= 0 && HoldFrameCounter > 1f)
            {
                Projectile.soundDelay = 22; // SoundInterval
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.13f, Pitch = SoundPitch, MaxInstances = 4 }, Projectile.Center);
            }
        }
    }
}
