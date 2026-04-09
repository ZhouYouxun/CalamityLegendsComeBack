using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
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

        public bool ApplyDamageAmpMark(int owner, int timeLeft)
        {
            bool isNewMark = ApplyMark(owner, timeLeft);
            if (BFArrowCommon.InBounds(owner, Main.maxPlayers) && damageAmpTimers[owner] < timeLeft)
                damageAmpTimers[owner] = timeLeft;

            return isNewMark;
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

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (HasDamageAmp())
                modifiers.FinalDamage *= 1.12f;
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (HasDamageAmp())
                modifiers.FinalDamage *= 1.12f;
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            float intensity = GetIntensity();
            if (intensity <= 0f)
                return;

            drawColor = Color.Lerp(drawColor, new Color(255, 80, 80), 0.45f * intensity);
            Lighting.AddLight(npc.Center, new Vector3(0.4f, 0.05f, 0.05f) * intensity);
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
            Color outlineColor = new Color(255, 55, 55, 0) * (0.22f + 0.26f * intensity);

            Vector2[] offsets =
            {
                new Vector2(2f, 0f),
                new Vector2(-2f, 0f),
                new Vector2(0f, 2f),
                new Vector2(0f, -2f)
            };

            foreach (Vector2 offset in offsets)
            {
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

        private bool HasDamageAmp()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (damageAmpTimers[i] > 0)
                    return true;
            }

            return false;
        }
    }
}
