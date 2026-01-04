namespace Mnema.Common;

public class PaginationParams
{
    private const int MaxPageSize = int.MaxValue;

    public static readonly PaginationParams Default = new()
    {
        PageSize = 20,
        PageNumber = 0
    };

    private readonly int _pageSize = MaxPageSize;
    public int PageNumber { get; init; }

    /// <summary>
    ///     If set to 0, will set as MaxInt
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value == 0 ? MaxPageSize : value;
    }
}