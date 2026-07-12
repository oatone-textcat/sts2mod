using IntegratedStrategyEvents.Relics;
using MegaCrit.Sts2.Core.Events;

namespace IntegratedStrategyEvents.Events;

public sealed partial class ExpressionEvent : IntegratedStrategyEventModel
{
	private const int CardsToRemove = 2;

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return
		[
			Choice(Endure, "ENDURE"),
			CreateResistOption(),
			Choice(Negotiate, "NEGOTIATE")
		];
	}

	private EventOption CreateResistOption()
	{
		return HasRemovableDeckCards(CardsToRemove)
			? RelicChoice<RollingAncestorsRelic>(Resist, "RESIST")
			: LockedChoice("RESIST_LOCKED");
	}

	private async Task Endure()
	{
		await ObtainRandomRelic();
		Finish("ENDURE");
	}

	private async Task Resist()
	{
		await RemoveDeckCards(CardsToRemove);
		await ObtainRelic<RollingAncestorsRelic>();
		Finish("RESIST");
	}

	private Task Negotiate()
	{
		Finish("NEGOTIATE");
		return Task.CompletedTask;
	}
}
