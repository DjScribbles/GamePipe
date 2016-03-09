namespace GamePipeLib.Interfaces
{
    public interface ILocalSteamApplication : ISteamApplication
    {
        string GameDir { get; }
        bool SizeIsMeasured { get; }
        void MeasureDiskSize();
        void DeleteGameData();
        bool CanCopy();
        void RefreshFromAcf();
    }
}
