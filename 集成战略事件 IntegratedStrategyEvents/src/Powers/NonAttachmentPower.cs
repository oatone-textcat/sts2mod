using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;
using IntegratedStrategyEvents.Encounters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IntegratedStrategyEvents.Powers;

public sealed class NonAttachmentPower : PowerModel, IModPowerAssetOverrides
{
	public PowerAssetProfile AssetProfile => PowerAssetProfile.Empty;

	private const int FreePhaseRound = 3;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public string? CustomIconPath => ModelDb.Power<TheSealedThronePower>().PackedIconPath;

	public string? CustomBigIconPath => ModelDb.Power<TheSealedThronePower>().ResolvedBigIconPath;

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		_ = participants;
		if (side != CombatSide.Player ||
			combatState.RoundNumber < FreePhaseRound ||
			Owner == null ||
			Owner.IsDead ||
			Owner.CombatState != combatState ||
			!combatState.Enemies.Contains(Owner) ||
			Owner.Monster is not KuilongMahasattvaAvatar kuilong ||
			!kuilong.IsMeditationPhase)
		{
			return;
		}

		Flash();
		await kuilong.EnterFreePhase();
	}
}
