using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;

namespace IntegratedStrategyEvents.Compatibility;

/// <summary>
/// 本地化合并共用骨架：LocManager 初始化后把 buildEntries 的结果合入指定表，
/// 并订阅语言切换重新合并。事件文案与遭遇战文案的两套合并共用本类。
/// </summary>
internal sealed class IntegratedStrategyLocMerge
{
	private readonly string _tableName;
	private readonly string _logLabel;
	private readonly Func<Dictionary<string, string>> _buildEntries;
	private bool _localeChangeSubscribed;

	public IntegratedStrategyLocMerge(
		string tableName,
		string logLabel,
		Func<Dictionary<string, string>> buildEntries)
	{
		_tableName = tableName;
		_logLabel = logLabel;
		_buildEntries = buildEntries;
	}

	public void Install()
	{
		TrySubscribeToLocaleChanges();
		Merge();
	}

	public void Merge()
	{
		if (LocManager.Instance == null)
		{
			return;
		}

		TrySubscribeToLocaleChanges();

		try
		{
			Dictionary<string, string> entries = _buildEntries();
			if (entries.Count == 0)
			{
				return;
			}

			LocManager.Instance.GetTable(_tableName).MergeWith(entries);
			Log.Info(
				$"{ModInfo.LogPrefix} Merged {entries.Count} {_logLabel} localization entries " +
				$"for {LocManager.Instance.Language}.");
		}
		catch (Exception ex)
		{
			Log.Warn($"{ModInfo.LogPrefix} Failed to merge {_logLabel} localization entries: {ex}");
		}
	}

	private void TrySubscribeToLocaleChanges()
	{
		if (_localeChangeSubscribed || LocManager.Instance == null)
		{
			return;
		}

		LocManager.Instance.SubscribeToLocaleChange(Merge);
		_localeChangeSubscribed = true;
	}
}
