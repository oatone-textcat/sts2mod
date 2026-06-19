using BaseLib.Abstracts;
using IntegratedStrategyEvents.Encounters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Powers;

public sealed class IzumikTransformationPower : PowerModel, ICustomPower
{
	private const string ConfusedPowerPackedIconPath =
		"res://images/atlases/power_atlas.sprites/confused_power.tres";
	private const string ConfusedPowerBigIconPath = "res://images/powers/confused_power.png";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public string? CustomPackedIconPath => ConfusedPowerPackedIconPath;

	public string? CustomBigIconPath => ConfusedPowerBigIconPath;

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		_ = choiceContext;
		if (Owner == null ||
			Owner.IsDead ||
			side != Owner.Side ||
			!participants.Contains(Owner) ||
			Owner.Monster is not IzumikOffspring offspring)
		{
			return;
		}

		await offspring.TransformIntoRandomEnemy();
	}

	public void Pulse()
	{
		Flash();
	}
}
