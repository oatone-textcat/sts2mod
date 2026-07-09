using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace IntegratedStrategyEvents.Compatibility;

/// <summary>
/// 原版 ModelDb.AllEvents 只枚举"章节事件池 + 共享事件"。本模组大部分事件不进任何池
/// （由秘境节点/结局分支/终局层强制刷新），BaseLib 时代靠其枚举合并补丁仍可被
/// 控制台 event 命令、资源预加载等按 AllEvents 消费的系统看到；迁移 RitsuLib 后
/// 该合并缺失（表现为控制台只剩章节池里的 17 个事件）。这里恢复等价合并：
/// 把目录里已注册的全部事件模型并入枚举结果，不改变任何事件池归属。
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllEvents), MethodType.Getter)]
internal static class IntegratedStrategyAllEventsPatch
{
	private static IEnumerable<EventModel>? _cacheSource;
	private static List<EventModel>? _cache;

	private static void Postfix(ref IEnumerable<EventModel> __result)
	{
		if (_cache != null && ReferenceEquals(_cacheSource, __result))
		{
			__result = _cache;
			return;
		}

		IEnumerable<EventModel> source = __result;
		List<EventModel> augmented = [.. source];
		HashSet<ModelId> known = [.. augmented.Select(static eventModel => eventModel.Id)];
		foreach (Type eventType in IntegratedStrategyContentCatalog.EventTypes)
		{
			ModelId id = ModelDb.GetId(eventType);
			if (!known.Add(id) || !ModelDb.Contains(eventType))
			{
				continue;
			}

			if (ModelDb.GetByIdOrNull<EventModel>(id) is { } eventModel)
			{
				augmented.Add(eventModel);
			}
		}

		_cacheSource = source;
		_cache = augmented;
		__result = augmented;
	}
}
