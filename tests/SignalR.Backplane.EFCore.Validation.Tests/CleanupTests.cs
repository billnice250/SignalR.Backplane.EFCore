using SignalR.Backplane.EFCore.Models;
using SignalR.Backplane.EFCore.Options;

namespace SignalR.Backplane.EFCore.Tests;

public class CleanupTests
{
    [Test]
    public async Task LogicalDeleteMarksMessage()
    {
        var efOptions = new EfBackplaneOptions
        {
            StoreSubscriberId = "testLogDetele",
            AutoCreate = true,
            HeartbeatTimeout = TimeSpan.FromMinutes(5),
            CleanupMode = CleanupMode.Logical
        };
        var (store, db) = TestUtils.CreateStore(efOptions);
        var msg = new BackplaneEnvelope { Method = "Test", MessageText = "DeleteMe" };
        await store.PublishAsync("test", msg);
        var enumerator = store.SubscribeAsync("test").GetAsyncEnumerator();
        await enumerator.MoveNextAsync();
        var (_, id) = enumerator.Current;
        Assert.That(enumerator.Current.Payload.MessageText, Is.EqualTo("DeleteMe"));

        await store.AckForCurrentStoreAsync(id);
        await store.RunCleanerAsync();
        var message = await db.Messages.FindAsync(id);
        Assert.NotNull(message);
        Assert.True(message.IsDeleted);
    }

    [Test]
    public async Task PhysicalDeleteRemovesMessage()
    {
        var efOptions = new EfBackplaneOptions
        {
            StoreSubscriberId = "testLogDetele",
            AutoCreate = true,
            HeartbeatTimeout = TimeSpan.FromMinutes(5),
            CleanupMode = CleanupMode.Physical
        };
        var (store, db) = TestUtils.CreateStore(efOptions);
        var msg = new BackplaneEnvelope { Method = "Test", MessageText = "DeleteMe" };
        await store.PublishAsync("test", msg);
        var enumerator = store.SubscribeAsync("test").GetAsyncEnumerator();
        await enumerator.MoveNextAsync();
        var (_, id) = enumerator.Current;
        Assert.That(enumerator.Current.Payload.MessageText, Is.EqualTo("DeleteMe"));

        await store.AckForCurrentStoreAsync(id);
        await store.RunCleanerAsync();
        var message = await db.Messages.FindAsync(id);
        Assert.Null(message);
    }
}
