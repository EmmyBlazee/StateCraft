using System;

namespace StateCraft.Interfaces
{
    public interface ISaveable
    {
        string ExportState();

        void ImportState(string json);
    }
}
