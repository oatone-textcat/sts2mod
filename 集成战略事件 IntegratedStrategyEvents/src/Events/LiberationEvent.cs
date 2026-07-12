using IntegratedStrategyEvents.Relics;
using MegaCrit.Sts2.Core.Events;

namespace IntegratedStrategyEvents.Events;

public sealed partial class LiberationEvent : IntegratedStrategyEventModel
{
	private const int MaxHpGain = 8;

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return
		[
			Choice(Observe, "OBSERVE"),
			Choice(StopReadingTragedies, "STOP_READING")
		];
	}

	private Task Observe()
	{
		ShowPage("OBSERVE", [RelicChoice<TatteredDollRelic>(PickUpDoll, "PICK_UP", "OBSERVE")]);
		return Task.CompletedTask;
	}

	private async Task PickUpDoll()
	{
		await ObtainRelic<TatteredDollRelic>();
		Finish("PICK_UP");
	}

	private async Task StopReadingTragedies()
	{
		await GainMaxHp(MaxHpGain);
		Finish("STOP_READING");
	}
}
