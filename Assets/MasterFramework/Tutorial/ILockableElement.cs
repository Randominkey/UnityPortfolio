namespace MasterFramework.Tutorial
{
    public interface ILockableElement
    {
        string ElementId { get; }
        bool IsLocked { get; set; }
    }
}
