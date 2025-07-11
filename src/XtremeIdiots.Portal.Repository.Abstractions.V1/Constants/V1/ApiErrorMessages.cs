namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1
{
    /// <summary>
    /// Constants for API error messages used across the repository API controllers.
    /// These standardized error messages provide consistent user-facing error descriptions.
    /// </summary>
    public static class ApiErrorMessages
    {
        // General Error Messages
        public const string InvalidRequestBodyMessage = "Could not deserialize request body";
        public const string RequestBodyNullMessage = "Request body was null";
        public const string RequestBodyNullOrEmptyMessage = "Request body was null or did not contain any entries";
        public const string RequestEntityMismatchMessage = "Request entity identifiers did not match";

        // Entity Not Found Messages
        public const string EntityNotFound = "Entity not found";

        // Conflict Error Messages
        public const string ProtectedNameConflictMessage = "This name is already protected";
        public const string PlayerTagConflictMessage = "Player already has this tag";
        public const string UserProfileConflictMessage = "User profile with this identity already exists";
        public const string PlayerConflictMessage = "Player with this game type and identifier already exists";
        public const string MapConflictMessage = "Map with this game type and name already exists";

        // Validation Error Messages
        public const string TagIdRequiredMessage = "TagId is required";
        public const string PlayerIdMismatchMessage = "PlayerId in the URL must match PlayerId in the request body";
        public const string BanFileMonitorIdMismatchMessage = "BanFileMonitorId in the URL must match BanFileMonitorId in the request body";
        public const string UserProfileIdMismatchMessage = "UserProfileId in the URL must match UserProfileId in the request body";
        public const string InvalidCutoffDateMessage = "Cutoff date was not provided or was invalid";

        // Entity Not Found Messages (Specific)
        public const string UserProfileNotFoundMessage = "User profile not found";

        // File Upload Error Messages
        public const string NoFilesProvidedMessage = "Request did not contain any files";
        public const string InvalidFileTypeMessage = "Invalid file type extension";

        // Server Error Messages
        public const string InternalServerErrorMessage = "An internal server error occurred";
    }
}
