using Godot;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Implemented by state objects that support dictionary-based save/load.
    /// </summary>
    public interface IDictionaryPersistable
    {
        Godot.Collections.Dictionary<string, Variant> ToDictionary();
        void FromDictionary(Godot.Collections.Dictionary<string, Variant> data);
    }
}
