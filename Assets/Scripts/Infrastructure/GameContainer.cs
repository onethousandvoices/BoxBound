using System;
using System.Collections.Generic;

namespace BoxBound.Infrastructure
{
    public sealed class GameContainer : IDisposable
    {
        private readonly Dictionary<Type, object> _instances = new();

        public void Register<T>(T instance) where T : class
        {
            if (instance == null)
                throw new("Instance is null.");

            var type = typeof(T);

            if (!_instances.TryAdd(type, instance))
                throw new($"Service already registered: {type.Name}");
        }

        public void Dispose()
        {
            foreach (var instance in _instances.Values)
                if (instance is IDisposable disposable) disposable.Dispose();

            _instances.Clear();
        }
    }
}
