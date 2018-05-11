using System;
using System.Collections.Generic;
using System.Linq;
using XRL.Core;

namespace XRL.World.Parts
{
    [Serializable]
    public class rr_DeathReputation : IPart
    {
        public int repValue = 1;
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
                    // Figure out how much to adjust by (repValue * Level)
                    int repMod = repValue * ParentObject.Statistics["Level"].Value;
                    if (repMod == 0) return true;

                    // Adjust the players reputation.
                    Reputation myReputation = XRLCore.Core.Game.PlayerReputation;
                    foreach (KeyValuePair<string, int> item in ParentObject.pBrain.FactionMembership)
                    {
                        myReputation.modify(item.Key, -repMod, false);
                    }
                    if ((repMod / 4) > 0)
                    {
                        foreach (FriendorFoe relatedFaction in relatedFactions)
                        {
                            if (relatedFaction.status == "liked")
                            {
                                myReputation.modify(relatedFaction.faction, -repMod / 4, false);
                            }
                            else
                            {
                                myReputation.modify(relatedFaction.faction, repMod / 4, false);
                            }
                        }
                    }
                } 
                else
                {
                    /* This section tries to give the reputation to the killer of the Creature. Normally this is
                     * not important, but when dominating or when allys make a kill this can be useful to track. 
                     */

                    // Figure out how much to adjust by repValue
                    int repMod = repValue;
                    if (repMod == 0) return true;

                    // Get the brain of the killer.
                    Brain killersBrain = myKiller.GetPart("Brain") as Brain;
                    if (killersBrain == null) return true;

                    // Adjust the killers reputation.
                    foreach (KeyValuePair<string, int> item in ParentObject.pBrain.FactionMembership)
                    {
                        AddOrChangeFactionFeelings(killersBrain, item.Key, -repMod);
                    }
                    if ((repMod / 2) > 0)
                    {
                        foreach (FriendorFoe relatedFaction in relatedFactions)
                        {
                            if (relatedFaction.status == "liked")
                            {
                                AddOrChangeFactionFeelings(killersBrain, relatedFaction.faction, -repMod / 2);
                            }
                            else
                            {
                                AddOrChangeFactionFeelings(killersBrain, relatedFaction.faction, repMod / 2);
                            }
                        }
                    }
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
