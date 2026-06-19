using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IntegratedStrategyEvents.Relics;

public sealed class DeterminationRelic : IntegratedStrategyEventRelic
{
	private const decimal PowerAmount = 2m;
	private const decimal DrawAmount = 2m;

	public DeterminationRelic()
		: base("determination.png")
	{
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower(ModelDb.Power<StrengthPower>()),
		HoverTipFactory.FromPower(ModelDb.Power<DexterityPower>())
	];

	public override async Task BeforeCombatStart()
	{
		Player? owner = Owner;
		if (owner == null || owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<StrengthPower>(owner.Creature, PowerAmount, owner.Creature, null);
		await PowerCmd.Apply<DexterityPower>(owner.Creature, PowerAmount, owner.Creature, null);
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + DrawAmount : count;
	}
}
