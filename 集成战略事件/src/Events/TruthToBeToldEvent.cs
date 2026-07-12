using IntegratedStrategyEvents.TreeHoles;
using MegaCrit.Sts2.Core.Events;

namespace IntegratedStrategyEvents.Events;

public sealed partial class TruthToBeToldEvent : IntegratedStrategyEventModel
{
	private const int CompleteFlawCardCount = 2;

	public override bool IsShared => true;

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return
		[
			Choice(CompleteFlaw, "COMPLETE_FLAW"),
			Choice(AbsorbIntoThought, "ABSORB_THOUGHT")
		];
	}

	private async Task CompleteFlaw()
	{
		await GrantRandomCards(CompleteFlawCardCount);
		ShowPage("COMPLETE_FLAW", [Choice(EnterStrangeFragment, "ENTER_FRAGMENT", "COMPLETE_FLAW")]);
	}

	private Task AbsorbIntoThought()
	{
		ShowPage("ABSORB_THOUGHT", [Choice(CollectScatteredThoughts, "COLLECT_THOUGHTS", "ABSORB_THOUGHT")]);
		return Task.CompletedTask;
	}

	private async Task CollectScatteredThoughts()
	{
		await OfferRandomPotionReward();
		Finish("COLLECT_THOUGHTS");
	}

	private Task EnterStrangeFragment()
	{
		Finish("COMPLETE_FLAW");
		return IntegratedStrategyTreeHoleController.EnterProphetHornFragmentFromEvent(
			OwnerOrThrow,
			TreeHoleConstants.StrangeFragmentActName,
			TreeHoleConstants.StrangeFragmentStageLabel);
	}
}
