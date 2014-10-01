namespace NServiceBus.InMemory.SubscriptionStorage
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// In memory implementation of the subscription storage
    /// </summary>
    class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                var dict = storage.GetOrAdd(m, type => new ConcurrentDictionary<Address, object>());
                dict.AddOrUpdate(address, null, (address1, o) => null);
            });
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                ConcurrentDictionary<Address, object> dict;
                if (storage.TryGetValue(m, out dict))
                {
                    object _;
                    dict.TryRemove(address, out _);
                }
            });
        }

        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new HashSet<Address>();
            messageTypes.ToList().ForEach(m =>
            {
                ConcurrentDictionary<Address, object> list;
                if (storage.TryGetValue(m, out list))
                {
                    result.UnionWith(list.Keys);
                }
            });
            return result;
        }

        public void Init()
        {
        }

        readonly ConcurrentDictionary<MessageType, ConcurrentDictionary<Address, object>> storage = new ConcurrentDictionary<MessageType, ConcurrentDictionary<Address, object>>();
    }
}
