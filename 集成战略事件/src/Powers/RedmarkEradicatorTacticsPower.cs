using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Powers;

public sealed class RedmarkEradicatorTacticsPower : PowerModel, IModPowerAssetOverrides
{
	public PowerAssetProfile AssetProfile => PowerAssetProfile.Empty;

	private const string SneakyPowerPackedIconPath = "res://images/atlases/power_atlas.sprites/sneaky_power.tres";
	private const string SneakyPowerBigIconPath = "res://images/powers/sneaky_power.png";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public string? CustomIconPath => SneakyPowerPackedIconPath;

	public string? CustomBigIconPath => SneakyPowerBigIconPath;
}
