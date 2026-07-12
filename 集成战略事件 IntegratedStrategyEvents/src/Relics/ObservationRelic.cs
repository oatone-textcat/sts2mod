using MegaCrit.Sts2.Core.Entities.Players;

namespace IntegratedStrategyEvents.Relics;

public sealed class ObservationRelic : IntegratedStrategyEventRelic
{
	private const decimal DrawAmount = 1m;

	public ObservationRelic()
		: base("observation.png")
	{
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + DrawAmount : count;
	}
}
