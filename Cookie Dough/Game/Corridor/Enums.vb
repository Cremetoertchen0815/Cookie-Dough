Namespace Game.Corridor
    Public Enum SpielfigurType
        König
        Dame
        Turm
        Läufer
        Springer
        Bauer
        Debug
    End Enum

    Public Enum SpielStatus
        WähleFigur
        FahreZug
        SpielZuEnde = 3
        WarteAufOnlineSpieler = 4
        Waitn = 5
    End Enum
End Namespace