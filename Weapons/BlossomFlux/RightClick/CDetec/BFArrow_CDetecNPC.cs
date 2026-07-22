using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    // 侦查标记挂在 NPC 身上，统一处理描边、染色和团队增伤。
    internal class BFArrow_CDetecNPC : GlobalNPC
    {
        private readonly int[] markTimers = new int[Main.maxPlayers];
        private readonly int[] priorityTimers = new int[Main.maxPlayers];
        private readonly int[] damageAmpTimers = new int[Main.maxPlayers];

        public override bool InstancePerEntity => true;

        public bool ApplyMark(int owner, int timeLeft)
        {
            if (!BFArrowCommon.InBounds(owner, Main.maxPlayers))
                return false;

            bool isNewMark = markTimers[owner] <= 0;
            if (markTimers[owner] < timeLeft)
                markTimers[owner] = timeLeft;

            return isNewMark;
        }

        public bool ApplyPriorityMark(int owner, int timeLeft)
        {
            bool isNewMark = ApplyMark(owner, timeLeft);
            if (BFArrowCommon.InBounds(owner, Main.maxPlayers) && priorityTimers[owner] < timeLeft)
                priorityTimers[owner] = timeLeft;

            return isNewMark;
        }

        public bool IsMarkedBy(int owner) => BFArrowCommon.InBounds(owner, Main.maxPlayers) && markTimers[owner] > 0;

        public bool IsPriorityMarkedBy(int owner) => BFArrowCommon.InBounds(owner, Main.maxPlayers) && priorityTimers[owner] > 0;

        public int MultiplyCurrentMarkTime(int owner, float multiplier)
        {
            if (!BFArrowCommon.InBounds(owner, Main.maxPlayers) || multiplier <= 1f)
                return 0;

            if (markTimers[owner] <= 0 && priorityTimers[owner] <= 0)
                return 0;

            if (markTimers[owner] > 0)
                markTimers[owner] = System.Math.Max(markTimers[owner], (int)System.Math.Ceiling(markTimers[owner] * multiplier));

            if (priorityTimers[owner] > 0)
                priorityTimers[owner] = System.Math.Max(priorityTimers[owner], (int)System.Math.Ceiling(priorityTimers[owner] * multiplier));

            // 增伤窗口跟着标记一起延长，否则侵染延长的只是描边。
            if (damageAmpTimers[owner] > 0)
                damageAmpTimers[owner] = System.Math.Max(damageAmpTimers[owner], (int)System.Math.Ceiling(damageAmpTimers[owner] * multiplier));

            return System.Math.Max(markTimers[owner], priorityTimers[owner]);
        }

        // 侦查箭命中的每一个敌人都会走这里：标记本体负责视觉/索敌，增伤窗口独立且更短。
        public bool ApplyDamageAmpMark(int owner, int markTime, int ampTime)
        {
            bool isNewMark = ApplyMark(owner, markTime);
            if (BFArrowCommon.InBounds(owner, Main.maxPlayers) && damageAmpTimers[owner] < ampTime)
                damageAmpTimers[owner] = ampTime;

            return isNewMark;
        }

        public bool HasDamageAmp()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (damageAmpTimers[i] > 0)
                    return true;
            }

            return false;
        }

        public float DamageAmpIntensity()
        {
            int strongest = 0;
            for (int i = 0; i < Main.maxPlayers; i++)
                strongest = System.Math.Max(strongest, damageAmpTimers[i]);

            return strongest <= 0 ? 0f : Utils.GetLerpValue(0f, 45f, strongest, true);
        }

        public override void PostAI(NPC npc)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (markTimers[i] > 0)
                    markTimers[i]--;

                if (priorityTimers[i] > 0)
                    priorityTimers[i]--;

                if (damageAmpTimers[i] > 0)
                    damageAmpTimers[i]--;
            }
        }

        // 全局判定：只要标记的增伤窗口还在，任何来源（近战/弹幕/召唤/DoT）打上来都吃这一层放大，
        // 不需要为每种弹幕单独写特判。
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (HasDamageAmp())
                modifiers.FinalDamage *= BFReconRightBalance.DamageAmpMultiplier;
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            float intensity = GetIntensity();
            if (intensity <= 0f)
                return;

            Lighting.AddLight(npc.Center, new Vector3(0.08f, 0.34f, 0.4f) * intensity);

            if (Main.dedServ || !Main.rand.NextBool(3))
                return;

            Vector2 offset = Main.rand.NextVector2CircularEdge(npc.width * 0.55f + 8f, npc.height * 0.55f + 8f);
            Dust dust = Dust.NewDustPerfect(
                npc.Center + offset,
                DustID.Electric,
                -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.8f, 1.9f),
                100,
                Color.Lerp(new Color(96, 230, 255), Color.White, Main.rand.NextFloat(0.18f, 0.42f)),
                Main.rand.NextFloat(0.75f, 1.08f) * intensity);
            dust.noGravity = true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float intensity = GetIntensity();
            if (intensity <= 0f)
                return;

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = npc.Center - screenPos + new Vector2(0f, npc.gfxOffY);
            SpriteEffects effects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // 增伤窗口内描边更快更亮并压出白芯，让「这一层放大还在」肉眼可读。
            float amp = DamageAmpIntensity();
            float pulseSpeed = MathHelper.Lerp(0.09f, 0.19f, amp);
            float pulse = 0.5f + 0.5f * (float)System.Math.Sin(Main.GameUpdateCount * pulseSpeed + npc.whoAmI * 0.53f);
            float outlineOpacity = (0.52f + 0.38f * pulse) * intensity * (1f + 0.35f * amp);
            Color outlineColor = Color.Lerp(new Color(100, 220, 255), new Color(205, 250, 255), amp * 0.7f) with { A = 0 } * outlineOpacity;
            float thickness = 2.8f + amp * 1.1f;

            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Vector2 offset = new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * thickness;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    frame,
                    outlineColor,
                    npc.rotation,
                    origin,
                    npc.scale,
                    effects,
                    0);
            }
        }

        private float GetIntensity()
        {
            int strongestTimer = 0;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                strongestTimer = System.Math.Max(strongestTimer, System.Math.Max(markTimers[i], priorityTimers[i]));
            }

            return strongestTimer <= 0 ? 0f : Utils.GetLerpValue(0f, 180f, strongestTimer, true);
        }
    }
}
