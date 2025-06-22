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
        CreateMultipleEntities,

        /// <summary> 
        /// Creates multiple entities and selects them. 
        /// </summary>
        CreateMultipleEntitiesAndSelect,

        /// <summary> 
        /// Destroys selected entities. 
        /// </summary>
        DestroySelectedEntities,

        /// <summary> 
        /// Clears selection. 
        /// </summary>
        ClearSelection,

        /// <summary> 
        /// Selects a single entity. 
        /// </summary>
        AppendEntityToSelection,

        /// <summary> 
        /// Clears selection and selects a single entity. 
        /// </summary>
        SetSelectedEntity,

        /// <summary> 
        /// Selects entities. 
        /// </summary>
        AppendMultipleEntitiesToSelection,

        /// <summary> 
        /// Selects a previously created entity. 
        /// </summary>
        AppendPreviouslyCreatedEntityToSelection,

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
        EnableSelectedEntities,

        /// <summary> 
        /// Makes selected entities disabled
        /// </summary>
        DisableSelectedEntities,

        /// <summary> 
        /// Add a component to selected entities. 
        /// </summary>
        AddComponentType,

        /// <summary> 
        /// Adds a component type to the selected entities if possible. 
        /// </summary>
        TryAddComponentType,

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
        /// Assigns a single element in an array on selected entities. 
        /// </summary>
        SetArrayElement,

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