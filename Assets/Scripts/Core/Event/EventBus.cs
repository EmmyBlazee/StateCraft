using System;

public static class EventBus
{
    public static Action<WorldEvent> OnWorldEvent;

    public static void Publish(WorldEvent worldEvent)
    {
        OnWorldEvent?.Invoke(worldEvent);
    }
}