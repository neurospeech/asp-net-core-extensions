namespace NeuroSpeech.Eternity
{
    public class EternityBlobLock : IEternityLock
    {
        public string LeaseID { get; set; }
        public string LockName { get; set; }
    }
}
