using Npgsql;

namespace SocialNetwork.Api.Services;

public class SlaveDataSourcePool
{
    private readonly NpgsqlDataSource[] _sources;
    private int _counter;

    public SlaveDataSourcePool(NpgsqlDataSource[] sources)
    {
        _sources = sources;
    }

    public NpgsqlDataSource GetNext()
    {
        var index = Interlocked.Increment(ref _counter);
        return _sources[((index % _sources.Length) + _sources.Length) % _sources.Length];
    }
}
