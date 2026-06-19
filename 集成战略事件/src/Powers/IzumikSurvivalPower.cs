using BaseLib.Abstracts;
using IntegratedStrategyEvents.Encounters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Powers;

public sealed class IzumikSurvivalPower : PowerModel, ICustomPower
{
	private const string LoopPowerPackedIconPath = "res://images/atlases/power_atlas.sprites/loop_power.tres";
	private const string LoopPowerBigIconPath = "res://images/powers/loop_power.png";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public string? CustomPackedIconPath => LoopPowerPackedIconPath;

	public string? CustomBigIconPath => LoopPowerBigIconPath;

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		_ = participants;
		if (side != CombatSide.Player ||
			Owner == null ||
			Owner.IsDead ||
			Owner.CombatState != combatState ||
			!combatState.Enemies.Contains(Owner) ||
			Owner.Monster is not IzumikEcologicalFountain izumik)
		{
			return;
		}

		Flash();
		await izumik.SummonOffspringToEmptySlots();
	}
}
