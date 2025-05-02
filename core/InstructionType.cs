namespace Worlds
{
    /// <summary>
    /// Describes a possible instruction in an <see cref="Operation"/>.
    /// </summary>
    public enum InstructionType : byte
    {
        /// <summary> 
        /// Creates one entity. 
        /// </summary>
        CreateSingleEntity,

        /// <summary> 
        /// Creates one entity and select it. 
        /// </summary>
        CreateSingleEntityAndSelect,

        /// <summary> 
        /// Creates multiple entities. 
        /// </summary>
        CreateEntities,

        /// <summary> 
        /// Creates multiple entities and selects them. 
        /// </summary>
        CreateEntitiesAndSelect,

        /// <summary> 
        /// Destroys selected entities. 
        /// </summary>
        DestroySelectedEntities,

        /// <summary> 
        /// Clears selection. 
        /// </summary>
        ClearSelection,

        /// <summary> 
        /// Selects entities. 
        /// </summary>
        SelectEntities,

        /// <summary> 
        /// Selects a previously created entity. 
        /// </summary>
        SelectPreviouslyCreatedEntity,

        /// <summary> 
        /// Sets parent of selected entities to a previously created entity. 
        /// </summary>
        SetParentToPreviouslyCreatedEntity,

        /// <summary> 
        /// Sets parent of selected entities. 
        /// </summary>
        SetParent,

        /// <summary> 
        /// Makes selected entities enabled
        /// </summary>
        Enable,

        /// <summary> 
        /// Makes selected entities disabled
        /// </summary>
        Disable,

        /// <summary> 
        /// Add a component to selected entities. 
        /// </summary>
        AddComponentType,

        /// <summary> 
        /// Add a component to selected entities. 
        /// </summary>
        AddComponent,

        /// <summary> 
        /// Removes a component to selected entities. 
        /// </summary>
        RemoveComponentType,

        /// <summary> 
        /// Sets a component on selected entities. 
        /// </summary>
        SetComponent,

        /// <summary> 
        /// Adds a component or sets an existing selected entities. 
        /// </summary>
        AddOrSetComponent,

        /// <summary> 
        /// Adds a tag to selected entities. 
        /// </summary>
        AddTag,

        /// <summary> 
        /// Removes a tag from selected entities. 
        /// </summary>
        RemoveTag,

        /// <summary> 
        /// Creates an array on selected entities. 
        /// </summary>
        CreateArray,

        /// <summary> 
        /// Sets an array on selected entities. 
        /// </summary>
        SetArray,

        /// <summary> 
        /// Creates and initializes an array on selected entities. 
        /// </summary>
        CreateAndInitializeArray,

        /// <summary> 
        /// Creates or sets an existing array on selected entities. 
        /// </summary>
        CreateOrSetArray,

        /// <summary> 
        /// Destroys an array from selected entities. 
        /// </summary>
        DestroyArray,

        /// <summary> 
        /// Resizes an existing array on selected entities. 
        /// </summary>
        ResizeArray,

        /// <summary> 
        /// Assigns an element in an array on selected entities. 
        /// </summary>
        SetArrayElements,

        /// <summary> 
        /// Adds a reference on selected entities. 
        /// </summary>
        AddReference,

        /// <summary> 
        /// Adds a reference to a previously selected entities. 
        /// </summary>
        AddReferenceToPreviouslyCreatedEntity,

        /// <summary> 
        /// Removes a reference from selected entities. 
        /// </summary>
        RemoveReference
    }
}