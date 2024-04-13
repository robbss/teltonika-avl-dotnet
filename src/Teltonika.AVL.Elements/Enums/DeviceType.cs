namespace Teltonika.AVL.Elements;

public enum DeviceType
{
    #region Basic Trackers

    FMB900,
    FMB910,
    FMB920,
    FMC920,
    FMM920,
    FMB930,

    #endregion Basic Trackers

    #region OBD Trackers

    FMB001,
    FMB002,
    FMB003,
    FMC001,
    FMC003,
    FMC00A,
    FMM001,
    FMM003,
    FMM00A,

    #endregion OBD Trackers

    #region CAN Trackers & Adapters

    FMB140,
    FMB150,
    FMC150,
    FMM150,
    LV_CAN200,
    ALL_CAN300,
    CAN_CONTROL,
    ECAN02,
    FMB240,

    #endregion CAN Trackers & Adapters

    #region Fast & Easy Trackers

    FMP100,
    FMB010,
    FMB020,
    FMC800,
    FMM800,
    FMM80A,
    FMT100,
    FMC880,
    FMM880,
    FTC881,

    #endregion Fast & Easy Trackers

    #region Advanced Trackers

    FMB110,
    FMB120,
    FMB122,
    FMB130,
    FMC130,
    FMC13A,
    FMM130,
    FMM13A,
    MSP500,
    FMB202,
    FMB204,
    FMB209,
    FMB230,
    FMC230,
    FMC234,
    FMM230,

    #endregion Advanced Trackers

    #region Autonomous Trackers

    TAT100,
    TAT140,
    TAT141,
    TAT240,
    TMT250,
    GH5200,

    #endregion Autonomous Trackers

    #region Sensors

    EYE_Beacon,
    EYE_Sensor,

    #endregion Sensors

    #region Professional Trackers

    FMB641,
    FMC650,
    FMM650,
    FMB125,
    FMC125,
    FMM125,
    FMB225,
    FMC225,

    #endregion Professional Trackers

    #region E-Mobility Trackers

    TST100,
    TFT100,

    #endregion E-Mobility Trackers

    #region Legacy

    FM1100,
    FM2200

    #endregion Legacy
}