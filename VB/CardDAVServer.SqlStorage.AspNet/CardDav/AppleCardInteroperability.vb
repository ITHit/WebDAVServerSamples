Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports ITHit.Collab
Imports ITHit.Collab.Card

Namespace CardDav

    ''' <summary>
    ''' Converts vCard properties submitted by iOS and OS X to the format that is 
    ''' understood by any CardDAV client application. Also converts standard vCard 
    ''' props to format that could be understood by iOS and OS X CardDAV client.
    ''' </summary>
    Friend Class AppleCardInteroperability

        ''' <summary>
        ''' List of User-Agent header prefixes for which properties replacement must be applied.
        ''' </summary>
        Shared Private userAgentsPrefixes As String() = {"iOS", "Mac OS X"}

        Shared Friend Function NeedsConversion(userAgent As String) As Boolean
            Return userAgentsPrefixes.FirstOrDefault(Function(x) userAgent.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)) IsNot Nothing
        End Function

        Friend Shared Function GetClientAppName(userAgent As String) As String
            Dim aAgent As String() = userAgent.Split({"/"c}, StringSplitOptions.RemoveEmptyEntries)
            Dim clientAppName As String = aAgent.FirstOrDefault()
            If clientAppName Is Nothing Then Return Nothing
            If NeedsConversion(userAgent) Then Return "Apple Inc."
            Return clientAppName
        End Function

        Shared Friend Function Convert(userAgent As String, card As ICard2) As Boolean
            If card.ProductId Is Nothing OrElse card.ProductId.Text Is Nothing Then Return False
            Dim aProductId As String() = card.ProductId.Text.Split(New String() {"//"}, StringSplitOptions.RemoveEmptyEntries)
            If aProductId.Length < 3 Then Return False
            Dim product As String = aProductId(2)
            If NeedsConversion(userAgent) AndAlso Not NeedsConversion(product) Then
                Return Denormalize(card)
            ElseIf Not NeedsConversion(userAgent) AndAlso NeedsConversion(product) Then
                Return Normalize(card)
            End If

            Return False
        End Function

        Shared Friend Function Normalize(card As ICard2) As Boolean
            Dim modified As Boolean = False
            If NormalizeProps(card) Then
                modified = True
            End If

            Dim birthday As IDate2 = card.BirthDate
            If(birthday IsNot Nothing) AndAlso (birthday.Value IsNot Nothing) AndAlso birthday.Value.DateVal.Year = 1604 Then
                birthday.Value = New [Date](birthday.Value.DateVal, DateComponents.Month Or DateComponents.Day)
                modified = True
            End If

            Return modified
        End Function

        Shared Private Function NormalizeProps(card As ICard2) As Boolean
            Dim modified As Boolean = False
            Dim appleProps = card.Properties.Where(Function(x) x.Key.Contains(".")).ToList()
            Dim propNamesToDelete As List(Of String) = New List(Of String)()
            For Each keyValProps As KeyValuePair(Of String, IList(Of IRawProperty)) In appleProps
                Dim dotIndex As Integer = keyValProps.Key.IndexOf("."c)
                If(dotIndex < 0) OrElse (dotIndex = keyValProps.Key.Length - 1) Then Continue For
                Dim fixedName As String = keyValProps.Key.Substring(dotIndex + 1)
                If fixedName.StartsWith("X-") Then Continue For
                Dim prop As IRawProperty = keyValProps.Value.First()
                Dim labelPropName As String = keyValProps.Key.Substring(0, dotIndex) & ".X-ABLabel"
                If card.Properties.ContainsKey(labelPropName) Then
                    Dim propLabel As IRawProperty = card.Properties(labelPropName).FirstOrDefault()
                    Dim typeVal As String = propLabel.TextValue.Replace("_$!<", "").Replace(">!$_", "")
                    ' Add itemN.X-ABLabel property value to the list of TYPE parameter values.
                    prop.Parameters.Add(New Parameter("TYPE", typeVal))
                    propNamesToDelete.Add(labelPropName)
                End If

                ' Add "PROP" propery instead of "itemN.PROP".
                card.AddProperty(fixedName, prop)
                propNamesToDelete.Add(keyValProps.Key)
                modified = True
            Next

            ' Remove "itemX.PROP" props.
            propNamesToDelete.ForEach(Sub(propName) card.Properties.Remove(propName))
            Return modified
        End Function

        Shared Friend Function Denormalize(card As ICard2) As Boolean
            Dim propsToDelete As List(Of IProperty) = New List(Of IProperty)()
            Dim i As Integer = 1
            i = DenormalizePropertyList(Of IEmail2, EmailType)(card, card.Emails, "EMAIL", i, New String() {"HOME", "WORK", "INTERNET"}, New String() {"OTHER"})
            i = DenormalizePropertyList(Of IAddress2, AddressType)(card, card.Addresses, "ADR", i, New String() {"HOME", "WORK", "OTHER"}, New String() {})
            i = DenormalizePropertyList(Of ITelephone2, TelephoneType)(card, card.Telephones, "TEL", i, New String() {"VOICE", "HOME", "WORK", "IPHONE", "CELL", "MAIN", "HOME", "FAX", "PAGER", "OTHER"}, New String() {})
            i = DenormalizePropertyList(Of ICardUriProperty2, ExtendibleEnum)(card, card.Urls, "URL", i, New String() {"HOME", "WORK"}, New String() {"HOMEPAGE", "OTHER"})
            Dim card3 As ICard3 = TryCast(card, ICard3)
            If card3 IsNot Nothing Then
                i = DenormalizePropertyList(Of IInstantMessenger3, MessengerType)(card, card3.InstantMessengers, "IMPP", i, New String() {}, New String() {})
                ' If there are any IMPPs left which does not have any TYPE specified create itemN.X-ABLabel for them.
                ' If no label is added "IM_SERVICE_NAME" is displayed.
                i = DenormalizeMessengersList(Of IInstantMessenger3, MessengerType)(card, card3.InstantMessengers, i)
            End If

            Return i > 1
        End Function

        Shared Private Function DenormalizePropertyList(Of T As {Class, ICardMultiProperty}, E As ExtendibleEnum)(card As ICard2, propsList As ICardPropertyList(Of T), propName As String, index As Integer,
                                                                                                                 supportedStandardTypes As IEnumerable(Of String), customLabelMetaTypes As IEnumerable(Of String)) As Integer
            Dim propsToDelete As List(Of IProperty) = New List(Of IProperty)()
            For Each prop As ITypedProperty(Of E) In propsList
                If DenormalizeTypedProperty(Of E)(card, prop, propName, index, supportedStandardTypes, customLabelMetaTypes) Then
                    propsToDelete.Add(prop)
                    index += 1
                End If
            Next

            ' Remove modified props.
            propsToDelete.ForEach(Sub(x) x.Remove())
            Return index
        End Function

        Shared Private Function DenormalizeTypedProperty(Of E As ExtendibleEnum)(card As ICard2, prop As ITypedProperty(Of E), propName As String, index As Integer,
                                                                                supportedStandardTypes As IEnumerable(Of String), customLabelMetaTypes As IEnumerable(Of String)) As Boolean
            For Each type As E In prop.Types
                Dim typeVal As String = type.Name
                If supportedStandardTypes.Contains(typeVal.ToUpper()) Then
                    Continue For
                End If

                If customLabelMetaTypes.Contains(typeVal.ToUpper()) Then
                    ' Must be converted to itemN.X-ABLabel:_$!<Other>!$_
                    typeVal = String.Format("_$!<{0}>!$_", typeVal)
                End If

                ' Remove this param value from TYPE.
                prop.Types = prop.Types.Where(Function(x) x <> type).ToArray()
                Dim applePropName As String = String.Format("item10{0}.{1}", index, propName)
                card.AddProperty(applePropName, prop.RawProperty)
                Dim appleLabelPropName As String = String.Format("item10{0}.X-ABLabel", index)
                Dim appleLabelProp As ITextProperty = card.CreateTextProp(typeVal)
                appleLabelProp.RawProperty.SortIndex = prop.RawProperty.SortIndex
                card.AddProperty(appleLabelPropName, appleLabelProp.RawProperty)
                Return True
            Next

            Return False
        End Function

        Shared Private Function DenormalizeMessengersList(Of T As {Class, ICardMultiProperty}, E As ExtendibleEnum)(card As ICard2, propsList As ICardPropertyList(Of T), index As Integer) As Integer
            Dim propsToDelete As List(Of IProperty) = New List(Of IProperty)()
            For Each prop As ITypedProperty(Of E) In propsList
                If DenormalizeMessenger(Of E)(card, prop, index) Then
                    propsToDelete.Add(prop)
                    index += 1
                End If
            Next

            ' Remove modified props.
            propsToDelete.ForEach(Sub(x) x.Remove())
            Return index
        End Function

        Shared Private Function DenormalizeMessenger(Of E As ExtendibleEnum)(card As ICard2, prop As ITypedProperty(Of E), index As Integer) As Boolean
            Dim typeVal As String = If(prop.RawProperty.TextValue.Split(":"c).FirstOrDefault(), "Messenger")
            If typeVal.Length > 1 Then typeVal = typeVal.First().ToString().ToUpper() & typeVal.Substring(1)
            Dim applePropName As String = String.Format("item10{0}.IMPP", index)
            prop.RawProperty.Parameters.Add(New Parameter("X-SERVICE-TYPE", typeVal))
            card.AddProperty(applePropName, prop.RawProperty)
            Dim appleLabelPropName As String = String.Format("item10{0}.X-ABLabel", index)
            Dim appleLabelProp As ITextProperty = card.CreateTextProp(typeVal)
            appleLabelProp.RawProperty.SortIndex = prop.RawProperty.SortIndex
            card.AddProperty(appleLabelPropName, appleLabelProp.RawProperty)
            Return True
        End Function
    End Class
End Namespace
