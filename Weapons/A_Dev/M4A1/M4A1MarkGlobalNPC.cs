using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 复仇印记：连续命中同一敌人累积，最多三层。
    /// 三层时周期性引发小型战术爆破；印记随时间褪去。头顶绘制锁定框 + 印记点。
    /// 左键增益（伤害/破甲）在子弹里读取；右键增益（追踪/范围/强化爆炸）在炮弹里读取。
    /// </summary>
    public class M4A1MarkGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int MarkLevel { get; private set; }
        private int hitProgress;
        private int lifeTimer;
        private int detonationTimer;
        private int owner = -1;
        private int lastHitDamage;
        private int flashTimer;

        public bool HasMark => MarkLevel > 0;

        public static M4A1MarkGlobalNPC Of(NPC npc) => npc.GetGlobalNPC<M4A1MarkGlobalNPC>();

        /// <summary>由 M4A1 弹幕命中时调用：累计印记进度、必要时升层、刷新存活计时。</summary>
        public static void RegisterHit(NPC npc, Player player, int projectileDamage)
        {
            if (npc == null || !npc.active || npc.friendly || npc.dontTakeDamage)
                return;

            M4A1MarkGlobalNPC mark = Of(npc);
            mark.owner = player.whoAmI;
            mark.lastHitDamage = Math.Max(mark.lastHitDamage, projectileDamage);

            int stage = M4A1Player.Get(player).SyncStage;
            int worth = Math.Max(1, (int)Math.Round(BalanceM4A1.GetMarkBuildMultiplier(stage)));
            mark.hitProgress += worth;

            while (mark.hitProgress >= BalanceM4A1.HitsPerMark && mark.MarkLevel < BalanceM4A1.MaxVengeanceMarks)
            {
                mark.hitProgress -= BalanceM4A1.HitsPerMark;
                mark.MarkLevel++;
                mark.flashTimer = 20;

                if (mark.MarkLevel == BalanceM4A1.MaxVengeanceMarks)
                    mark.detonationTimer = BalanceM4A1.Mark3DetonationInterval;

                SoundEngine.PlaySound(SoundID.Item41 with { Volume = 0.4f, Pitch = 0.35f + mark.MarkLevel * 0.12f }, npc.Center);
            }

            mark.lifeTimer = BalanceM4A1.MarkLifetimeTicks;
        }

        public override void PostAI(NPC npc)
        {
            if (flashTimer > 0)
                flashTimer--;

            if (MarkLevel <= 0)
                return;

            if (--lifeTimer <= 0)
            {
                MarkLevel--;
                hitProgress = 0;
                lifeTimer = BalanceM4A1.MarkLifetimeTicks;
                if (MarkLevel <= 0)
                {
                    owner = -1;
                    lastHitDamage = 0;
                    return;
                }
            }

            // 三层：周期性小型战术爆破
            if (MarkLevel >= BalanceM4A1.MaxVengeanceMarks && owner >= 0)
            {
                if (--detonationTimer <= 0)
                {
                    detonationTimer = BalanceM4A1.Mark3DetonationInterval;
                    TriggerDetonation(npc);
                }
            }
        }

        private void TriggerDetonation(NPC npc)
        {
            Player player = Main.player[owner];
            if (!player.active || player.dead)
                return;

            if (owner == Main.myPlayer)
            {
                int damage = Math.Max(1, (int)(lastHitDamage * 1.5f));
                Projectile.NewProjectile(
                    player.GetSource_Misc("M4A1MarkDetonation"),
                    npc.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<M4A1MarkDetonation>(),
                    damage,
                    3f,
                    owner);
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (MarkLevel <= 0)
                return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float pulse = 0.55f + 0.45f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f);
            float flash = flashTimer / 20f;
            Color color = Color.Lerp(M4A1Visuals.MarkColor, Color.White, flash * 0.6f) * (0.6f + pulse * 0.4f);

            // ── 锁定框（4 角括号）──
            Rectangle box = new((int)(npc.position.X - Main.screenPosition.X) - 4,
                                (int)(npc.position.Y - Main.screenPosition.Y) - 4,
                                npc.width + 8, npc.height + 8);
            int corner = Math.Clamp(Math.Min(box.Width, box.Height) / 4, 6, 20);
            int t = 2;
            DrawCornerBrackets(spriteBatch, pixel, box, corner, t, color);

            // ── 印记点（头顶居中）──
            Vector2 pipCenter = new(box.Center.X, box.Y - 12);
            float spacing = 10f;
            float startX = pipCenter.X - (MarkLevel - 1) * spacing * 0.5f;
            for (int i = 0; i < MarkLevel; i++)
            {
                Vector2 p = new(startX + i * spacing, pipCenter.Y);
                DrawDiamond(spriteBatch, pixel, p, 3f + pulse, color);
            }
        }

        private static void DrawCornerBrackets(SpriteBatch sb, Texture2D pixel, Rectangle box, int len, int thick, Color color)
        {
            void H(int x, int y, int w) => sb.Draw(pixel, new Rectangle(x, y, w, thick), color);
            void V(int x, int y, int h) => sb.Draw(pixel, new Rectangle(x, y, thick, h), color);

            // 左上
            H(box.Left, box.Top, len); V(box.Left, box.Top, len);
            // 右上
            H(box.Right - len, box.Top, len); V(box.Right - thick, box.Top, len);
            // 左下
            H(box.Left, box.Bottom - thick, len); V(box.Left, box.Bottom - len, len);
            // 右下
            H(box.Right - len, box.Bottom - thick, len); V(box.Right - thick, box.Bottom - len, len);
        }

        private static void DrawDiamond(SpriteBatch sb, Texture2D pixel, Vector2 center, float size, Color color)
        {
            sb.Draw(pixel, center, new Rectangle(0, 0, 1, 1), color, MathHelper.PiOver4, new Vector2(0.5f), size, SpriteEffects.None, 0f);
        }
    }
}
