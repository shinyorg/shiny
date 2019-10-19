using System;


namespace $safeprojectname$
{
    static class Constants
    {
#if DEBUG
        public static string AppCenterToken = "ios=;android=";
#else
        public static string AppCenterToken = "#{AppCenterToken}#";
#endif
    }
}
