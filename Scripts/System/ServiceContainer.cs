using System;
using System.Collections.Generic;

public class ServiceContainer
{
    private Dictionary<System.Type, object> singletons = new();
    private Dictionary<System.Type, Func<object>> transients = new();

    public void RegisterSingleton<T>(T instance)
    {
        singletons[typeof(T)] = instance;
    }

    public void RegisterTransient<T>(Func<T> factory)
    {
        transients[typeof(T)] = () => factory();
    }

    public T Resolve<T>()
    {
        System.Type type = typeof(T);

        if (singletons.TryGetValue(type, out var val))
        {
            return (T)val;
        }
        
        if (transients.TryGetValue(type, out var valFunc))
        {
            return (T)valFunc();
        }

        throw new Exception($"Service not registered: {type}");
    }
}