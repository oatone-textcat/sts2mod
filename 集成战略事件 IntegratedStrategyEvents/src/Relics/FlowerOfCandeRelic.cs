using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace IntegratedStrategyEvents.Relics;

public sealed class FlowerOfCandeRelic : IntegratedStrategyEventRelic
{
	private const decimal HealAmount = 1m;

	public FlowerOfCandeRelic()
		: base("flower_of_cande.png")
	{
	}

	public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		Player? owner = Owner;
		if (owner == null ||
			owner.Creature.IsDead ||
			side != CombatSide.Player ||
			!participants.Contains(owner.Creature))
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.Heal(owner.Creature, HealAmount);
	}
}
