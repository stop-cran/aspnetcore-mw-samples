namespace SampleApp.Middleware
{
    public interface IVisitorIdFeature
    {
        string VisitorId { get; }
        bool IsFirstTimeVisitor { get; }
    }
}