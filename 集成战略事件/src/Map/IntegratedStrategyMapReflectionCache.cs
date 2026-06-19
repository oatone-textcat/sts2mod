using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace IntegratedStrategyEvents.Map;

internal static class IntegratedStrategyMapReflectionCache
{
	private static readonly AccessTools.FieldRef<NNormalMapPoint, TextureRect> NormalMapPointIconRef =
		RequiredFieldRef<NNormalMapPoint, TextureRect>("_icon");
	private static readonly AccessTools.FieldRef<NNormalMapPoint, TextureRect> NormalMapPointOutlineRef =
		RequiredFieldRef<NNormalMapPoint, TextureRect>("_outline");
	private static readonly AccessTools.FieldRef<NNormalMapPoint, TextureRect> NormalMapPointQuestIconRef =
		RequiredFieldRef<NNormalMapPoint, TextureRect>("_questIcon");
	private static readonly AccessTools.FieldRef<NNormalMapPoint, Tween?> NormalMapPointTweenRef =
		RequiredFieldRef<NNormalMapPoint, Tween?>("_tween");
	private static readonly AccessTools.FieldRef<NMapPoint, Color> MapPointOutlineColorRef =
		RequiredFieldRef<NMapPoint, Color>("_outlineColor");
	private static readonly AccessTools.FieldRef<NMapLegendItem, TextureRect> LegendItemIconRef =
		RequiredFieldRef<NMapLegendItem, TextureRect>("_icon");
	private static readonly AccessTools.FieldRef<NMapLegendItem, HoverTip> LegendItemHoverTipRef =
		RequiredFieldRef<NMapLegendItem, HoverTip>("_hoverTip");
	private static readonly AccessTools.FieldRef<NMapLegendItem, MapPointType> LegendItemPointTypeRef =
		RequiredFieldRef<NMapLegendItem, MapPointType>("_pointType");
	private static readonly FieldInfo LegendItemsField =
		RequiredField<NMapScreen>("_legendItems");
	private static readonly FieldInfo MapLegendField =
		RequiredField<NMapScreen>("_mapLegend");
	private static readonly MethodInfo NormalMapPointAnimHoverMethod =
		RequiredMethod<NNormalMapPoint>("AnimHover");
	private static readonly MethodInfo NormalMapPointAnimUnhoverMethod =
		RequiredMethod<NNormalMapPoint>("AnimUnhover");

	private static bool _validated;

	public static void Validate()
	{
		if (_validated)
		{
			return;
		}

		_ = NormalMapPointIconRef;
		_ = NormalMapPointOutlineRef;
		_ = NormalMapPointQuestIconRef;
		_ = NormalMapPointTweenRef;
		_ = MapPointOutlineColorRef;
		_ = LegendItemIconRef;
		_ = LegendItemHoverTipRef;
		_ = LegendItemPointTypeRef;
		_ = LegendItemsField;
		_ = MapLegendField;
		_ = NormalMapPointAnimHoverMethod;
		_ = NormalMapPointAnimUnhoverMethod;
		_validated = true;
		Log.Info($"{ModInfo.LogPrefix} Validated map UI reflection cache.");
	}

	public static TextureRect NormalMapPointIcon(NNormalMapPoint node)
	{
		return NormalMapPointIconRef(node);
	}

	public static TextureRect NormalMapPointOutline(NNormalMapPoint node)
	{
		return NormalMapPointOutlineRef(node);
	}

	public static TextureRect NormalMapPointQuestIcon(NNormalMapPoint node)
	{
		return NormalMapPointQuestIconRef(node);
	}

	public static ref Tween? NormalMapPointTween(NNormalMapPoint node)
	{
		return ref NormalMapPointTweenRef(node);
	}

	public static ref Color MapPointOutlineColor(NMapPoint node)
	{
		return ref MapPointOutlineColorRef(node);
	}

	public static TextureRect LegendItemIcon(NMapLegendItem item)
	{
		return LegendItemIconRef(item);
	}

	public static ref HoverTip LegendItemHoverTip(NMapLegendItem item)
	{
		return ref LegendItemHoverTipRef(item);
	}

	public static ref MapPointType LegendItemPointType(NMapLegendItem item)
	{
		return ref LegendItemPointTypeRef(item);
	}

	public static Control? GetLegendItems(NMapScreen screen)
	{
		return LegendItemsField.GetValue(screen) as Control;
	}

	public static Control? GetMapLegend(NMapScreen screen)
	{
		return MapLegendField.GetValue(screen) as Control;
	}

	public static MethodInfo NormalMapPointAnimHover => NormalMapPointAnimHoverMethod;

	public static MethodInfo NormalMapPointAnimUnhover => NormalMapPointAnimUnhoverMethod;

	private static AccessTools.FieldRef<TInstance, TField> RequiredFieldRef<TInstance, TField>(string fieldName)
	{
		_ = RequiredField<TInstance>(fieldName);
		try
		{
			return AccessTools.FieldRefAccess<TInstance, TField>(fieldName);
		}
		catch (Exception ex)
		{
			Log.Error($"{ModInfo.LogPrefix} Failed to bind {typeof(TInstance).Name}.{fieldName}: {ex}");
			throw;
		}
	}

	private static FieldInfo RequiredField<TInstance>(string fieldName)
	{
		FieldInfo? field = AccessTools.Field(typeof(TInstance), fieldName);
		if (field != null)
		{
			return field;
		}

		string message = $"{ModInfo.LogPrefix} Missing required map UI field {typeof(TInstance).Name}.{fieldName}.";
		Log.Error(message);
		throw new MissingFieldException(typeof(TInstance).FullName, fieldName);
	}

	private static MethodInfo RequiredMethod<TInstance>(string methodName)
	{
		MethodInfo? method = AccessTools.Method(typeof(TInstance), methodName);
		if (method != null)
		{
			return method;
		}

		string message = $"{ModInfo.LogPrefix} Missing required map UI method {typeof(TInstance).Name}.{methodName}.";
		Log.Error(message);
		throw new MissingMethodException(typeof(TInstance).FullName, methodName);
	}
}
