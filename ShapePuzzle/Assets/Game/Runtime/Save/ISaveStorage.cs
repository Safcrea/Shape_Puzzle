namespace ToyPuzzle
{
    public interface ISaveStorage
    {
        bool TryReadPrimary(out string json);
        bool TryReadBackup(out string json);
        void WriteAtomically(string json);
        void PreserveCorruptPrimary();
    }
}
