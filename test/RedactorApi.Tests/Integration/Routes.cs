namespace RedactorApi.Tests.Integration;

public static class Routes
{
    private const string BaseRoute = "api";

    public static class Reviews
    {
        private const string BaseAuthorsRoute = BaseRoute + "/review";

        public const string Review = BaseAuthorsRoute;
    }

    public static class Verification
    {
        private const string BaseAuthorsRoute = BaseRoute + "/verify";

        public const string Verify = BaseAuthorsRoute;

    }

    public static class FileCheck
    {
        private const string BaseAuthorsRoute = BaseRoute + "/files";

        public const string Files = BaseAuthorsRoute;
    }
}