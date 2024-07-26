namespace Simulation
{
    public enum CommandOperation : byte
    {
        CreateEntity,
        DestroyEntities,
        ClearSelection,
        AddToSelection,
        SelectEntity,
        SetParent,
        AddComponent,
        RemoveComponent,
        SetComponent,
        CreateList,
        DestroyList,
        ClearList,
        InsertElement,
        RemoveElement,
        ModifyElement,
    }
}