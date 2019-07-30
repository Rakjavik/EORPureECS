using Unity.Entities;

namespace rak.ecs.Systems
{
    public enum CreatureTaskStatus { None, Complete, InProgress, Started, Failed}
    public enum CreatureActionType { None, Move, Eat, Cancelled }
    public enum CreatureTaskType { None, Food, Explore }
    public enum TaskFailReason { None, NoKnownFood, TargetIsNull }
}
