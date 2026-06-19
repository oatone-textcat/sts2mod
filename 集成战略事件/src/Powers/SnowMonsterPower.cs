using BaseLib.Abstracts;
using IntegratedStrategyEvents.Encounters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IntegratedStrategyEvents.Powers;

public sealed class SnowMonsterPower : PowerModel, ICustomPower
{
	private bool _isReviving;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public string? CustomPackedIconPath => ModelDb.Power<HailstormPower>().PackedIconPath;

	public string? CustomBigIconPath => ModelDb.Power<HailstormPower>().ResolvedBigIconPath;

	public void CompleteRevive()
	{
		AssertMutable();
		_isReviving = false;
	}

	public override async Task AfterDeath(
		PlayerChoiceContext choiceContext,
		Creature creature,
		bool wasRemovalPrevented,
		float deathAnimLength)
	{
		_ = choiceContext;
		if (wasRemovalPrevented ||
			creature != Owner ||
			creature.Monster is not FrostNovaWinterScar frostNova ||
			frostNova.HasRevived ||
			_isReviving)
		{
			return;
		}

		_isReviving = true;
		Flash();
		await frostNova.TriggerReviveWaitingState(deathAnimLength);
	}

	public override bool ShouldAllowHitting(Creature creature)
	{
		if (creature != Owner)
		{
			return true;
		}

		return !_isReviving;
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return Owner?.Monster is FrostNovaWinterScar frostNova && (_isReviving || !frostNova.HasRevived);
	}

	public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		if (creature != Owner || creature.Monster is not FrostNovaWinterScar frostNova)
		{
			return true;
		}

		return frostNova.HasRevived && creature.IsDead;
	}

	public override bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return Owner?.Monster is not FrostNovaWinterScar frostNova || (frostNova.HasRevived && Owner.IsDead);
	}
}
