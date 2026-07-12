using MegaCrit.Sts2.Core.Events;

namespace IntegratedStrategyEvents.Events;

public sealed partial class OdeEvent : IntegratedStrategyEventModel
{
	private const int RelicCount = 3;

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return
		[
			Choice(TakeRelics, "TAKE_RELICS"),
			Choice(Leave, "LEAVE")
		];
	}

	private async Task TakeRelics()
	{
		await ObtainRandomRelics(RelicCount);
		Finish("AFTERMATH");
	}

	private Task Leave()
	{
		Finish("AFTERMATH");
		return Task.CompletedTask;
	}
}
