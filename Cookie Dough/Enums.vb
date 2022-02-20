''' <summary>
''' Lists the games in the collection
''' </summary>
Public Enum GameType
    BetretenVerboten
    CarCrash
    Corridor
    Pain
    DuoCard
    Peng
    Megäa
    Barrelled
    DropTrop
End Enum
''' <summary>
''' Lists the types of playyers
''' </summary>
Public Enum SpielerTyp
    Local = 0
    CPU = 1
    None = 2
    Online = 3
End Enum
''' <summary>
''' Indicates whether a round is competitive
''' </summary>
Public Enum GameMode
    Casual = 0
    Competetive = 1
End Enum
''' <summary>
''' Defines the difficulty of the CPUs
''' </summary>
Public Enum Difficulty
    Brainless = 0
    Smart = 1
End Enum
''' <summary>
''' Indicates the type of sound preset being used
''' </summary>
Public Enum IdentType
    TypeA
    TypeB
    TypeC
    TypeD
    TypeE
    TypeF
    Custom
End Enum

''' <summary>
''' Describes what kind of ending the server game loop experienced
''' </summary>
Public Enum EndingMode
    Running
    Properly
    Abruptly
End Enum