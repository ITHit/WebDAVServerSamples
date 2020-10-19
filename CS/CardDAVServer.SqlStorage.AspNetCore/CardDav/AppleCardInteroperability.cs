using System;
using System.Collections.Generic;
using System.Linq;

using ITHit.Collab;
using ITHit.Collab.Card;


namespace CardDAVServer.SqlStorage.AspNetCore.CardDav
{
    /// <summary>
    /// Converts vCard properties submitted by iOS and OS X to the format that is 
    /// understood by any CardDAV client application. Also converts standard vCard 
    /// props to format that could be understood by iOS and OS X CardDAV client.
    /// </summary>
    internal class AppleCardInteroperability
    {
        /// <summary>
        /// List of User-Agent header prefixes for which properties replacement must be applied.
        /// </summary>
        static private string[] userAgentsPrefixes = { "iOS", "Mac OS X" };

        /// <summary>
        /// Detects if the specified CardDAV client application requires 
        /// vCard Normalization or Denormalization.
        /// </summary>
        /// <returns>True if Normalization or Denormalization must be called, false otherwise.</returns>
        static internal bool NeedsConversion(string userAgent)
        {
            return userAgentsPrefixes.FirstOrDefault(x => userAgent.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)) != null;
        }

        /// <summary>
        /// Returns client app name. Returns "Apple Inc." if client is iOS or OS X.
        /// </summary>
        /// <param name="userAgent">User-Agent string.</param>
        /// <returns>Client app name or null.</returns>
        internal static string GetClientAppName(string userAgent)
        {
            string[] aAgent = userAgent.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string clientAppName = aAgent.FirstOrDefault();

            if (clientAppName == null)
                return null;

            if (NeedsConversion(userAgent))
                return "Apple Inc.";

            return clientAppName;
        }

        /// <summary>
        /// Normalizes or denormalizes vCard depending on the User-Agent header provided.
        /// Use this function only if vCard is stored in the original form sent by the client app (typically not in a database).
        /// </summary>
        /// <param name="card">Business card to process.</param>
        /// <param name="userAgent">User-Agent header value.</param>
        /// <returns>True if the vCard was modified, false otherwise.</returns>
        static internal bool Convert(string userAgent, ICard2 card)
        {
            if (card.ProductId == null || card.ProductId.Text == null)
                return false;

            // Example: PRODID:-//Apple Inc.//iOS 10.1.1//EN
            string[] aProductId = card.ProductId.Text.Split(new string[] { "//" }, StringSplitOptions.RemoveEmptyEntries);
            if (aProductId.Length < 3)
                return false;

            string product = aProductId[2];

            if (NeedsConversion(userAgent) && !NeedsConversion(product))
            {
                // Example: client: Apple, saved: not Apple -> Denormalize
                return Denormalize(card);
            }
            else
            if (!NeedsConversion(userAgent) && NeedsConversion(product))
            {
                // Example: client: not Apple, saved: Apple -> Normalize
                return Normalize(card);
            }

            return false;
        }

        /// <summary>
        /// Replaces "itemX.PROP" properties with "PROP". Saves itemX.X-ABLabel value to TYPE parameter.
        /// </summary>
        /// <param name="card">Business card to process.</param>
        /// <returns>True if the vCard was modified, false otherwise.</returns>
        static internal bool Normalize(ICard2 card)
        {
            bool modified = false;
            if (NormalizeProps(card))
            {
                modified = true;
            }

            // BDAY: iOS & OS X may set year to 1604, this may not be properly interpreted by other CardDAV clients
            IDate2 birthday = card.BirthDate;
            if ((birthday != null) && (birthday.Value != null) && birthday.Value.DateVal.Year == 1604)
            {
                birthday.Value = new Date(birthday.Value.DateVal, DateComponents.Month | DateComponents.Day);
                modified = true;
            }

            return modified;
        }

        static private bool NormalizeProps(ICard2 card)
        {
            bool modified = false;

            // Find props with "itemX." prefix. Copy props to new list.
            var appleProps = card.Properties.Where(x => x.Key.Contains(".")).ToList();

            List<string> propNamesToDelete = new List<string>();

            foreach (KeyValuePair<string, IList<IRawProperty>> keyValProps in appleProps)
            {
                // Typically iOS / OS X provides 2 props instead of one for each vCard property, for example:
                // item2.TEL:(222)222 - 2222
                // item2.X-ABLabel:Emergency

                // Get regular vCard prop name ("TEL") from Apple prop name ("item2.TEL").
                int dotIndex = keyValProps.Key.IndexOf('.');
                if ((dotIndex < 0) || (dotIndex == keyValProps.Key.Length - 1))
                    continue;

                string fixedName = keyValProps.Key.Substring(dotIndex + 1);

                if (fixedName.StartsWith("X-"))
                    continue;

                IRawProperty prop = keyValProps.Value.First(); // There is always only one property itemN.PROP in the list in case of iOS / OS X.

                // Find itemN.X-ABLabel property.
                string labelPropName = keyValProps.Key.Substring(0, dotIndex) + ".X-ABLabel";
                if (card.Properties.ContainsKey(labelPropName))
                {
                    IRawProperty propLabel = card.Properties[labelPropName].FirstOrDefault();

                    // Remove _$!< and >!$_ around value.
                    string typeVal = propLabel.TextValue.Replace("_$!<", "").Replace(">!$_", "");

                    // Add itemN.X-ABLabel property value to the list of TYPE parameter values.
                    prop.Parameters.Add(new Parameter("TYPE", typeVal));

                    propNamesToDelete.Add(labelPropName);
                }

                // Add "PROP" propery instead of "itemN.PROP".
                card.AddProperty(fixedName, prop);

                propNamesToDelete.Add(keyValProps.Key);

                modified = true;
            }

            // Remove "itemX.PROP" props.
            propNamesToDelete.ForEach(propName => card.Properties.Remove(propName));

            return modified;
        }

        /// <summary>
        /// Repalaces itemN.PROP and itemN.X-ABLabel properties with a standard vCard properties. Moves itemN.X-ABLabel prop value into a TYPE parameter.
        /// </summary>
        /// <param name="card">Business card to process.</param>
        /// <returns>True if the vCard was modified, false otherwise.</returns>
        static internal bool Denormalize(ICard2 card)
        {
            List<IProperty> propsToDelete = new List<IProperty>();

            int i = 1;

            i = DenormalizePropertyList<IEmail2, EmailType>(card, card.Emails, "EMAIL", i, new string[] { "HOME", "WORK", "INTERNET" }, new string[] { "OTHER" });
            i = DenormalizePropertyList<IAddress2, AddressType>(card, card.Addresses, "ADR", i, new string[] { "HOME", "WORK", "OTHER" }, new string[] { });
            i = DenormalizePropertyList<ITelephone2, TelephoneType>(card, card.Telephones, "TEL", i, new string[] { "VOICE", "HOME", "WORK", "IPHONE", "CELL", "MAIN", "HOME", "FAX", "PAGER", "OTHER" }, new string[] { });
            i = DenormalizePropertyList<ICardUriProperty2, ExtendibleEnum>(card, card.Urls, "URL", i, new string[] { "HOME", "WORK" }, new string[] { "HOMEPAGE", "OTHER" });

            ICard3 card3 = card as ICard3;
            if (card3 != null)
            {
                i = DenormalizePropertyList<IInstantMessenger3, MessengerType>(card, card3.InstantMessengers, "IMPP", i, new string[] { }, new string[] { });

                // If there are any IMPPs left which does not have any TYPE specified create itemN.X-ABLabel for them.
                // If no label is added "IM_SERVICE_NAME" is displayed.
                i = DenormalizeMessengersList<IInstantMessenger3, MessengerType>(card, card3.InstantMessengers, i);
            }

            return i > 1;
        }

        static private int DenormalizePropertyList<T, E>(ICard2 card, ICardPropertyList<T> propsList, string propName, int index,
                IEnumerable<string> supportedStandardTypes, IEnumerable<string> customLabelMetaTypes)
            where T : class, ICardMultiProperty
            where E : ExtendibleEnum
        {
            List<IProperty> propsToDelete = new List<IProperty>();

            foreach (ITypedProperty<E> prop in propsList)
            {
                if (DenormalizeTypedProperty<E>(card, prop, propName, index, supportedStandardTypes, customLabelMetaTypes))
                {
                    propsToDelete.Add(prop);
                    index++;
                }
            }

            // Remove modified props.
            propsToDelete.ForEach(x => x.Remove());

            return index;
        }

        static private bool DenormalizeTypedProperty<E>(ICard2 card, ITypedProperty<E> prop, string propName, int index,
            IEnumerable<string> supportedStandardTypes, IEnumerable<string> customLabelMetaTypes) where E : ExtendibleEnum
        {
            // Find first non-standard type param and move it to item2.X-ABLabel property.
            foreach (E type in prop.Types)
            {
                string typeVal = type.Name;
                if (supportedStandardTypes.Contains(typeVal.ToUpper()))
                {
                    continue; // No need to change, continue searching.
                }

                if (customLabelMetaTypes.Contains(typeVal.ToUpper()))
                {
                    // Must be converted to itemN.X-ABLabel:_$!<Other>!$_
                    typeVal = string.Format("_$!<{0}>!$_", typeVal);
                }

                // Remove this param value from TYPE.
                prop.Types = prop.Types.Where(x => x != type).ToArray();

                // Add "itemX.PROP" property.
                string applePropName = string.Format("item10{0}.{1}", index, propName);
                card.AddProperty(applePropName, prop.RawProperty);

                // Add itemN.X-ABLabel property.
                string appleLabelPropName = string.Format("item10{0}.X-ABLabel", index);
                ITextProperty appleLabelProp = card.CreateTextProp(typeVal);
                appleLabelProp.RawProperty.SortIndex = prop.RawProperty.SortIndex;
                card.AddProperty(appleLabelPropName, appleLabelProp.RawProperty);

                return true;
            }

            return false;
        }

        static private int DenormalizeMessengersList<T, E>(ICard2 card, ICardPropertyList<T> propsList, int index)
            where T : class, ICardMultiProperty
            where E : ExtendibleEnum
        {
            List<IProperty> propsToDelete = new List<IProperty>();

            foreach (ITypedProperty<E> prop in propsList)
            {
                if (DenormalizeMessenger<E>(card, prop, index))
                {
                    propsToDelete.Add(prop);
                    index++;
                }
            }

            // Remove modified props.
            propsToDelete.ForEach(x => x.Remove());

            return index;
        }

        static private bool DenormalizeMessenger<E>(ICard2 card, ITypedProperty<E> prop, int index) where E : ExtendibleEnum
        {
            string typeVal = prop.RawProperty.TextValue.Split(':').FirstOrDefault() ?? "Messenger";
            if (typeVal.Length > 1)
                typeVal = typeVal.First().ToString().ToUpper() + typeVal.Substring(1); // Make first letter capital.

            // Add "itemX.PROP" property.
            string applePropName = string.Format("item10{0}.IMPP", index);
            prop.RawProperty.Parameters.Add(new Parameter("X-SERVICE-TYPE", typeVal));

            card.AddProperty(applePropName, prop.RawProperty);

            // Add itemN.X-ABLabel property.

            string appleLabelPropName = string.Format("item10{0}.X-ABLabel", index);
            ITextProperty appleLabelProp = card.CreateTextProp(typeVal);
            appleLabelProp.RawProperty.SortIndex = prop.RawProperty.SortIndex;
            card.AddProperty(appleLabelPropName, appleLabelProp.RawProperty);

            return true;
        }
    }
}