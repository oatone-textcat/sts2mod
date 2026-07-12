using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace IntegratedStrategyEvents.Map;

/// <summary>先知长角的第三幕地图：全路径必经的中央秘境节点。</summary>
internal sealed class ProphetHornActMap : IntegratedStrategyHourglassActMap
{
	public ProphetHornActMap(IRunState runState)
		: base(runState, "integrated_strategy_prophet_horn_map", MapPointType.Unknown)
	{
	}

	public MapCoord SecretCoord => FixedNodeCoord;

	public static bool TryGetSecretCoord(ActMap map, out MapCoord coord)
	{
		return TryGetFixedNodeCoord(map, MapPointType.Unknown, out coord);
	}
}
