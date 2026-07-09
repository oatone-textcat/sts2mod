namespace IntegratedStrategyEvents.TreeHoles;

/// <summary>
/// 标记接口：树洞 / 各终局 / 先知长角断章等模组自建的临时 ActMap。
/// 秘境节点等按"正常大地图"工作的系统据此统一排除临时图，
/// 新增临时图实现本接口即可，不需要再逐处扩写类型枚举。
/// </summary>
internal interface IIntegratedStrategyTemporaryActMap;
