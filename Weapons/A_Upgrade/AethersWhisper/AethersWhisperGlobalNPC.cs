using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper
{
    /// <summary>
    /// 回收晶片的「同组防重」记录器（第 6.4 节）。
    /// 每束主伪激光生成的一对晶片共享一个 returnGroupId（= 父束 identity）。
    /// 记录某 NPC 最近一次被哪个 (owner, groupId) 晶片伤到、以及记录时的 tick，
    /// 使同一组的两片只能对同一 NPC 结算一次；不阻止下一束、下一轮或其他玩家的晶片。
    /// 只用于防重，不承载任何 Debuff / 数值加成 / 玩家状态。
    /// </summary>
    internal sealed class AethersWhisperGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private int lastReturnOwner = -1;
        private int lastReturnGroupId = -1;
        private int lastReturnHitTick = int.MinValue;

        /// <summary>
        /// 尝试为一枚回收晶片登记一次命中。允许则记录并返回 true；
        /// 若同一 (owner, groupId) 已在存活窗口内命中过该 NPC，则拒绝并返回 false。
        /// </summary>
        internal static bool TryRegisterReturnHit(NPC target, int owner, int groupId)
        {
            AethersWhisperGlobalNPC state = target.GetGlobalNPC<AethersWhisperGlobalNPC>();
            int now = (int)Main.GameUpdateCount;

            bool sameGroupStillActive =
                state.lastReturnOwner == owner &&
                state.lastReturnGroupId == groupId &&
                now - state.lastReturnHitTick <= AethersWhisperBalance.ReturnGroupImmuneWindow;

            if (sameGroupStillActive)
                return false;

            state.lastReturnOwner = owner;
            state.lastReturnGroupId = groupId;
            state.lastReturnHitTick = now;
            return true;
        }
    }
}
