namespace Common
{
    public interface IParallelServiceOptions
    {
        int LowConcurrency { get; set; }
        int MediumConcurrency { get; set; }
        int HighConcurrency { get; set; }
    }
}


