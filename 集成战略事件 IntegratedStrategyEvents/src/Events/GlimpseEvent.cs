using IntegratedStrategyEvents.TreeHoles;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace IntegratedStrategyEvents.Events;

public sealed partial class GlimpseEvent : IntegratedStrategyEventModel
{
	private const string TimeSliceActName = "绀碧摇篮";

	public override bool IsShared => true;

	internal static bool CanEnterTreeHoleForAllPlayers(IRunState state)
	{
		return state.Players.All(static player =>
			IntegratedStrategyEventEffects.GetMostRecentlyObtainedPotion(player) != null);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		PotionModel? latestPotion = GetMostRecentlyObtainedPotion();
		return
		[
			latestPotion != null && CanEnterTreeHoleForAllPlayers(OwnerOrThrow.RunState)
				? PotionCostChoice(latestPotion, ClearSeabornTrace, "CLEAR_SEABORN")
				: LockedChoice("CLEAR_SEABORN_LOCKED"),
			Choice(ListenToMizuki, "LISTEN_TO_MIZUKI")
		];
	}

	private async Task ClearSeabornTrace()
	{
		PotionModel? latestPotion = GetMostRecentlyObtainedPotion();
		if (latestPotion != null)
		{
			await DiscardPotionAndRemoveSlot(latestPotion);
		}

		ShowPage("CLEAR_SEABORN", [Choice(EnterMysteriousTime, "ENTER_TIME_SPACE", "CLEAR_SEABORN")]);
	}

	private async Task ListenToMizuki()
	{
		await ObtainRandomRelic();
		Finish("LISTEN_TO_MIZUKI");
	}

	private Task EnterMysteriousTime()
	{
		Finish("CLEAR_SEABORN");
		return IntegratedStrategyTreeHoleController.EnterFromEvent(OwnerOrThrow, TimeSliceActName);
	}
}
