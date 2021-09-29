﻿#if EC
using ADVPart.Manipulate;
using ADVPart.Manipulate.Chara;
using HPlay;
using UnityEngine;
#endif
#if KK || KKS
#endif

namespace MoreAccessoriesKOI
{
    public partial class MoreAccessories
    {
        public const string versionNum = "2.0.10";
        public const string GUID = "com.joan6694.illusionplugins.moreaccessories";

        public static MoreAccessories _self;

        private const int _saveVersion = 2;
        private const string _extSaveKey = "moreAccessories";
        internal static CharAdditionalData PreviousMigratedData;

        internal static bool _hasDarkness;
        public bool ImportingCards { get; private set; } = true;
        internal static bool CharaMaker => MakerMode != null;

        private static bool BackwardCompatibility = true; //Do not turn back on once off.

        public static MakerMode MakerMode { get; internal set; }
#if KK || KKS
        internal static bool InH => HMode != null;
        public static HScene HMode { get; internal set; }

        internal static bool InStudio => StudioMode != null;
        public static StudioClass StudioMode { get; internal set; }
#elif EC
        internal bool InPlayMode => PlayMode != null;
        public static PlayMode PlayMode;
#if false
        internal bool InADVMode => ADVMode != null;
        public static ADVMode ADVMode;
#endif
#endif
    }
}
