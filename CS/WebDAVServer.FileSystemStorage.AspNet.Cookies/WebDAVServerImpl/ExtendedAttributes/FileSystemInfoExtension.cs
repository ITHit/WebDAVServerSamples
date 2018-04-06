using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WebDAVServer.FileSystemStorage.AspNet.Cookies.ExtendedAttributes
{
    /// <summary>
    /// Provides extension methods to read and write extended attributes on file and folders.
    /// </summary>
    /// <remarks>This class uses file system extended attributes in case of OS X and Linux or NTFS alternative data streams in case of Windows.</remarks>
    public static class FileSystemInfoExtension
    {
        /// <summary>
        /// Depending on OS holds WindowsExtendedAttribute, OSXExtendedAttribute or LinuxExtendedAttribute class instance.
        /// </summary>
        private static readonly IExtendedAttribute extendedAttribute;

        /// <summary>
        /// Initializes static class members.
        /// </summary>
        static FileSystemInfoExtension()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                extendedAttribute = new WindowsExtendedAttribute();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                extendedAttribute = new LinuxExtendedAttribute();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                extendedAttribute = new OSXExtendedAttribute();
            }
            else
            {
                throw new Exception("Not Supported OS");
            }
        }

        /// <summary>
        /// Determines whether extended attributes are supported.
        /// </summary>
        /// <param name="info"><see cref="FileSystemInfo"/> instance.</param>
        /// <returns>True if extended attributes or NTFS file alternative streams are supported, false otherwise.</returns>
        public static async Task<bool> IsExtendedAttributesSupportedAsync(this FileSystemInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            return await extendedAttribute.IsExtendedAttributesSupportedAsync(info.FullName);
        }

        /// <summary>
        /// Checks extended attribute existence.
        /// </summary>  
        /// <param name="info"><see cref="FileSystemInfo"/> instance.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>True if attribute exist, false otherwise.</returns>
        public static async Task<bool> HasExtendedAttributeAsync(this FileSystemInfo info, string attribName)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            return await extendedAttribute.GetExtendedAttributeAsync(info.FullName, attribName) != null;
        }

        /// <summary>
        /// Gets extended attribute or null if attribute or file not found.
        /// </summary>
        /// <typeparam name="T">The value will be automatically deserialized to the type specified by this type-parameter.</typeparam>
        /// <param name="info"><see cref="FileSystemInfo"/> instance.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value or null if attribute or file not found.</returns>
        public static async Task<T> GetExtendedAttributeAsync<T>(this FileSystemInfo info, string attribName) where T : new()
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            string attributeValue = await extendedAttribute.GetExtendedAttributeAsync(info.FullName, attribName);

            return Deserialize<T>(attributeValue);
        }

        /// <summary>
        /// Sets extended attribute.
        /// </summary>
        /// <param name="info"><see cref="FileSystemInfo"/> instance.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        /// <remarks>Preserves file last modification date.</remarks>
        public static async Task SetExtendedAttributeAsync(this FileSystemInfo info, string attribName, object attribValue)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            if (attribValue==null)
            {
                throw new ArgumentNullException("attribValue");
            }

            string serializedValue = Serialize(attribValue);

            // As soon as Modified property is using FileSyatemInfo.LastWriteTimeUtc 
            // we need to preserve it when updating or deleting extended attribute.
            DateTime lastWriteTimeUtc = info.LastWriteTimeUtc;

            await extendedAttribute.SetExtendedAttributeAsync(info.FullName, attribName, serializedValue);

            // Restore last write time.
            info.LastWriteTimeUtc = lastWriteTimeUtc;
        }

        /// <summary>
        /// Deletes extended attribute.
        /// </summary>
        /// <param name="info"><see cref="FileSystemInfo"/> instance.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <remarks>Preserves file last modification date.</remarks>
        public static async Task DeleteExtendedAttributeAsync(this FileSystemInfo info, string attribName)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            // As soon as Modified property is using FileSyatemInfo.LastWriteTimeUtc 
            // we need to preserve it when updating or deleting extended attribute.
            DateTime lastWriteTimeUtc = info.LastWriteTimeUtc;

            await extendedAttribute.DeleteExtendedAttributeAsync(info.FullName, attribName);

            // Restore last write time.
            info.LastWriteTimeUtc = lastWriteTimeUtc;
        }

        /// <summary>
        /// Serializes object to XML string.
        /// </summary>
        /// <param name="data">Object to be serialized.</param>
        /// <returns>String representation of an object.</returns>
        private static string Serialize(object data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            XmlSerializer xmlSerializer = new XmlSerializer(data.GetType());
            StringBuilder stringBulder = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(stringBulder
                , System.Globalization.CultureInfo.InvariantCulture))
            {
                xmlSerializer.Serialize(stringWriter, data);
                return stringBulder.ToString();
            }
        }

        /// <summary>
        /// Deserializes XML string to an object of a specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlString">XML string to be deserialized.</param>
        /// <returns>Deserialized object. If xmlString is empty or null returns new empty instance of an object.</returns>
        private static T Deserialize<T>(string xmlString) where T : new()
        {
            if (string.IsNullOrEmpty(xmlString))
            {
                return new T();
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader reader = new StringReader(xmlString))
            {
                return (T)xmlSerializer.Deserialize(reader);
            }
        }
    }
}
