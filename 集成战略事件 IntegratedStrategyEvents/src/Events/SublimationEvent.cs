using IntegratedStrategyEvents.Relics;
using MegaCrit.Sts2.Core.Events;

namespace IntegratedStrategyEvents.Events;

public sealed partial class SublimationEvent : IntegratedStrategyEventModel
{
	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return
		[
			RelicChoice<DeterminationRelic>(ChooseDetermination, "DETERMINATION"),
			RelicChoice<ObservationRelic>(ChooseObservation, "OBSERVATION"),
			RelicChoice<HesitationRelic>(ChooseHesitation, "HESITATION")
		];
	}

	private async Task ChooseDetermination()
	{
		await ObtainRelic<DeterminationRelic>();
		Finish("DETERMINATION");
	}

	private async Task ChooseObservation()
	{
		await ObtainRelic<ObservationRelic>();
		Finish("OBSERVATION");
	}

	private async Task ChooseHesitation()
	{
		await ObtainRelic<HesitationRelic>();
		Finish("HESITATION");
	}
}
