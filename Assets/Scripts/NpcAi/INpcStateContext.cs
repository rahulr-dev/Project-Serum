using Events;

namespace NpcAi
{
    public interface INpcStateContext
    {
        SerumActionBridge Bridge { get; }

        void ExecuteSceneAction(string handlerId);
    }
}
