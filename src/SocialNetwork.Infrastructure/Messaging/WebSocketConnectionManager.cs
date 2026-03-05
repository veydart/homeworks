using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;

namespace SocialNetwork.Infrastructure.Messaging;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<WebSocket>> _connections = new();

    public void AddConnection(Guid userId, WebSocket socket)
    {
        _connections.AddOrUpdate(
            userId,
            _ => new ConcurrentBag<WebSocket> { socket },
            (_, bag) => { bag.Add(socket); return bag; });
    }

    public void RemoveConnection(Guid userId, WebSocket socket)
    {
        if (_connections.TryGetValue(userId, out var bag))
        {
            var remaining = new ConcurrentBag<WebSocket>(bag.Where(s => s != socket));
            if (remaining.IsEmpty)
                _connections.TryRemove(userId, out _);
            else
                _connections[userId] = remaining;
        }
    }

    public async Task SendToUserAsync(Guid userId, string message)
    {
        if (!_connections.TryGetValue(userId, out var sockets)) return;

        var bytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var socket in sockets)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch { }
            }
        }
    }

    public IEnumerable<Guid> GetOnlineUserIds() => _connections.Keys;
}
