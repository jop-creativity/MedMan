namespace MedMan.Core
{
    /// <summary>
    /// Top-level game states. Defines which narrative section the player is currently in.
    /// </summary>
    public enum GameState
    {
        None,
        MainMenu,
        DoctorsOffice,
        HotelRoom,
        Dream,
        Epilogue
    }

    /// <summary>
    /// The type of fear selected by the player at the doctor's office.
    /// Adding a new fear = new entry here. Zero changes to any other code.
    /// </summary>
    public enum FearType
    {
        None,
        Fear_A,  // Darkness / loss of daughter
        Fear_B   // Claustrophobia / brother in cave (future expansion)
    }

    /// <summary>
    /// The dream level within the active fear narrative.
    /// </summary>
    public enum DreamLevel
    {
        None,
        Level1,
        Level2,
        Level3
    }
}
