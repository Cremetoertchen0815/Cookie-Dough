Namespace Game.BetretenVerboten

    Public Enum SpielStatus
        Würfel = 0
        WähleFigur = 1
        FahreFelder = 2
        SpielZuEnde = 3
        WarteAufOnlineSpieler = 4
        Waitn = 5
        SaucerFlight = 6
        WähleOpfer = 7
    End Enum

    Public Enum PlayFieldPos
        Home1
        Home2
        Home3
        Home4
        Feld1
        Feld2
        Feld3
        Feld4
        Feld5
        Feld6
        Feld7
        Feld8
        Feld9
        Feld10
        Haus1
        Haus2
        Haus3
        Haus4
    End Enum

    Public Enum Difficulty
        Brainless = 0
        Smart = 1
    End Enum

    Public Enum GaemMap
        Default4Players = 0
        Default6Players = 1
    End Enum
End Namespace