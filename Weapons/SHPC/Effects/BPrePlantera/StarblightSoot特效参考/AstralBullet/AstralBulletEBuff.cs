#if false
// REFERENCE_CODE_DISABLED: Legacy reference source kept for reading only. Do not compile in debug builds.
namespace CalamityRangerExpansion.Content.Ammunition.CPreMoodLord.AstralBullet
{
    public class AstralBulletEBuff : ModBuff, ILocalizedModType
    {
        public new string LocalizationCategory => "Buffs";
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true; // 确保这个buff是一个debuff
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.HasBuff(ModContent.BuffType<AstralBulletEBuff>()))
            {
                npc.GetGlobalNPC<AstralBulletGlobalNPC>().hasAstralBulletBuff = true;
            }
        }
    }
}
#endif // REFERENCE_CODE_DISABLED
