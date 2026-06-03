using UnityEngine;

public abstract class ItemUseAction : ScriptableObject
{
    public abstract bool CanUse(GameObject user);
    public abstract bool TryUse(GameObject user);

    public virtual string GetSuccessMessage(GameObject user) => "Used";
    public virtual string GetFailMessage(GameObject user) => "Cannot use right now";
}
