using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 右键举盾holdout。
    // Phase 0（0-14帧）：举盾过渡 = 完美格挡窗口。
    // Phase 1（15帧+）：盾已完全举起，提供防御加成；松开右键释放幻影。
    public class AegisShieldHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/庇护盾牌";

        private Player Owner => Main.player[Projectile.owner];
        private AegisBladePlayer BladePlayer => Owner.GetModPlayer<AegisBladePlayer>();

        private int phase = 0;     // 0=举起中, 1=已举起
        private int phaseTimer = 0;
        private bool perfectParryFired = false;   // 本次举盾是否已触发过完美格挡
        private float displayScale = 0f;

        private static readonly Color GoldColor = new(255, 200, 60);
        private static readonly Color GoldOutline = new(255, 235, 140);

        // 从 SetDefaults 外传入（在 AI 首帧由 item 写入）
        private readonly BalanceAegisBlade balance = new();

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 70;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
        }

        private bool IsRightHeld()
        {
            // 非本地玩家：holdout 由其 owner 客户端的 netUpdate 控制寿命
            if (Main.myPlayer != Projectile.owner)
                return true;
            return Owner.Calamity().mouseRight
                   && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 4;
            phaseTimer++;

            // ── Phase 0：举盾中（完美格挡窗口） ──────────────────────────
            if (phase == 0)
            {
                float raiseProg = (float)phaseTimer / BalanceAegisBlade.ShieldRaiseFrames;
                displayScale = MathHelper.Lerp(0.3f, 1f, CalamityUtils.SineInOutEasing(raiseProg, 1));

                // 告知 AegisBladePlayer 当前处于举盾窗口
                BladePlayer.ShieldRaising = true;
                BladePlayer.ShieldRaised = false;

                // 检测是否在此期间被攻击（由 AegisBladePlayer.ModifyHurt 写入标志）
                if (!perfectParryFired && BladePlayer.WasHurtDuringRaise)
                {
                    perfectParryFired = true;
                    OnPerfectParry();
                }

                if (phaseTimer >= BalanceAegisBlade.ShieldRaiseFrames)
                {
                    phase = 1;
                    phaseTimer = 0;
                }

                // 举盾期间松开右键：直接取消
                if (!IsRightHeld())
                {
                    Projectile.Kill();
                    return;
                }
            }

            // ── Phase 1：盾已举起 ────────────────────────────────────────
            else
            {
                displayScale = MathHelper.Lerp(displayScale, 1f, 0.2f);

                BladePlayer.ShieldRaised = true;
                BladePlayer.ShieldRaising = false;

                // 粒子环绕盾
                if (!Main.dedServ && Main.rand.NextBool(5))
                    EmitIdleParticle();

                // 松开右键：释放盾牌幻影
                if (!IsRightHeld())
                {
                    if (Main.myPlayer == Projectile.owner)
                        ReleasePhantom(perfectParryFired);
                    Projectile.Kill();
                    return;
                }
            }

            UpdateShieldPosition();
        }

        private void UpdateShieldPosition()
        {
            // 盾放在玩家面向方向的后方
            int backDir = -Owner.direction;
            Vector2 shieldOffset = new Vector2(backDir * 20f, 0f);
            Projectile.Center = Owner.Center + shieldOffset;
            Projectile.rotation = -MathHelper.PiOver4 * Owner.direction;

            // 锁定玩家视觉
            Owner.heldProj = Projectile.whoAmI;
        }

        private void OnPerfectParry()
        {
            if (Main.dedServ) return;

            SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 1f, Pitch = 0.3f }, Owner.Center);

            // 金色闪光
            for (int i = 0; i < 16; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 10f);
                Dust d = Dust.NewDustPerfect(Owner.Center, DustID.GoldFlame, vel, 0, GoldOutline, 1.5f);
                d.noGravity = true;
            }
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Owner.Center, Vector2.Zero,
                GoldColor, Vector2.One, 0f, 0.05f, 1.2f, 20));

            CombatText.NewText(new Rectangle((int)Owner.Center.X, (int)Owner.Center.Y - 30, 1, 1),
                GoldOutline, "完美格挡！", true);
        }

        private void ReleasePhantom(bool isPerfect)
        {
            Vector2 mouse = Owner.Calamity().mouseWorld;
            if (mouse == Vector2.Zero) mouse = Main.MouseWorld;
            Vector2 dir = Owner.Center.DirectionTo(mouse).SafeNormalize(Vector2.UnitX * Owner.direction);

            // 有BOSS时锁定BOSS
            NPC bossTarget = FindNearestBoss();
            if (bossTarget != null)
                dir = Owner.Center.DirectionTo(bossTarget.Center).SafeNormalize(dir);

            int phantomDamage = balance.GetShieldPhantomDamage();

            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                Owner.Center,
                dir * 18f,
                ModContent.ProjectileType<AegisShieldPhantom>(),
                phantomDamage,
                8f,
                Projectile.owner,
                isPerfect ? 1f : 0f);

            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.9f, Pitch = isPerfect ? 0.3f : 0.1f }, Owner.Center);
        }

        private NPC FindNearestBoss()
        {
            float bestDist = 600f;
            NPC best = null;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.boss || !npc.CanBeChasedBy()) continue;
                float dist = Owner.Distance(npc.Center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = npc;
                }
            }
            return best;
        }

        private void EmitIdleParticle()
        {
            Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(20f, 20f);
            Vector2 vel = -Vector2.UnitY * Main.rand.NextFloat(0.5f, 1.5f);
            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(pos, vel, false,
                Main.rand.Next(10, 18), 0.03f, GoldColor, new Vector2(1f, 0.3f), true, false, 0.7f));
        }

        // ── 绘制 ──────────────────────────────────────────────────────────

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D shieldTex = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation;
            float drawScale = displayScale;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 完美格挡触发后的金色描边
            if (perfectParryFired)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f + Main.GlobalTimeWrappedHourly * 2f;
                    Vector2 offset = angle.ToRotationVector2() * 4f;
                    Main.EntitySpriteDraw(shieldTex, drawPos + offset, null,
                        GoldOutline with { A = 0 } * 0.5f,
                        rotation, shieldTex.Size() * 0.5f, drawScale, SpriteEffects.None, 0);
                }
            }

            // 举盾时光晕
            float pulseGlow = 0.3f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4f);
            Main.EntitySpriteDraw(bloom, drawPos, null,
                GoldColor with { A = 0 } * pulseGlow * drawScale,
                0f, bloom.Size() * 0.5f, drawScale * 1.0f, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // 盾本体（正常渲染）
            Main.EntitySpriteDraw(shieldTex, drawPos, null, lightColor,
                rotation, shieldTex.Size() * 0.5f, drawScale, SpriteEffects.None, 0);

            // 防御进度UI条（显示在玩家头顶）
            if (Main.myPlayer == Projectile.owner)
                DrawDefenseBar();

            return false;
        }

        private void DrawDefenseBar()
        {
            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            float progress = phase == 0
                ? (float)phaseTimer / BalanceAegisBlade.ShieldRaiseFrames
                : 1f;

            Vector2 drawPos = Owner.Center - Main.screenPosition + new Vector2(-barBG.Width * 0.75f, -56f);
            Rectangle frame = new Rectangle(0, 0, (int)(progress * barFG.Width), barFG.Height);
            Color barColor = perfectParryFired ? GoldOutline : GoldColor;

            Main.spriteBatch.Draw(barBG, drawPos, barColor * 0.9f);
            Main.spriteBatch.Draw(barFG, drawPos, frame, barColor * 0.75f);
        }
    }
}
