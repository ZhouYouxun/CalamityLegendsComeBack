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
        private int previewIndex = -1;

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
                if (Projectile.owner == Main.myPlayer)
                    Owner.GetModPlayer<CallofDutyPlayer>().CloseWheel();
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

            Owner.direction = aim.X >= 0f ? 1 : -1;
            Owner.itemRotation = aim.ToRotation();
            if (Owner.direction < 0)
                Owner.itemRotation += MathHelper.Pi;
            Owner.heldProj = Projectile.whoAmI;

            Projectile.Center = Owner.MountedCenter + aim * 18f + Vector2.UnitY * Owner.gfxOffY;
            Projectile.rotation = aim.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Owner.direction;

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
            Vector2 mouseScreen = new(Main.mouseX, Main.mouseY);

            if (right && !previousRight)
            {
                phonePlayer.OpenWheel(mouseScreen);
                previewIndex = -1;
                SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.45f, Pitch = 0.15f }, Owner.Center);
            }

            if (right && phonePlayer.WheelOpen)
            {
                int hovered = phonePlayer.UpdateWheel(mouseScreen);
                if (hovered >= 0 && hovered != previewIndex)
                {
                    previewIndex = hovered;
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.28f, Pitch = -0.1f + hovered * 0.08f }, Owner.Center);
                }
            }

            if (!right && previousRight && phonePlayer.WheelOpen)
            {
                bool wasInCancelZone = phonePlayer.WheelHoverIndex < 0;
                bool changed = phonePlayer.CommitWheelSelection();
                SoundEngine.PlaySound((changed ? SoundID.MenuOpen : SoundID.MenuClose) with { Volume = 0.5f, Pitch = changed ? 0.25f : -0.05f }, Owner.Center);

                if (wasInCancelZone && phonePlayer.ArmyActive)
                    SendCommand(ResponsibilityCommandMode.Return, Owner.Center, -1);
            }

            if (left && !phonePlayer.WheelOpen && fireCooldown <= 0)
            {
                FireSequence(aim);
                float attackSpeed = Math.Max(0.1f, Owner.GetAttackSpeed(DamageClass.Summon));
                fireCooldown = Math.Max(CallofDuty.MinimumSequenceInterval,
                    (int)MathF.Round(CallofDuty.BaseSequenceInterval / attackSpeed));
            }

            if (left && !previousLeft && phonePlayer.ArmyActive && !phonePlayer.WheelOpen)
            {
                Vector2 mouseWorld = CallofDuty.GetMouseWorld(Owner);
                int target = FindTargetUnderMouse(mouseWorld);
                SendCommand(target >= 0 ? ResponsibilityCommandMode.Attack : ResponsibilityCommandMode.Move, mouseWorld, target);
            }

            previousLeft = left;
            previousRight = right;
        }

        private void FireSequence(Vector2 aim)
        {
            CallofDutyPlayer phonePlayer = Owner.GetModPlayer<CallofDutyPlayer>();
            int sequenceId = phonePlayer.AllocateSequenceId();
            int damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Vector2 muzzle = Projectile.Center + aim * 14f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                muzzle,
                aim,
                ModContent.ProjectileType<ResponsibilityCommunicationSequence>(),
                damage,
                Owner.HeldItem.knockBack,
                Owner.whoAmI,
                phonePlayer.SelectedLanguageIndex,
                sequenceId);

            ResponsibilityLanguageDefinition definition = ResponsibilityLanguageRegistry.Get(phonePlayer.SelectedLanguageIndex);
            if (definition != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust dust = Dust.NewDustPerfect(muzzle, DustID.Electric, aim.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.8f, 2.4f), 90, definition.Color, 0.75f);
                    dust.noGravity = true;
                }
            }
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.38f, Pitch = 0.25f }, muzzle);
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
            Color screenGlow = ResponsibilityLanguageRegistry.Get(Owner.GetModPlayer<CallofDutyPlayer>().SelectedLanguageIndex)?.Color ?? Color.White;

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, lightColor, Projectile.rotation, source.Size() * 0.5f, Projectile.scale, effects);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, screenGlow * 0.18f, Projectile.rotation, source.Size() * 0.5f, Projectile.scale * 1.05f, effects);
            return false;
        }
    }
}
