namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1
{
    /// <summary>
    /// Constants for API error codes used across the repository API controllers.
    /// These standardized error codes provide consistent error identification for client applications.
    /// </summary>
    public static class ApiErrorCodes
    {
        // General Error Codes
        public const string InvalidRequestBody = "INVALID_REQUEST_BODY";
        public const string RequestBodyNull = "REQUEST_BODY_NULL";
        public const string RequestBodyNullOrEmpty = "REQUEST_BODY_NULL_OR_EMPTY";
        public const string RequestEntityMismatch = "REQUEST_ENTITY_MISMATCH";

        // Entity Errors
        public const string EntityNotFound = "ENTITY_NOT_FOUND";
        public const string EntityConflict = "ENTITY_CONFLICT";
        public const string EntityIdMismatch = "ENTITY_ID_MISMATCH";
        public const string MissingEntityId = "MISSING_ENTITY_ID";

        // Deserialization Errors
        public const string DeserializationError = "DESERIALIZATION_ERROR";
        public const string InvalidRequest = "INVALID_REQUEST";

        // File Upload Errors
        public const string NoFilesProvided = "NO_FILES_PROVIDED";
        public const string InvalidFileType = "INVALID_FILE_TYPE";

        // Date/Time Validation Errors
        public const string InvalidCutoffDate = "INVALID_CUTOFF_DATE";

        // Server Errors
        public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    }
}
