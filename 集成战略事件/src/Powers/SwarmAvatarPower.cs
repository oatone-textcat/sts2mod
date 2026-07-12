using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;
using IntegratedStrategyEvents.Encounters;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Powers;

public sealed class SwarmAvatarPower : PowerModel, IModPowerAssetOverrides
{
	public PowerAssetProfile AssetProfile => PowerAssetProfile.Empty;

	private const string CallOfTheVoidPowerPackedIconPath =
		"res://images/atlases/power_atlas.sprites/call_of_the_void_power.tres";
	private const string CallOfTheVoidPowerBigIconPath = "res://images/powers/call_of_the_void_power.png";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public string? CustomIconPath => CallOfTheVoidPowerPackedIconPath;

	public string? CustomBigIconPath => CallOfTheVoidPowerBigIconPath;

	public override bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return Owner?.Monster is not IsharmlaCorruptedHeart boss || boss.HasRevived;
	}
}
