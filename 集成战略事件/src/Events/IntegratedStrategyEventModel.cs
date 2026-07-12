using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace IntegratedStrategyEvents.Events;

public abstract partial class IntegratedStrategyEventModel : ModEventTemplate
{
	protected const string InitialPage = "INITIAL";

	protected abstract IntegratedStrategyEventDefinition Definition { get; }

	public override string? CustomInitialPortraitPath => Definition.PortraitPath;

	public virtual IntegratedStrategyEventLayoutProfile LayoutProfile =>
		Definition.Layout ?? IntegratedStrategyEventLayoutProfile.Standard;

	public bool AlignHoverTipsRight => Definition.AlignHoverTipsRight;

	public override bool IsShared => false;

	public override bool IsAllowed(IRunState runState)
	{
		return IntegratedStrategyEventSpawnRules.IsAllowed(GetType(), runState);
	}

	protected Player OwnerOrThrow => Owner
		?? throw new InvalidOperationException($"{GetType().Name} has no owner.");

	protected override Task BeforeEventStarted(bool isPreFinished)
	{
		IntegratedStrategyEventRuntimeCompatibility.MergeCurrentEventLocalization();
		return base.BeforeEventStarted(isPreFinished);
	}
}
