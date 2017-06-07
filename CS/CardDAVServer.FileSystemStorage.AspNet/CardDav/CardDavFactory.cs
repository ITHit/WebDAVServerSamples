using ITHit.WebDAV.Server;


namespace CardDAVServer.FileSystemStorage.AspNet.CardDav
{
    /// <summary>
    /// Represents a factory for creating CardDAV items.
    /// </summary>
    public static class CardDavFactory
    {
        /// <summary>
        /// Gets CardDAV items.
        /// </summary>
        /// <param name="path">Relative path requested.</param>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <returns>Object implementing various CardDAV items or null if no object corresponding to path is found.</returns>
        internal static IHierarchyItemAsync GetCardDavItem(DavContext context, string path)
        {
            IHierarchyItemAsync item = null;

            item = AddressbooksRootFolder.GetAddressbooksRootFolder(context, path);
            if (item != null)
                return item;

            item = AddressbookFolder.GetAddressbookFolder(context, path);
            if (item != null)
                return item;

            item = CardFile.GetCardFile(context, path);
            if (item != null)
                return item;

            return null;
        }
    }
}