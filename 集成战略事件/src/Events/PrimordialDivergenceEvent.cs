using IntegratedStrategyEvents.Relics;
using MegaCrit.Sts2.Core.Events;

namespace IntegratedStrategyEvents.Events;

public sealed partial class PrimordialDivergenceEvent : IntegratedStrategyEventModel
{
	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return
		[
			Choice(ChooseOddity, "ODDITY"),
			Choice(ChooseBlackBox, "BLACK_BOX"),
			Choice(ChooseLotusPetal, "LOTUS_PETAL"),
			Choice(ChooseMetal, "METAL")
		];
	}

	private Task ChooseOddity()
	{
		ShowPage("ODDITY", [Choice(ClaimOddityReward, "FATE_IS_WONDROUS", "ODDITY")]);
		return Task.CompletedTask;
	}

	private Task ChooseBlackBox()
	{
		ShowPage("BLACK_BOX", [RelicChoice<ProphetHornRelic>(ClaimProphetHorn, "HORN_OR_GUN", "BLACK_BOX")]);
		return Task.CompletedTask;
	}

	private Task ChooseLotusPetal()
	{
		ShowPage("LOTUS_PETAL", [RelicChoice<PetalRelic>(ClaimPetal, "TEEKAZ_ANASA", "LOTUS_PETAL")]);
		return Task.CompletedTask;
	}

	private Task ChooseMetal()
	{
		ShowPage("METAL", [Choice(LeaveMetal, "LEAVE", "METAL")]);
		return Task.CompletedTask;
	}

	private async Task ClaimOddityReward()
	{
		await ObtainRandomRelic();
		Finish("FATE_IS_WONDROUS");
	}

	private async Task ClaimProphetHorn()
	{
		await ObtainRelic<ProphetHornRelic>();
		Finish("HORN_OR_GUN");
	}

	private async Task ClaimPetal()
	{
		await ObtainRelic<PetalRelic>();
		Finish("TEEKAZ_ANASA");
	}

	private Task LeaveMetal()
	{
		Finish("METAL");
		return Task.CompletedTask;
	}
}
