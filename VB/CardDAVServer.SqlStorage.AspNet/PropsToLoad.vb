''' <summary>
''' Specifies which properties should be loaded.
''' </summary>
Public Enum PropsToLoad
    ''' <summary>
    ''' Used for OPTIONS, COPY, MOVE, DELETE
    ''' </summary>
    None
    ''' <summary>
    ''' Used for PROPFIND (GetChildren call)
    ''' </summary>
    Minimum
    ''' <summary>
    ''' Used for DET
    ''' </summary>
    All
End Enum
