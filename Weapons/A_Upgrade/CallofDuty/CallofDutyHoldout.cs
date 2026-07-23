using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty
{
    internal sealed class CallofDutyHoldout : ModProjectile
    {
        private int fireCooldown;
        private bool previousLeft;
        private bool previousRight;

        public override string Texture => "CalamityMod/Items/SummonItems/Invasion/MartianDistressRemote_Animated";

        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 52;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem?.type != ModContent.ItemType<CallofDuty>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Vector2 oldAim = Projectile.velocity;
            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            if (Projectile.owner == Main.myPlayer)
            {
                aim = (CallofDuty.GetMouseWorld(Owner) - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.velocity = aim;
                if (Vector2.Dot(oldAim.SafeNormalize(aim), aim) < 0.998f)
                    Projectile.netUpdate = true;
            }

            Owner.ChangeDir(aim.X >= 0f ? 1 : -1);
            Owner.itemRotation = aim.ToRotation();
            if (Owner.direction < 0)
                Owner.itemRotation += MathHelper.Pi;
            Owner.heldProj = Projectile.whoAmI;

            Projectile.Center = Owner.MountedCenter + aim * 18f + Vector2.UnitY * Owner.gfxOffY;
            Projectile.rotation = aim.ToRotation() + MathHelper.PiOver2;
            Projectile.direction = Owner.direction;
            Projectile.spriteDirection = Owner.direction;

            float armRotation = (aim.ToRotation() - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

            if (fireCooldown > 0)
                fireCooldown--;

            if (Projectile.owner == Main.myPlayer)
                HandleOwnerInput(aim);
        }

        private void HandleOwnerInput(Vector2 aim)
        {
            bool validInput = CallofDuty.CanUseWorldInput(Owner);
            bool left = validInput && Main.mouseLeft;
            bool right = validInput && Main.mouseRight;
            CallofDutyPlayer phonePlayer = Owner.GetModPlayer<CallofDutyPlayer>();
            Vector2 muzzle = Projectile.Center + aim * 14f;
            int damage = Owner.GetWeaponDamage(Owner.HeldItem);

            // 1. Both buttons held: Speed Dial Charge (左右键长按: 快捷拨号)
            if (left && right)
            {
                phonePlayer.BothHoldTimer++;
                Owner.velocity *= 0.96f; // Slightly reduce movement speed during charge

                // LCD charging particles
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(6f, 6f), DustID.Electric, aim * 2f, 100, new Color(132, 226, 255), 0.6f);
                    d.noGravity = true;
                }

                if (phonePlayer.BothHoldTimer % 6 == 0)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.35f, Pitch = -0.2f + phonePlayer.BothHoldTimer * 0.02f }, muzzle);
                }

                // Completed 0.5s hold (30 frames)
                if (phonePlayer.BothHoldTimer >= 30)
                {
                    phonePlayer.BothHoldTimer = 0;
                    fireCooldown = 35;

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        muzzle,
                        aim * 12f,
                        ModContent.ProjectileType<CallofDutyFastDialSignal>(),
                        (int)(damage * 1.25f),
                        Owner.HeldItem.knockBack * 1.5f,
                        Owner.whoAmI);

                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.55f, Pitch = 0.35f }, muzzle);
                }
            }
            // 2. Right click: Redial (右键: 重拨)
            else if (right && !left)
            {
                phonePlayer.BothHoldTimer = 0;

                if (!previousRight && fireCooldown <= 0)
                {
                    int redialTarget = phonePlayer.RedialTarget;
                    bool hasValidTarget = redialTarget >= 0 && Main.npc.IndexInRange(redialTarget) && Main.npc[redialTarget].CanBeChasedBy();
                    bool canRedial = hasValidTarget && phonePlayer.RedialCooldownTimer <= 0;

                    if (canRedial)
                    {
                        phonePlayer.RedialCooldownTimer = 150; // 2.5s CD
                        fireCooldown = 20;

                        bool isFastDialBoosted = phonePlayer.FastDialPriorityTarget == redialTarget;
                        int redialDamage = (int)(damage * (isFastDialBoosted ? 1.85f : 1.5f));

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Main.npc[redialTarget].Center,
                            Vector2.Zero,
                            ModContent.ProjectileType<CallofDutyRedialKeyboard>(),
                            redialDamage,
                            Owner.HeldItem.knockBack,
                            Owner.whoAmI,
                            redialTarget,
                            isFastDialBoosted ? 1f : 0f);

                        SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.5f, Pitch = 0.2f }, muzzle);
                    }
                    else
                    {
                        // Invalid target or on cooldown - play error tone
                        fireCooldown = 15;
                        SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.4f, Pitch = -0.35f }, muzzle);
                    }
                }
            }
            // 3. Left click: Dial sequence (左键: 拨号 1-2-3 脉冲)
            else if (left && !right)
            {
                phonePlayer.BothHoldTimer = 0;

                if (fireCooldown <= 0)
                {
                    int sequenceId = phonePlayer.AllocateSequenceId();
                    float attackSpeed = Math.Max(0.1f, Owner.GetAttackSpeed(DamageClass.Summon));
                    fireCooldown = Math.Max(CallofDuty.MinimumSequenceInterval, (int)MathF.Round(CallofDuty.BaseSequenceInterval / attackSpeed));

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        muzzle,
                        aim,
                        ModContent.ProjectileType<ResponsibilityCommunicationSequence>(),
                        damage,
                        Owner.HeldItem.knockBack,
                        Owner.whoAmI,
                        0,
                        sequenceId);
                }

                // Army command on left click
                if (!previousLeft && phonePlayer.ArmyActive)
                {
                    Vector2 mouseWorld = CallofDuty.GetMouseWorld(Owner);
                    int target = FindTargetUnderMouse(mouseWorld);
                    SendCommand(target >= 0 ? ResponsibilityCommandMode.Attack : ResponsibilityCommandMode.Move, mouseWorld, target);
                }
            }
            else
            {
                phonePlayer.BothHoldTimer = 0;
            }

            previousLeft = left;
            previousRight = right;
        }

        private static int FindTargetUnderMouse(Vector2 mouseWorld)
        {
            int result = -1;
            float bestDistance = 48f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;
                float distance = npc.Distance(mouseWorld);
                if (npc.Hitbox.Contains(mouseWorld.ToPoint()) || distance < bestDistance)
                {
                    bestDistance = distance;
                    result = npc.whoAmI;
                }
            }
            return result;
        }

        private void SendCommand(ResponsibilityCommandMode mode, Vector2 position, int target)
        {
            Owner.GetModPlayer<CallofDutyPlayer>().SetCommand(mode, position, target);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                CallofDutyPackets.SendCommand(mode, position, target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / 12;
            int frame = (int)(Main.GameUpdateCount / 5 % 12);
            Rectangle source = new(0, frame * frameHeight, texture.Width, frameHeight);
            SpriteEffects effects = Owner.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color screenGlow = new Color(132, 226, 255);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, lightColor, Projectile.rotation, source.Size() * 0.5f, Projectile.scale, effects);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, screenGlow * 0.22f, Projectile.rotation, source.Size() * 0.5f, Projectile.scale * 1.05f, effects);
            return false;
        }
    }
}
