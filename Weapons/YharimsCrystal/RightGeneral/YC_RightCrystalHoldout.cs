using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
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
        private readonly BalanceYharimsCrystal balance = new();
        private int manaDrainTimer;
        private int laserIndex = -1;
        private int shardIndex = -1;
        private bool chargedSoundPlayed;
        private bool heavyAttackQueued;

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

            DrainManaOrKill();
            EmitChargeFX();
            MaintainEmpoweredShard();

            if (Projectile.owner == Main.myPlayer && WantsHeavyAttack())
                heavyAttackQueued = true;

            // Spawn drones one by one at frames 10, 20, 30, 40, 50, 60
            if (Projectile.owner == Main.myPlayer)
            {
                if (HoldFrameCounter == 10 || HoldFrameCounter == 20 || HoldFrameCounter == 30 ||
                    HoldFrameCounter == 40 || HoldFrameCounter == 50 || HoldFrameCounter == 60)
                {
                    int slot = (int)(HoldFrameCounter / 10) - 1;
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

            // Laser convergence explosion at frame 120
            if (HoldFrameCounter == 120)
            {
                Vector2 convergencePoint = Owner.MountedCenter + ForwardDirection * 240f;
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f, Pitch = -0.1f }, convergencePoint);

                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(convergencePoint, Vector2.Zero, Color.Orange * 0.8f, "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.08f, 0.95f, 22, true));
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(convergencePoint, Vector2.Zero, Color.Gold, Vector2.One, ForwardDirection.ToRotation(), 0.16f, 2.1f, 22));
                    for (int i = 0; i < 24; i++)
                    {
                        Dust d = Dust.NewDustPerfect(convergencePoint, DustID.GoldFlame, Main.rand.NextVector2Circular(8f, 8f), 0, default, 1.4f);
                        d.noGravity = true;
                    }
                }

                if (Projectile.owner == Main.myPlayer)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.active && !npc.friendly && npc.chaseable && !npc.dontTakeDamage)
                        {
                            if (Vector2.Distance(npc.Center, convergencePoint) < 120f)
                            {
                                npc.SimpleStrikeNPC((int)(Projectile.damage * 1.5f), Projectile.direction, true, Projectile.knockBack, DamageClass.Magic);
                            }
                        }
                    }
                }
            }

            if (Charged)
            {
                if (!chargedSoundPlayed)
                {
                    chargedSoundPlayed = true;
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.62f, Pitch = -0.18f }, Projectile.Center);
                    Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 3.8f);
                }

                EnsureLaser();
            }

            if (HoldFrameCounter >= 120)
            {
                // Trigger heavy command on left click, including clicks queued during deployment.
                if (Projectile.owner == Main.myPlayer && heavyAttackQueued)
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile other = Main.projectile[i];
                        if (other.active && other.owner == Projectile.owner && other.type == ModContent.ProjectileType<YC_RightDrone>() && (int)other.ai[1] == Projectile.whoAmI)
                        {
                            other.ai[2] = 1f; // Command heavy attack
                            other.netUpdate = true;
                        }
                    }

                    Owner.GetModPlayer<YharimsCrystalStatePlayer>().RightClickCooldown = 300;
                    Projectile.Kill();
                    return;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            KillProjectile(laserIndex);
            KillProjectile(shardIndex);

            // Kill all associated drones
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == Projectile.owner && other.type == ModContent.ProjectileType<YC_RightDrone>() && (int)other.ai[1] == Projectile.whoAmI)
                {
                    if (other.ai[2] == 1f)
                        continue;

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
            // After all drones are deployed, left click also keeps the holdout alive
            // so the player can release right click and press left click without the holdout dying first.
            if (HoldFrameCounter >= 120 &&
                Main.mouseLeft &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface)
            {
                return true;
            }
            return base.IsRightHeld();
        }

        private bool WantsHeavyAttack()
        {
            return Main.mouseLeft &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;
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

        private void EnsureLaser()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            if (IsProjectileActive(laserIndex, ModContent.ProjectileType<YC_RightScorchingLaser>()))
                return;

            laserIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Muzzle,
                ForwardDirection,
                ModContent.ProjectileType<YC_RightScorchingLaser>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI);

            if (Main.projectile.IndexInRange(laserIndex))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[laserIndex], YCWeaponForm.Crystal);
                Main.projectile[laserIndex].CritChance = Projectile.CritChance;
            }
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

        private void DrainManaOrKill()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            manaDrainTimer++;
            if (manaDrainTimer < 12)
                return;

            manaDrainTimer = 0;
            if (Owner.CheckMana(Owner.HeldItem, -1, true))
                return;

            SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.45f }, Owner.Center);
            Projectile.Kill();
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
