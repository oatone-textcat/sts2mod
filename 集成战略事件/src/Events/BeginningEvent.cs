using IntegratedStrategyEvents.Relics;
using MegaCrit.Sts2.Core.Events;

namespace IntegratedStrategyEvents.Events;

public sealed partial class BeginningEvent : IntegratedStrategyEventModel
{
	private const int MaxHpGain = 8;

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return
		[
			RelicChoice<BishopResearchRelic>(DiveDeep, "DIVE_DEEP"),
			RelicChoice<DeepBlueMemoryRelic>(DeepBlue, "DEEP_BLUE"),
			Choice(Nightmare, "NIGHTMARE")
		];
	}

	private async Task DiveDeep()
	{
		await ObtainRelic<BishopResearchRelic>();
		Finish("DIVE_DEEP");
	}

	private async Task DeepBlue()
	{
		await ObtainRelic<DeepBlueMemoryRelic>();
		Finish("DEEP_BLUE");
	}

	private async Task Nightmare()
	{
		await GainMaxHp(MaxHpGain);
		Finish("NIGHTMARE");
	}
}
