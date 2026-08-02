using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 大招·复仇印记重炮展开：完全同步后按大招键触发。武器箱完全展开、玩家悬停，
    /// 随后点击左键或右键，锁定光标附近至多五个目标（敌少则一敌多框），齐射一对一追踪炮弹。
    /// </summary>
    public class M4A1UltimateHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/M4A1/InheritedCase";

        private const int ArmDelay = 8;
        private const int Timeout = 300;
        private const int MaxLocks = 5;
        private const float TargetRadius = 520f;
        private const int LingerAfterFire = 34;

        private int state; // 0 = 展开待命, 1 = 已齐射收尾
        private int timer;
        private int lingerTimer;
        private float caseScale;
        private float flash;

        private Player Owner => Main.player[Projectile.owner];
        private InheritedCaseM4A1 Weapon => Owner.HeldItem.ModItem as InheritedCaseM4A1;
        private Vector2 HoverPos => Owner.Top + new Vector2(0f, -46f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 3f);

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item113 with { Volume = 0.9f }, Owner.Center);
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.6f, Pitch = -0.4f }, Owner.Center);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Weapon == null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = HoverPos;
            Owner.heldProj = Projectile.whoAmI;
            KeepUseAnimation();

            caseScale = MathHelper.Lerp(caseScale, 1.5f, 0.18f);
            if (flash > 0f) flash -= 0.06f;

            if (state == 0)
            {
                // 悬停 / 降低移动速度
                Owner.velocity *= 0.82f;
                Owner.itemRotation = 0f;

                timer++;
                bool clicked = Projectile.owner == Main.myPlayer && timer >= ArmDelay && (Main.mouseLeft || Main.mouseRight);
                if (clicked && FireBarrage())
                {
                    state = 1;
                    lingerTimer = LingerAfterFire;
                    flash = 1f;
                }
                else if (timer > Timeout)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else
            {
                Owner.velocity *= 0.9f;
                if (--lingerTimer <= 0)
                {
                    Projectile.Kill();
                    return;
                }
            }

            Projectile.timeLeft = 2;
        }

        private void KeepUseAnimation() => Owner.itemTime = Owner.itemAnimation = 2;

        private bool FireBarrage()
        {
            List<int> slots = BuildLockSlots(InheritedCaseM4A1.GetMouseWorld(Owner));
            if (slots.Count == 0)
                return false;

            int damage = (int)(Owner.GetWeaponDamage(Owner.HeldItem) * BalanceM4A1.GetUltimateShellDamageMultiplier());

            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.9f, Pitch = -0.2f }, Owner.Center);
            M4A1Player.Get(Owner).ConsumeAllForUltimate();

            for (int i = 0; i < slots.Count; i++)
            {
                Projectile.NewProjectile(
                    Owner.GetSource_Misc("M4A1Ultimate"),
                    Main.npc[slots[i]].Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<M4A1LockOnBox>(),
                    0,
                    0f,
                    Projectile.owner,
                    slots[i],
                    i * 5,
                    damage);
            }

            // 展开爆发特效
            if (!Main.dedServ)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(8f, 8f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Projectile.Center, vel, false, 20, Main.rand.NextFloat(0.4f, 0.9f),
                        Color.Lerp(M4A1Visuals.MarkColor, Color.White, Main.rand.NextFloat(0.3f, 0.7f)), true, true));
                }
                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, M4A1Visuals.MarkColor, 1.6f, 24, true));
            }
            return true;
        }

        /// <summary>光标附近至多 5 个锁定槽；敌少时循环分配（一敌多框）。</summary>
        private List<int> BuildLockSlots(Vector2 cursor)
        {
            List<NPC> chaseable = new();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.CanBeChasedBy(Projectile))
                    chaseable.Add(npc);
            }

            List<NPC> all = chaseable.OrderBy(n => Vector2.DistanceSquared(n.Center, cursor)).ToList();
            List<NPC> near = all.Where(n => Vector2.Distance(n.Center, cursor) <= TargetRadius).ToList();
            List<NPC> pool = near.Count > 0 ? near : all;

            List<int> baseTargets = pool.Take(MaxLocks).Select(n => n.whoAmI).ToList();
            List<int> slots = new();
            if (baseTargets.Count == 0)
                return slots;

            for (int i = 0; i < MaxLocks; i++)
                slots.Add(baseTargets[i % baseTargets.Count]);
            return slots;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;

            Color theme = M4A1Visuals.MarkColor;
            float glow = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) + flash;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 发光包边
            Color outline = (Color.Lerp(theme, Color.White, 0.5f) with { A = 0 }) * (0.5f + glow * 0.6f);
            for (int i = 0; i < 16; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * (3f + glow * 3f);
                Main.EntitySpriteDraw(tex, drawCenter + offset, null, outline, 0f, origin, caseScale, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(tex, drawCenter, null, lightColor, 0f, origin, caseScale, SpriteEffects.None, 0);

            // 待命阶段：光标准星 + 候选高亮
            if (state == 0 && Projectile.owner == Main.myPlayer)
                DrawTargetingPreview();

            return false;
        }

        private void DrawTargetingPreview()
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 cursor = InheritedCaseM4A1.GetMouseWorld(Owner);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            Color c = M4A1Visuals.MarkColor * (0.5f + pulse * 0.5f);

            Vector2 cp = cursor - Main.screenPosition;
            Main.spriteBatch.Draw(pixel, new Rectangle((int)cp.X - 1, (int)cp.Y - 9, 2, 18), c);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)cp.X - 9, (int)cp.Y - 1, 18, 2), c);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || Vector2.Distance(npc.Center, cursor) > TargetRadius)
                    continue;

                Rectangle box = new((int)(npc.position.X - Main.screenPosition.X) - 3, (int)(npc.position.Y - Main.screenPosition.Y) - 3, npc.width + 6, npc.height + 6);
                Color b = c * 0.6f;
                Main.spriteBatch.Draw(pixel, new Rectangle(box.X, box.Y, box.Width, 1), b);
                Main.spriteBatch.Draw(pixel, new Rectangle(box.X, box.Bottom - 1, box.Width, 1), b);
                Main.spriteBatch.Draw(pixel, new Rectangle(box.X, box.Y, 1, box.Height), b);
                Main.spriteBatch.Draw(pixel, new Rectangle(box.Right - 1, box.Y, 1, box.Height), b);
            }
        }
    }
}
