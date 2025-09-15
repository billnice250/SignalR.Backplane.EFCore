using SignalR.Backplane.EFCore.Models;
namespace SignalR.Backplane.EFCore.Tests;

public class BasicTests
{
    [Test]
    public async Task CanPublishAndSubscribe()
    {
        var (store, _) = TestUtils.CreateStore();
        var msg = new BackplaneEnvelope { Method = "Test", MessageText = "Hello" };
        await store.PublishAsync("test", msg);

        var enumerator = store.SubscribeAsync("test").GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        var (payload, id) = enumerator.Current;

        Assert.That(payload.MessageText, Is.EqualTo("Hello"));
        await store.AckForCurrentStoreAsync(id);
    }

    [Test]

    public async Task MessagesAreDeliveredInOrder()
    {
        var (store, _) = TestUtils.CreateStore();
        for (int i = 0; i < 5; i++)
            await store.PublishAsync("test", new BackplaneEnvelope { Method = "Test", MessageText = $"msg{i}" });

        var enumerator = store.SubscribeAsync("test").GetAsyncEnumerator();
        var results = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            Assert.True(await enumerator.MoveNextAsync());
            results.Add(enumerator.Current.Payload.MessageText ?? string.Empty);
            await store.AckForCurrentStoreAsync(enumerator.Current.Id);
        }
        CollectionAssert.AreEqual(new[] { "msg0", "msg1", "msg2", "msg3", "msg4" }, results);
    }
}
