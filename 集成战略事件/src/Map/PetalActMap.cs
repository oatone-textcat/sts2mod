using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace IntegratedStrategyEvents.Map;

/// <summary>花瓣遗物的整幕地图：全路径必经的中央特殊精英节点。</summary>
internal sealed class PetalActMap : IntegratedStrategyHourglassActMap
{
	public PetalActMap(IRunState runState)
		: base(runState, "integrated_strategy_petal_map", MapPointType.Elite)
	{
	}

	public MapCoord SpecialEliteCoord => FixedNodeCoord;

	public static bool TryGetSpecialEliteCoord(ActMap map, out MapCoord coord)
	{
		return TryGetFixedNodeCoord(map, MapPointType.Elite, out coord);
	}
}
