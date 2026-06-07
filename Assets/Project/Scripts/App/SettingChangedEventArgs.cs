namespace Gazeus.DesafioMatch3.App
{
    public readonly struct SettingChangedEventArgs
    {
        public SettingId Id { get; }

        public SettingChangedEventArgs(SettingId id) => Id = id;
    }
}
