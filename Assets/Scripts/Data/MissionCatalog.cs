using System.Collections.Generic;
using UnityEngine;

namespace StateCraft.Data
{
    public static class MissionCatalog
    {
        private static List<MissionDefinitionSO> _missions = null;

        public static List<MissionDefinitionSO> GetAll()
        {
            if (_missions == null)
            {
                _missions = new List<MissionDefinitionSO>(Resources.LoadAll<MissionDefinitionSO>("Missions"));
                
                // Fallback / Default in-memory definitions if Resources folder is empty!
                if (_missions.Count == 0)
                {
                    _missions.Add(CreateDef("ground_assault", "Ground Assault", "Conduct general land operations using mechanized infantry.", MilitaryBranch.Army, new List<MilitaryBranch> { MilitaryBranch.Army }, 60f, 1f));
                    _missions.Add(CreateDef("counter_insurgency", "Counter Insurgency", "Suppress insurgent networks and stabilize the region.", MilitaryBranch.Army, new List<MilitaryBranch> { MilitaryBranch.Army }, 50f, 1f));
                    _missions.Add(CreateDef("aerial_recon", "Aerial Recon", "Gather intelligence using drone sorties and recon planes.", MilitaryBranch.AirForce, new List<MilitaryBranch> { MilitaryBranch.AirForce }, 40f, 0f));
                    _missions.Add(CreateDef("aerial_strike", "Aerial Strike", "Precision air strike to destroy hostile infrastructure.", MilitaryBranch.AirForce, new List<MilitaryBranch> { MilitaryBranch.AirForce }, 70f, 1f));
                    _missions.Add(CreateDef("coastal_patrol", "Coastal Patrol", "Secure national waters against piracy and smuggling.", MilitaryBranch.Navy, new List<MilitaryBranch> { MilitaryBranch.Navy }, 45f, 0f));
                    _missions.Add(CreateDef("special_ops", "Special Operations", "Infiltrate and eliminate high-value targets behind enemy lines.", MilitaryBranch.RDF, new List<MilitaryBranch> { MilitaryBranch.RDF }, 75f, 2f));
                    _missions.Add(CreateDef("border_defense", "Border Defense", "Deploy a joint defensive line to halt enemy invasion at the border.", MilitaryBranch.Army, new List<MilitaryBranch> { MilitaryBranch.Army, MilitaryBranch.AirForce }, 80f, 1f));
                    
                    // Joint Missions
                    _missions.Add(CreateDef("amphibious_landing", "Amphibious Landing", "Conduct coordinated beachhead operations requiring Army and Navy cooperation.", MilitaryBranch.Navy, new List<MilitaryBranch> { MilitaryBranch.Army, MilitaryBranch.Navy }, 80f, 2f));
                    _missions.Add(CreateDef("close_air_support", "Close Air Support", "Coordinated army ground push supported by air force precision strikes.", MilitaryBranch.Army, new List<MilitaryBranch> { MilitaryBranch.Army, MilitaryBranch.AirForce }, 75f, 1f));
                    _missions.Add(CreateDef("joint_blockade", "Joint Blockade", "Establish a maritime exclusion zone with air superiority protection.", MilitaryBranch.Navy, new List<MilitaryBranch> { MilitaryBranch.Navy, MilitaryBranch.AirForce }, 85f, 1f));
                    _missions.Add(CreateDef("theater_strike", "Theater Strike Force", "Full-scale joint operation combining Army, Navy, and Air Force elements.", MilitaryBranch.Army, new List<MilitaryBranch> { MilitaryBranch.Army, MilitaryBranch.AirForce, MilitaryBranch.Navy }, 90f, 2f));
                }
            }
            return _missions;
        }

        public static MissionDefinitionSO Get(string missionId)
        {
            return GetAll().Find(m => m.missionId == missionId);
        }

        private static MissionDefinitionSO CreateDef(string id, string name, string desc, MilitaryBranch lead, List<MilitaryBranch> reqs, float successWeight, float risk)
        {
            var def = ScriptableObject.CreateInstance<MissionDefinitionSO>();
            def.missionId = id;
            def.displayName = name;
            def.description = desc;
            def.leadBranch = lead;
            def.requiredBranches = reqs;
            def.baseSuccessWeight = successWeight;
            def.baseCasualtyRisk = risk;
            return def;
        }
    }
}
