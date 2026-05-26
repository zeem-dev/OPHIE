// ============================================================
//  OPHIO — CharacterData ScriptableObject
//  Core Data Layer
//  Blueprint for every playable character.
//  Create one asset per character in Assets/Characters/
//  Create > OPHIO > Character Data
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace OPHIO.Core
{
    [CreateAssetMenu(menuName = "OPHIO/Character Data", fileName = "CharacterData_New")]
    public class CharacterData : ScriptableObject
    {
        // --------------------------------------------------
        //  Identity
        // --------------------------------------------------
        [Header("Identity")]
        public string characterName   = "Unknown";
        public string subjectNumber   = "#0";
        [TextArea(2, 4)]
        public string shortLore       = "";
        public Sprite portrait;

        // --------------------------------------------------
        //  Base Stats
        // --------------------------------------------------
        [Header("Base Stats")]
        public int   maxHealth        = 100;
        public float maxEnergy        = 100f;
        public float energyRegen      = 5f;   // per second out of combat
        public float moveSpeed        = 5f;
        public float sprintMultiplier = 1.6f;
        public float baseMeleeDamage  = 20f;

        // --------------------------------------------------
        //  Primary Damage Type
        // --------------------------------------------------
        [Header("Primary Damage Type")]
        public DamageType primaryDamageType = DamageType.Physical;

        // --------------------------------------------------
        //  Passive Ability
        // --------------------------------------------------
        [Header("Passive")]
        public string passiveName = "Passive";
        [TextArea(2, 3)]
        public string passiveDesc = "";

        // --------------------------------------------------
        //  Ability Pool — all abilities this character CAN use
        //  Unlocked progressively — index = unlock level
        // --------------------------------------------------
        [Header("Ability Pool")]
        public List<AbilityData> abilityPool = new List<AbilityData>();

        // --------------------------------------------------
        //  Super Ability
        // --------------------------------------------------
        [Header("Super Ability")]
        public AbilityData superAbility;

        // --------------------------------------------------
        //  Audio
        // --------------------------------------------------
        [Header("Audio")]
        public AudioClip abilityActivateSound;
        public AudioClip hurtSound;
        public AudioClip deathSound;
    }
}
