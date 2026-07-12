using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;
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
/// 作品：层数表示"再受到 N 次伤害后触发"。每受到一次伤害层数 -1，
/// 归零时获得 5 点活力与 1 点临时荆棘，并把计数重置为 5。
/// </summary>
public sealed class MasterworkPower : PowerModel, IModPowerAssetOverrides
{
	public const decimal HitsPerTrigger = 5m;
	private const decimal VigorPerTrigger = 5m;
	private const decimal ThornsPerTrigger = 1m;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public PowerAssetProfile AssetProfile => PowerAssetProfile.Empty;

	public string? CustomIconPath => ModelDb.Power<BattlewornDummyTimeLimitPower>().PackedIconPath;

	public string? CustomBigIconPath => ModelDb.Power<BattlewornDummyTimeLimitPower>().ResolvedBigIconPath;

	public override async Task AfterDamageReceived(
		PlayerChoiceContext choiceContext,
		Creature target,
		DamageResult result,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource)
	{
		_ = choiceContext;
		_ = props;
		_ = dealer;
		_ = cardSource;
		if (target != Owner || Owner == null || Owner.IsDead || result.TotalDamage <= 0)
		{
			return;
		}

		if (Amount > 1m)
		{
			await PowerCmd.ModifyAmount(this, -1m, Owner, null, silent: true);
			return;
		}

		// 计数归零：触发奖励并重置计数。
		await PowerCmd.ModifyAmount(this, HitsPerTrigger - Amount, Owner, null, silent: true);
		await PowerCmd.Apply<VigorPower>(Owner, VigorPerTrigger, Owner, null);
		await PowerCmd.Apply<SorrowfulLockTemporaryThornsPower>(Owner, ThornsPerTrigger, Owner, null);
	}
}
