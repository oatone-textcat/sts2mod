namespace IntegratedStrategyEvents.Events;

// 事件本地化数据记录。键格式与 BaseLib 3.x 的 EventLoc/EventPageLoc/EventOptionLoc 完全一致
// （title / pages.<PAGE>.description / pages.<PAGE>.options.<OPTION>.title|description），
// 由 IntegratedStrategyEventRuntimeCompatibility 以事件 ModelDb entry 为前缀合入 "events" 表。
public record EventLoc(string Title, params EventPageLoc[] Pages)
{
	public static implicit operator List<(string, string)>(EventLoc loc)
	{
		List<(string, string)> entries = [("title", loc.Title)];
		foreach (EventPageLoc page in loc.Pages)
		{
			entries.AddRange((List<(string, string)>)page);
		}

		return entries;
	}
}

public record EventPageLoc(string PageKey, string Description, params EventOptionLoc[] Options)
{
	public static implicit operator List<(string, string)>(EventPageLoc loc)
	{
		List<(string, string)> entries = [($"pages.{loc.PageKey}.description", loc.Description)];
		foreach (EventOptionLoc option in loc.Options)
		{
			entries.AddRange(option.Create(loc));
		}

		return entries;
	}
}

public record EventOptionLoc(string OptionKey, string Title, string Description)
{
	public IEnumerable<(string, string)> Create(EventPageLoc page)
	{
		yield return ($"pages.{page.PageKey}.options.{OptionKey}.title", Title);
		yield return ($"pages.{page.PageKey}.options.{OptionKey}.description", Description);
	}
}
