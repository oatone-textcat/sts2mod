using BaseLib.Abstracts;
using IntegratedStrategyEvents.Encounters;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Powers;

public sealed class SwarmAvatarPower : PowerModel, ICustomPower
{
	private const string CallOfTheVoidPowerPackedIconPath =
		"res://images/atlases/power_atlas.sprites/call_of_the_void_power.tres";
	private const string CallOfTheVoidPowerBigIconPath = "res://images/powers/call_of_the_void_power.png";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public string? CustomPackedIconPath => CallOfTheVoidPowerPackedIconPath;

	public string? CustomBigIconPath => CallOfTheVoidPowerBigIconPath;

	public override bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return Owner?.Monster is not IsharmlaCorruptedHeart boss || boss.HasRevived;
	}
}
