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
    
    /// <summary>
    /// The type of perceptual effect triggered when the player consumes a pill.
    /// Each fear has its own effect — defined per FearProfileSO.
    /// </summary>
    public enum PillEffectType
    {
        None,
        LightSurge,       // Fear A — darkness flooded with light
        SpaceExpansion    // Fear B — environment feels vast and open (future expansion) 
    }
    
    /// <summary>
    /// Traversal skills unlocked on hard paths. Persist across levels.
    /// Each skill corresponds to a hard path mechanic introduced in a specific level.
    /// </summary>
    public enum SkillID
    {
        None,
        ObjectRotation,  // Fear A — Level 1 hard path
        Swimming         // Fear A — Level 2 hard path
    }
    
    /// <summary>
    /// Records which path the player chose at each corridor split.
    /// </summary>
    public enum PathChoice
    {
        None,
        Easy,
        Hard
    }
    
    // Interaction types — used by IInteractable and InteractionCursor.
    // Add new values here to extend the cursor system with new interaction states.
    public enum InteractionType
    {
        None,
        Examine,
        Rotate,
        Use
    }
}
