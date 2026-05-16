/// <summary>Körperteil des Angreifers, mit dem ein Move ausgeführt wird.</summary>
public enum BodyPart
{
    L_Hand,
    R_Hand,
    L_Leg,
    R_Leg
}

/// <summary>Körperzone des Ziels, die von einem Move getroffen oder gelernt wird.</summary>
public enum TargetZone
{
    Head,
    Torso,
    L_Arm,
    R_Arm,
    L_Leg,
    R_Leg
}

/// <summary>Position eines Moves innerhalb einer Combo-Sequenz.</summary>
public enum ComboType
{
    Opener,
    Chain,
    Finisher
}

/// <summary>Magisches Element, das einem Move zusätzliche Eigenschaften verleiht.</summary>
public enum MagicTag
{
    Fire,
    Lightning,
    Ice,
    Wind
}
