using System;

namespace StateCraft.Interfaces
{
    public interface ISaveable
    {
        /// <summary>
        /// Returns the internal state of the system serialized as a JSON string.
        /// </summary>
        string ExportState();

        /// <summary>
        /// Restores the internal state of the system from a JSON string.
        /// </summary>
        void ImportState(string json);
    }
}
