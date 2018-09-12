using System;
using System.Collections.Generic;
using System.Linq;
using XRL.Core;
using XRL.Messages;

namespace XRL.World.Parts
{
    [Serializable]
    public class rr_DeathReputation : IPart
    {
        public float repValue = 1.0f;
        public float repScale = 30.0f;
        public float levelScale = 1.0f;
        public float relationScale = 4.0f;
        public int gravitateOffset = 300;

        public string descriptionPostfix = string.Empty;
        [NonSerialized]
        public List<string> parentFactions = new List<string>();
        [NonSerialized]
        public List<FriendorFoe> relatedFactions = new List<FriendorFoe>();

        public rr_DeathReputation()
        {
            this.Name = "rr_DeathReputation";
        }

        public override void SaveData(SerializationWriter Writer)
        {
            Writer.Write(parentFactions.Count);
            foreach (string parentFaction in parentFactions)
                Writer.Write(parentFaction);
            Writer.Write(relatedFactions.Count);
            foreach (FriendorFoe relatedFaction in relatedFactions)
            {
                Writer.Write(relatedFaction.faction);
                Writer.Write(relatedFaction.status);
            }
            base.SaveData(Writer);
        }

        public override void LoadData(SerializationReader Reader)
        {
            int numParentFactions = Reader.ReadInt32();
            for (int index = 0; index < numParentFactions; ++index)
                parentFactions.Add(Reader.ReadString());
            int numRelatedFactions = Reader.ReadInt32();
            for (int index = 0; index < numRelatedFactions; ++index)
                relatedFactions.Add(new FriendorFoe(Reader.ReadString(), Reader.ReadString(), string.Empty));
            base.LoadData(Reader);
        }

        public override bool SameAs(IPart p)
        {
            return true;
        }

        public override void Register(GameObject GO)
        {
            GO.RegisterPartEvent(this, "BeforeDeathRemoval");
            GO.RegisterPartEvent(this, "FactionsAdded");
            GO.RegisterPartEvent(this, "GetShortDescription");
        }

        private void AddOrChangeFactionFeelings(Brain targetBrain, string faction, int amount)
        {
            if (targetBrain.FactionFeelings.ContainsKey(faction))
            {
                targetBrain.FactionFeelings[faction] += amount;
            }
            else
            {
                targetBrain.FactionFeelings.Add(faction, amount);
            }
        }

        public override bool FireEvent(Event E)
        {
            // Stop if this creature already gives reputation.
            if (ParentObject.GetPart("GivesRep") != null) return base.FireEvent(E);

            // Stop of this creature is the player.
            if (ParentObject.IsPlayer()) return base.FireEvent(E);

            // Add the Factions
            if (E.ID == "FactionsAdded")
            {
                int index = 0;

                // Get Parent Factions
                foreach (KeyValuePair<string, int> item in ParentObject.pBrain.FactionMembership)
                {
                    parentFactions.Add(item.Key);
                }

                if (parentFactions.Count <= 0) return false;

                // Format the Parent Factions list into text
                descriptionPostfix += "&C-----&y\nLoved by";
                foreach (string parentFaction in parentFactions)
                {
                    descriptionPostfix += " &C" + Faction.getFormattedName(parentFaction) + "&y,";
                }
                descriptionPostfix = descriptionPostfix.Remove(descriptionPostfix.Length - 1);
                descriptionPostfix += "&y.\n";

                if (parentFactions.Count > 1)
                {
                    int locationOf = descriptionPostfix.LastIndexOf(",");
                    descriptionPostfix = descriptionPostfix.Insert(locationOf + 1, " and");
                }

                // Pick 1-3 Factions to Like/Dislike/Hate
                int maxFactions = Rules.Stat.Random(1, 3);

                // Adjust the feelings towards other Factions
                Brain myBrain = ParentObject.GetPart("Brain") as Brain;
                string myFaction = myBrain.GetPrimaryFaction();
                for (index = 1; index <= maxFactions; index++)
                {
                    int randPercent = Rules.Stat.Random(1, 100);
                    int factionChange = -100;
                    if (randPercent <= 10) factionChange = 100;
                    else if (randPercent <= 55) factionChange = 0;

                    string FoF = GenerateFriendOrFoe.getRandomFaction(ParentObject);
                    factionChange += Factions.GetFeelingFactionToFaction(myFaction, FoF);
                    if (factionChange > 100) factionChange = 100;
                    if (factionChange < -100) factionChange = -100;
                    
                    AddOrChangeFactionFeelings(myBrain, FoF, factionChange);
                }

                Dictionary<string, int> tempRelatedFactions = new Dictionary<string, int>();

                // Add all the factions with a significant amount to the list.
                foreach (KeyValuePair<string, Faction> item in Factions.FactionList)
                {
                    if (item.Value.Name == myFaction) continue;
                    if (item.Value.bVisible == false) continue;

                    int factionAmount = Factions.GetFeelingFactionToFaction(myFaction, item.Value.Name);
                    if (myBrain.FactionFeelings.ContainsKey(item.Value.Name))
                    {
                        factionAmount = myBrain.FactionFeelings[item.Value.Name];
                    }

                    if (factionAmount < 0 || factionAmount > 50)
                    {
                        tempRelatedFactions.Add(item.Value.Name, factionAmount);
                    }
                }

                for (index = 1; index <= maxFactions; index++)
                {
                    if (tempRelatedFactions.Count <= 0) break;

                    KeyValuePair<string, int> item = tempRelatedFactions.GetRandomElement((System.Random)null);
                    if (item.Value > 50)
                    {
                        string reason = GenerateFriendOrFoe.getLikeReason();
                        relatedFactions.Add(new FriendorFoe(item.Key, "friend", reason));
                    }
                    else
                    {
                        string reason = GenerateFriendOrFoe.getHateReason();
                        relatedFactions.Add(new FriendorFoe(item.Key, "dislike", reason));
                    }
                    tempRelatedFactions.Remove(item.Key);
                }


                // Count and sort the factions by category (friend, dislike, hate)
                if (relatedFactions.Count <= 0) return true;

                int friendCount = 0;
                int hateCount = 0;
                string friendString = "\nAdmired by ";
                string hateString = "\nHated by";
                foreach (FriendorFoe relatedFaction in relatedFactions)
                {
                    if(relatedFaction.status == "friend")
                    {
                        friendString += " &C" + Faction.getFormattedName(relatedFaction.faction) + "&y,";
                        friendCount += 1;
                    }
                    else
                    {
                        hateString += " &C" + Faction.getFormattedName(relatedFaction.faction) + "&y,";
                        hateCount += 1;
                    }
                }
                if (friendCount > 0)
                {
                    friendString = friendString.Remove(friendString.Length - 1);
                    friendString += "&y.\n";
                    if (friendCount > 1)
                    {
                        int locationOf = friendString.LastIndexOf(",");
                        friendString = friendString.Insert(locationOf + 1, " and");
                    }
                    descriptionPostfix += friendString;
                }
                if (hateCount > 0)
                {
                    hateString = hateString.Remove(hateString.Length - 1);
                    hateString += "&y.\n";
                    if (hateCount > 1)
                    {
                        int locationOf = hateString.LastIndexOf(",");
                        hateString = hateString.Insert(locationOf + 1, " and");
                    }
                    descriptionPostfix += hateString;
                }

                return true;
            }
            else if(E.ID == "BeforeDeathRemoval")
            {
                // Give/Take Reputation when the creature is killed.
                GameObject myKiller = E.GetParameter("Killer") as GameObject;
                if (myKiller == null) return true;

                // Did the player kill this Creature?
                if (myKiller.IsPlayer())
                {
                    // Adjust the players reputation.
                    Reputation myReputation = XRLCore.Core.Game.PlayerReputation;
                    foreach (KeyValuePair<string, int> item in ParentObject.pBrain.FactionMembership)
                    {

                        // Calculate an adjust 'weight'
                        // 1) Does the player have a positive or negative relationship to this faction.
                        //    - Player has a positive relationship to this creature their reputation drops more.
                        //      (You are friendly they are not going to like that.)
                        //    - Player has a negitive relationship to this creature their reputation drops less.
                        //      (You are already hated, they are not going to hate you that much more)
                        //    - This should be the major factor in the equation.
                        // 2) Is the creature greater or lesser level than the player.
                        //    - More difficult creatures offer more reputation.
                        //    - Easier creatures offer less reputation.
                        //    - This should be a minor adjustment.

                        // Get the current reputation.
                        int currentRep = myReputation.get(item.Key);
                        //MessageQueue.AddPlayerMessage("currentRep " + currentRep);

                        // Scale the reputation based on the current reputation.
                        float fRep = 0;
                        //if (currentRep > 0) fRep = repValue * (currentRep / repScale);
                        //else if (currentRep < 0) fRep = repValue / (-currentRep / repScale);
                        if (currentRep > -gravitateOffset) fRep = repValue * ((currentRep + gravitateOffset) / repScale);
                        else if (currentRep < -gravitateOffset) fRep = repValue / (-(currentRep + gravitateOffset) / repScale);
                        //MessageQueue.AddPlayerMessage("fRep " + fRep);

                        // Get the level differences.
                        int creatureLevel = ParentObject.Statistics["Level"].Value;
                        int playerLevel = XRLCore.Core.Game.Player.Body.Statistics["Level"].Value;

                        // Scale the reputation based on the player vs creature levels.
                        int dLevel = creatureLevel - playerLevel;
                        if (dLevel > 0) fRep = fRep * (dLevel / levelScale);
                        else if (dLevel < 0) fRep = fRep / (-dLevel / levelScale);
                        //MessageQueue.AddPlayerMessage("fRep " + fRep);

                        currentRep = (int)Math.Floor(fRep);
                        //MessageQueue.AddPlayerMessage("currentRep " + currentRep);

                        // Only modify if the reputation change is not 0.
                        if (currentRep != 0) myReputation.modify(item.Key, -currentRep, false);
                    }

                    foreach (FriendorFoe relatedFaction in relatedFactions)
                    {
                        // Get the current reputation.
                        int currentRep = myReputation.get(relatedFaction.faction);
                        //MessageQueue.AddPlayerMessage("currentRep " + currentRep);

                        // Scale the reputation based on the current reputation.
                        float fRep = 0;
                        //if (currentRep > 0) fRep = repValue * (currentRep / repScale);
                        //else if (currentRep < 0) fRep = repValue / (-currentRep / repScale);
                        if (currentRep > -gravitateOffset) fRep = repValue * ((currentRep + gravitateOffset) / repScale);
                        else if (currentRep < -gravitateOffset) fRep = repValue / (-(currentRep + gravitateOffset) / repScale);
                        //MessageQueue.AddPlayerMessage("fRep " + fRep);

                        // Get the level differences.
                        int creatureLevel = ParentObject.Statistics["Level"].Value;
                        int playerLevel = XRLCore.Core.Game.Player.Body.Statistics["Level"].Value;

                        // Scale the reputation based on the player vs creature levels.
                        int dLevel = creatureLevel - playerLevel;
                        if (dLevel > 0) fRep = fRep * (dLevel / levelScale);
                        else if (dLevel < 0) fRep = fRep / (-dLevel / levelScale);
                        //MessageQueue.AddPlayerMessage("fRep " + fRep);

                        fRep = fRep / relationScale;
                        currentRep = (int)Math.Floor(fRep);
                        //MessageQueue.AddPlayerMessage("currentRep " + currentRep);

                        if (relatedFaction.status == "liked")
                        {
                            // Only modify if the reputation change is not 0.
                            if (currentRep != 0) myReputation.modify(relatedFaction.faction, -currentRep, false);
                        }
                        else
                        {
                            // Only modify if the reputation change is not 0.
                            if (currentRep != 0) myReputation.modify(relatedFaction.faction, currentRep, false);
                        }
                    }
                } 
                else
                {
                    /* Removed for Now; Where to adjust reputation for creature on creature combat */
                }

                return true;
            }
            else if (E.ID == "GetShortDescription")
            {
                E.AddParameter("Postfix", E.GetParameter("Postfix") + descriptionPostfix);
            }
            return base.FireEvent(E);
        }
    }
}
