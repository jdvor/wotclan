namespace WotClan.Personnel;

internal static class ExitCode
{
    public const int Success = 0;
    public const int HttpFailure = 1;
    public const int HttpTimeout = 2;
    public const int DeserializationFailure = 3;
    public const int WritingOutputFailure = 4;
    public const int Failure = 5;
}
