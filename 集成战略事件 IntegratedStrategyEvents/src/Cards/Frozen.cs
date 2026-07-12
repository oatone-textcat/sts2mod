using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace IntegratedStrategyEvents.Cards;

public sealed class Frozen : CardModel
{
	public const string PortraitAssetPath = $"res://{ModInfo.ModId}/images/cards/frozen.png";

	public override CardPoolModel Pool => IsMutable && Owner != null
		? Owner.Character.CardPool
		: ModelDb.CardPool<TokenCardPool>();

	public override CardPoolModel VisualCardPool => Pool;

	public override int MaxUpgradeLevel => 0;

	public override bool CanBeGeneratedInCombat => false;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Unplayable,
		CardKeyword.Retain
	];

	public override string PortraitPath => PortraitAssetPath;

	public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

	public Frozen()
		: base(-1, CardType.Status, CardRarity.Status, TargetType.None, shouldShowInCardLibrary: false)
	{
	}
}
