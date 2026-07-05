namespace LioConecta.Application.Common.Observability;

public static class ObservabilityEventNames
{
    public static class Authentication
    {
        public const string LoginSucceeded = "Authentication.LoginSucceeded";
        public const string LoginFailed = "Authentication.LoginFailed";
        public const string Logout = "Authentication.Logout";
        public const string SessionExpired = "Authentication.SessionExpired";
        public const string AnonymousBlocked = "Authentication.AnonymousBlocked";
    }

    public static class Authorization
    {
        public const string AccessDenied = "Authorization.AccessDenied";
    }

    public static class Resource
    {
        public const string Viewed = "Resource.Viewed";
        public const string Export = "Resource.Export";
        public const string Download = "Resource.Download";
    }

    public static class Application
    {
        public const string Error = "Application.Error";
    }
}

public static class AccessEventTypes
{
    public const string Authentication = "Authentication";
    public const string Authorization = "Authorization";
    public const string ResourceAccess = "ResourceAccess";
}

public static class AccessEventResults
{
    public const string Success = "Success";
    public const string Denied = "Denied";
    public const string Failed = "Failed";
}
