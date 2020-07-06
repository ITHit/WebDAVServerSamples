using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Azure.Storage.Files.DataLake;
using Newtonsoft.Json;

namespace AzureDataLakeStorage.ExtendedAttributes
{
    /// <summary>
    /// Provides extension methods to read and write extended attributes on file and folders.
    /// </summary>
    /// <remarks>This class uses file system extended attributes in case of OS X and Linux or NTFS alternative data streams in case of Windows.</remarks>
    public static class DataLakeAttributeExtension
    {
        /// <summary>
        /// Depending on OS holds WindowsExtendedAttribute, OSXExtendedAttribute or LinuxExtendedAttribute class instance.
        /// </summary>
        private static IExtendedAttribute _extendedAttribute;
        /// <summary>
        /// Name of the custom attribute for LastModified property.
        /// </summary>
        private const string LastModifiedProperty = "LastModified";
        /// <summary>
        /// Provides data lake client to manipulate custom attributes.
        /// </summary>
        /// <param name="client">Data Lake client.</param>
        public static void UseDataLakeAttribute(DataLakeFileSystemClient client)
        {
            if(client == null) throw new ArgumentNullException(nameof(client));
            _extendedAttribute = new DataLakeExtendedAttribute(client);
        }

        /// <summary>
        /// Determines whether extended attributes are supported.
        /// </summary>
        /// <param name="dlItem"><see cref="DLItem"/> instance.</param>
        /// <returns>True if custom properties are supported, false otherwise.</returns>
        private static async Task<bool> IsExtendedAttributesSupportedAsync(this DLItem dlItem)
        {
            if (dlItem == null)
            {
                throw new ArgumentNullException("dlItem");
            }
            if (_extendedAttribute is DataLakeExtendedAttribute attribute)
            {
                await attribute.UseDlItem(dlItem);
            }
            return await _extendedAttribute.IsExtendedAttributesSupportedAsync(dlItem.Path);
        }

        /// <summary>
        /// Determines whether extended attributes are supported.
        /// </summary>
        /// <param name="dlItem"><see cref="DLItem"/> instance.</param>
        /// <returns>True if custom properties are supported, false otherwise.</returns>
        public static bool IsExtendedAttributesSupported(this DLItem dlItem)
        {
            return dlItem.IsExtendedAttributesSupportedAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Checks extended attribute existence.
        /// </summary>  
        /// <param name="dlItem"><see cref="DLItem"/> instance.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>True if attribute exist, false otherwise.</returns>
        public static async Task<bool> HasExtendedAttributeAsync(this DLItem dlItem, string attribName)
        {
            if (dlItem == null)
            {
                throw new ArgumentNullException("dlItem");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            if (_extendedAttribute is DataLakeExtendedAttribute attribute)
            {
                await attribute.UseDlItem(dlItem);
            }
            return await _extendedAttribute.HasExtendedAttributeAsync(dlItem.Path, attribName);
        }

        /// <summary>
        /// Gets extended attribute or null if attribute or file not found.
        /// </summary>
        /// <typeparam name="T">The value will be automatically deserialized to the type specified by this type-parameter.</typeparam>
        /// <param name="dlItem"><see cref="DLItem"/> instance.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        public static async Task<T> GetExtendedAttributeAsync<T>(this DLItem dlItem, string attribName) where T : new()
        {
            if (dlItem == null)
            {
                throw new ArgumentNullException("dlItem");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }
            if (_extendedAttribute is DataLakeExtendedAttribute attribute)
            {
                await attribute.UseDlItem(dlItem);
            }
            string attributeValue = await _extendedAttribute.GetExtendedAttributeAsync(dlItem.Path, attribName);

            return Deserialize<T>(attributeValue);
        }

        /// <summary>
        /// Sets extended attribute.
        /// </summary>
        /// <param name="dlItem"><see cref="DLItem"/> instance.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        /// <remarks>Preserves file last modification date.</remarks>
        public static async Task SetExtendedAttributeAsync(this DLItem dlItem, string attribName, object attribValue)
        {
            if (dlItem == null)
            {
                throw new ArgumentNullException("dlItem");
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

            if (_extendedAttribute is DataLakeExtendedAttribute attribute)
            {
                await attribute.UseDlItem(dlItem);
            }
            // As soon as Modified property is using LastModified property 
            // we need to preserve it when updating or deleting extended attribute.
            if (!dlItem.Properties.ContainsKey(LastModifiedProperty))
            {
                DateTime lastWriteTimeUtc = dlItem.ModifiedUtc;
                await _extendedAttribute.SetExtendedAttributeAsync(dlItem.Path, LastModifiedProperty, (lastWriteTimeUtc.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
            }
            await _extendedAttribute.SetExtendedAttributeAsync(dlItem.Path, attribName, serializedValue);

        }

        /// <summary>
        /// Deletes extended attribute.
        /// </summary>
        /// <param name="dlItem"><see cref="DLItem"/> instance.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <remarks>Preserves file last modification date.</remarks>
        public static async Task DeleteExtendedAttributeAsync(this DLItem dlItem, string attribName)
        {
            if (dlItem == null)
            {
                throw new ArgumentNullException("dlItem");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            // As soon as Modified property is using LastModified property 
            // we need to preserve it when updating or deleting extended attribute.
            if (!dlItem.Properties.ContainsKey(LastModifiedProperty))
            {
                DateTime lastWriteTimeUtc = dlItem.ModifiedUtc;
                await _extendedAttribute.SetExtendedAttributeAsync(dlItem.Path, LastModifiedProperty, (lastWriteTimeUtc.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
            }
            if (_extendedAttribute is DataLakeExtendedAttribute attribute)
            {
                await attribute.UseDlItem(dlItem);
            }
            await _extendedAttribute.DeleteExtendedAttributeAsync(dlItem.Path, attribName);
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

            return JsonConvert.SerializeObject(data);
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

            return JsonConvert.DeserializeObject<T>(xmlString);
        }

        /// <summary>
        /// Deletes all extended attributes.
        /// </summary>
        /// <param name="dlItem">><see cref="DLItem"/> instance.</param>
        public static async Task DeleteExtendedAttributes(this DLItem dlItem)
        {
            if (dlItem == null) throw new ArgumentNullException(nameof(dlItem));
            if (_extendedAttribute is DataLakeExtendedAttribute attribute)
            {
                await attribute.UseDlItem(dlItem);
            }
            await _extendedAttribute.DeleteExtendedAttributes(dlItem.Path);
        }

        /// <summary>
        /// Copies all extended attributes.
        /// </summary>
        /// <param name="dlItem">><see cref="DLItem"/> instance.</param>
        /// <param name="destination">><see cref="DataLakePathClient"/> destination data lake client instance.</param>
        public static async Task CopyExtendedAttributes(this DLItem dlItem, DataLakePathClient destination)
        {
            if (dlItem == null) throw new ArgumentNullException(nameof(dlItem));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (_extendedAttribute is DataLakeExtendedAttribute attribute)
            {
                await attribute.UseDlItem(dlItem);
            }
            await _extendedAttribute.CopyExtendedAttributes(dlItem.Path, destination.Path);
        }


        /// <summary>
        /// Moves all extended attributes.
        /// </summary>
        /// <param name="dlItem">><see cref="DLItem"/> instance.</param>
        /// <param name="destination">><see cref="DataLakePathClient"/> destination data lake client instance.</param>
        public static async Task MoveExtendedAttributes(this DLItem dlItem, DataLakePathClient destination)
        {
            if (dlItem == null) throw new ArgumentNullException(nameof(dlItem));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (_extendedAttribute is DataLakeExtendedAttribute attribute)
            {
                await attribute.UseDlItem(dlItem);
            }
            await _extendedAttribute.MoveExtendedAttributes(dlItem.Path, destination.Path);
        }

    }
}
