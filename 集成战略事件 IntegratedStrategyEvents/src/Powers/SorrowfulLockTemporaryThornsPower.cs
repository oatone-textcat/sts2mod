using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace IntegratedStrategyEvents.Powers;

/// <summary>
/// 临时荆棘：仿原版 TemporaryStrengthPower 的套路——施加时内部叠等量荆棘，
/// 自身回合结束时移除本 power 并扣回等量荆棘。
/// </summary>
public sealed class SorrowfulLockTemporaryThornsPower : PowerModel, IModPowerAssetOverrides
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 荆棘本体已在图标栏显示，本 power 只负责到期回收，不额外占一格。
	protected override bool IsVisibleInternal => false;

	public PowerAssetProfile AssetProfile => PowerAssetProfile.Empty;

	public string? CustomIconPath => ModelDb.Power<ThornsPower>().PackedIconPath;

	public string? CustomBigIconPath => ModelDb.Power<ThornsPower>().ResolvedBigIconPath;

	public override async Task BeforeApplied(
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		await PowerCmd.Apply<ThornsPower>(target, amount, applier, cardSource, silent: true);
	}

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		_ = choiceContext;
		_ = participants;
		if (Owner == null || side != Owner.Side)
		{
			return;
		}

		decimal amount = Amount;
		await PowerCmd.Remove(this);
		await PowerCmd.Apply<ThornsPower>(Owner, -amount, Owner, null, silent: true);
	}
}
