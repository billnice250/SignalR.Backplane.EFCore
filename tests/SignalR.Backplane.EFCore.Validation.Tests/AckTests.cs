using SignalR.Backplane.EFCore.Models;
using static System.Formats.Asn1.AsnWriter;
namespace SignalR.Backplane.EFCore.Tests;
public class AckTests
{

    [Test]
    public async Task MessageNotRedeliveredOnceAcked()
    {
        string sessionDbconn = $"unitTest_{Guid.NewGuid().ToString()}.db";
        var (store, _) = TestUtils.CreateStore("sub", null, sessionDbconn);
        string randomMessage = $"AckTest{Guid.NewGuid()}";

        var msg = new BackplaneEnvelope
        {
            Method = "Test",
            MessageText = randomMessage,
            IsInvocationMessage = false
        };
        await store.PublishAsync("test", msg);

        // Read first message
        var first = await TryReadOneAsync(store.SubscribeAsync("test"), TimeSpan.FromSeconds(1));
        Assert.True(first.HasMessage);
        Assert.That(first.Value.Payload.MessageText, Is.EqualTo(randomMessage));

        // Ack and run cleaner
        await store.AckForCurrentStoreAsync(first.Value.Id);
        await store.RunCleanerAsync();

        // Try to read again
        var second = await TryReadOneAsync(store.SubscribeAsync("test"), TimeSpan.FromSeconds(1));

        Assert.False(second.HasMessage, "Acked message should not be re-delivered");
    }



    [Test]
    public async Task MultipleSubscribersMustAckIndependently()
    {
        const string Sub1 = "sub1";
        const string Sub2 = "sub2";
        string sessionDbconn = $"unitTest_{Guid.NewGuid().ToString()}.db";

        var (store1, _) = TestUtils.CreateStore(Sub1, null, sessionDbconn);
        var (store2, db) = TestUtils.CreateStore(Sub2, null, sessionDbconn);
        await store1.HeartbeatAsync(Sub1);
        await store1.HeartbeatAsync(Sub2);

        var msg = new BackplaneEnvelope { Method = "Test", MessageText = "MultiAck" };
        await store1.PublishAsync("test", msg);
        const string channel = "test";
        var r1 = await TryReadOneAsync(store1.SubscribeAsync(channel), TimeSpan.FromSeconds(1));
        var r2 = await TryReadOneAsync(store2.SubscribeAsync(channel), TimeSpan.FromSeconds(1));

        Assert.True(r1.HasMessage);
        Assert.True(r2.HasMessage);

        var id = r1.Value.Id;

        // Subscriber 1 acks
        await store1.AckAsync(id, Sub1);
        await store1.RunCleanerAsync();

        // Still present for sub2
        var notAcked = db.Messages.FirstOrDefault(m => !m.IsDeleted && m.Channel == channel);
        Assert.NotNull(notAcked);

        // Sub2 still sees it
        Assert.That(r2.Value.Payload.MessageText, Is.EqualTo("MultiAck"));

        // Sub2 acks
        await store2.AckAsync(id, Sub2);
        await store2.RunCleanerAsync();

        var deleted = db.Messages.FirstOrDefault(m => !m.IsDeleted && m.Channel == channel);
        Assert.Null(deleted);
    }



    private static async Task<(bool HasMessage, (BackplaneEnvelope Payload, long Id) Value)>
        TryReadOneAsync(IAsyncEnumerable<(BackplaneEnvelope Payload, long Id)> stream, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        await using var enumerator = stream.GetAsyncEnumerator(cts.Token);

        try
        {
            if (await enumerator.MoveNextAsync())
            {
                return (true, enumerator.Current);
            }
            return (false, default);
        }
        catch (OperationCanceledException)
        {
            return (false, default); // timeout => no message
        }
    }


}
