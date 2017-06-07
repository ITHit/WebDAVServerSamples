
namespace CalDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// Specifies which properties should be loaded.
    /// </summary>
    public enum PropsToLoad
    {
        /// <summary>
        /// Used for OPTIONS, COPY, MOVE, DELETE
        /// </summary>
        None,

        /// <summary>
        /// Used for PROPFIND (GetChildren call)
        /// </summary>
        Minimum,

        /// <summary>
        /// Used for DET
        /// </summary>
        All
    }
}
