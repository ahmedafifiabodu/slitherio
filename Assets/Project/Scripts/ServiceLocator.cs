using System;
using System.Collections.Generic;

public class ServiceLocator
{
    private readonly IDictionary<object, (object service, bool dontDestroyOnLoad)> services = new Dictionary<object, (object service, bool dontDestroyOnLoad)>();

    private static ServiceLocator _instance;

    public static ServiceLocator Instance
    {
        get
        {
            _instance ??= new ServiceLocator();
            return _instance;
        }
    }

    public T GetService<T>()
    {
        if (services.TryGetValue(typeof(T), out var serviceTuple))
            return (T)serviceTuple.service;
        else
            throw new ApplicationException("The requested service is not registered");
    }

    public bool TryGetService<T>(out T service)
    {
        service = default;
        if (services.TryGetValue(typeof(T), out var serviceTuple))
        {
            service = (T)serviceTuple.service;
            return true;
        }
        else
            return false;
    }

    public void RegisterService<T>(T service, bool dontDestroyOnLoad)
    {
        Type serviceType = typeof(T);

        if (services.ContainsKey(serviceType))
        {
            Logging.LogWarning($"Service of type {serviceType.Name} is already registered.");

            if (service is UnityEngine.Component existingComponent)
                UnityEngine.Object.Destroy(existingComponent.gameObject);
            else if (service is UnityEngine.GameObject existingGameObject)
                UnityEngine.Object.Destroy(existingGameObject);

            return;
        }

        if (service is UnityEngine.Component newComponent)
        {
            if (newComponent.transform.parent != null)
            {
                newComponent.transform.parent = null;

                if (dontDestroyOnLoad)
                    UnityEngine.Object.DontDestroyOnLoad(newComponent.gameObject);
            }
        }
        else if (service is UnityEngine.GameObject newGameObject)
        {
            if (newGameObject.transform.parent != null)
            {
                newGameObject.transform.parent = null;

                if (dontDestroyOnLoad)
                    UnityEngine.Object.DontDestroyOnLoad(newGameObject);
            }
        }

        services[typeof(T)] = (service, dontDestroyOnLoad);
    }

    private void UnregisterNonPersistentServices()
    {
        foreach (var service in new List<object>(services.Keys))
        {
            if (!services[service].dontDestroyOnLoad)
                services.Remove(service);
        }
    }

    public void UnregisterService<T>()
    {
        Type serviceType = typeof(T);

        if (services.ContainsKey(serviceType))
            services.Remove(serviceType);
        else
            Logging.LogWarning($"Attempted to unregister service of type {serviceType.Name}, but it was not registered.");
    }

    public List<object> GetDontDestroyOnLoadServices()
    {
        List<object> dontDestroyOnLoadServices = new();

        foreach (var (service, dontDestroyOnLoad) in services.Values)
        {
            if (dontDestroyOnLoad)
                dontDestroyOnLoadServices.Add(service);
        }

        return dontDestroyOnLoadServices;
    }

    private ServiceLocator() => UnityEngine.SceneManagement.SceneManager.sceneUnloaded += scene => UnregisterNonPersistentServices();
}