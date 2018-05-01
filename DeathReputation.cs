using System;
using System.Collections.Generic;
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

                int numParents = parentFactions.Count;
                if (numParents == 0) return false;

                // Format the list into text
                descriptionPostfix += "&C-----&y\nLoved by &C";

                for (index = 0; index < numParents - 2; ++index)
                {
                    descriptionPostfix += Faction.getFormattedName(parentFactions[index]) + "&y, &C";
                }
                descriptionPostfix += Faction.getFormattedName(parentFactions[numParents - 1]);

                if (numParents > 1)
                {
                    descriptionPostfix += "&y, and &C" + Faction.getFormattedName(parentFactions[numParents - 2]);
                }
                descriptionPostfix += "&y.\n";

                // Pick 1-3 Factions to Like/Dislike/Hate
                int maxFactions = Rules.Stat.Random(1, 3);

                // Adjust the feelings towards other Factions
                Brain myBrain = ParentObject.GetPart("Brain") as Brain;
                for (index = 1; index <= maxFactions; ++index)
                {
                    int randPercent = Rules.Stat.Random(1, 100);
                    int factionChange = -100;
                    if (randPercent <= 10) factionChange = 100;
                    else if (randPercent <= 55) factionChange = -50;

                    string FoF = GenerateFriendOrFoe.getRandomFaction(ParentObject);
                    AddOrChangeFactionFeelings(myBrain, FoF, factionChange);
                }

                // Add all the factions with a significant amount to the list.
                string myFaction = myBrain.GetPrimaryFaction();
                foreach (KeyValuePair<string, Faction> item in Factions.FactionList)
                {
                    if (item.Value.Name == myFaction) continue;

                    int factionAmount = Factions.GetFeelingFactionToFaction(myFaction, item.Value.Name);
                    if (myBrain.FactionFeelings.ContainsKey(item.Value.Name))
                    {
                        factionAmount += myBrain.FactionFeelings[item.Value.Name];
                    }
                    if (factionAmount > 50)
                    {
                        string reason = GenerateFriendOrFoe.getLikeReason();
                        relatedFactions.Add(new FriendorFoe(item.Key, "friend", reason));
                    }
                    else if (factionAmount <= -100)
                    {
                        string reason = GenerateFriendOrFoe.getHateReason();
                        relatedFactions.Add(new FriendorFoe(item.Key, "hate", reason));
                    }
                    else if (factionAmount < 0)
                    {
                        string reason = GenerateFriendOrFoe.getHateReason();
                        relatedFactions.Add(new FriendorFoe(item.Key, "dislike", reason));
                    }
                }

                // Count and sort the factions by category (friend, dislike, hate)
                int friendCount = 0;
                int dislikeCount = 0;
                int hateCount = 0;

                foreach (FriendorFoe relatedFaction in relatedFactions)
                {
                    if (relatedFaction.status == "friend")
                    {
                        friendCount += 1;
                    }
                    else if (relatedFaction.status == "dislike")
                    {
                        dislikeCount += 1;
                    }
                    else if (relatedFaction.status == "hate")
                    {
                        hateCount += 1;
                    }
                }

                if (friendCount > 0)
                {
                    index = 0;
                    descriptionPostfix += "\nAdmired by &C";
                    foreach (FriendorFoe relatedFaction in relatedFactions)
                    {
                        if (relatedFaction.status == "friend")
                        {
                            index += 1;
                            if (index < (friendCount - 1))
                            {
                                descriptionPostfix += Faction.getFormattedName(relatedFaction.faction) + "&y, &C";
                            }
                            else if (index == (friendCount - 1))
                            {
                                descriptionPostfix += Faction.getFormattedName(relatedFaction.faction) + "&y, and &C";
                            }
                            else
                            {
                                descriptionPostfix += Faction.getFormattedName(relatedFaction.faction) + "&y.\n";
                            }
                        }
                    }
                }
                if (dislikeCount > 0)
                {
                    index = 0;
                    descriptionPostfix += "\nDisliked by &C";
                    foreach (FriendorFoe relatedFaction in relatedFactions)
                    {
                        if (relatedFaction.status == "dislike")
                        {
                            index += 1;
                            if (index < (dislikeCount - 1))
                            {
                                descriptionPostfix += Faction.getFormattedName(relatedFaction.faction) + "&y, &C";
                            }
                            else if (index == (dislikeCount - 1))
                            {
                                descriptionPostfix += Faction.getFormattedName(relatedFaction.faction) + "&y, and &C";
                            }
                            else
                            {
                                descriptionPostfix += Faction.getFormattedName(relatedFaction.faction) + "&y.\n";
                            }
                        }
                    }
                }
                if (hateCount > 0)
                {
                    index = 0;
                    descriptionPostfix += "\nHated by &C";
                    foreach (FriendorFoe relatedFaction in relatedFactions)
                    {
                        if (relatedFaction.status == "hate")
                        {
                            index += 1;
                            if (index < (hateCount - 1))
                            {
                                descriptionPostfix += Faction.getFormattedName(relatedFaction.faction) + "&y, &C";
                            }
                            else if (index == (hateCount - 1))
                            {
                                descriptionPostfix += Faction.getFormattedName(relatedFaction.faction) + "&y, and &C";
                            }
                            else
                            {
                                descriptionPostfix += Faction.getFormattedName(relatedFaction.faction) + "&y.\n";
                            }
                        }
                    }
                }
                return true;
            }
            else if(E.ID == "BeforeDeathRemoval")
            {
                // Give/Take Reputation when the creature is killed.
                GameObject myKiller = E.GetParameter("Killer") as GameObject;
                if (myKiller == null) return true;

                // Figure out how much to adjust by (repValue * Level)
                int repMod = repValue * ParentObject.Statistics["Level"].Value;
                if (repMod == 0) return true;

                // Did the player kill this Creature?
                if (myKiller.IsPlayer())
                {
                    // Adjust the players reputation.
                    Reputation myReputation = XRLCore.Core.Game.PlayerReputation;
                    foreach (KeyValuePair<string, int> item in ParentObject.pBrain.FactionMembership)
                    {
                        myReputation.modify(item.Key, -repMod, false);
                    }
                    foreach (FriendorFoe relatedFaction in relatedFactions)
                    {
                        if (relatedFaction.status == "friend")
                        {
                            if ((repMod / 2) > 0)
                            {
                                myReputation.modify(relatedFaction.faction, -repMod / 2, false);
                            }
                        }
                        else if (relatedFaction.status == "dislike")
                        {
                            if ((repMod / 4) > 0)
                            {
                                myReputation.modify(relatedFaction.faction, repMod / 4, false);
                            }
                        }
                        else if (relatedFaction.status == "hate")
                        {
                            if ((repMod / 2) > 0)
                            {
                                myReputation.modify(relatedFaction.faction, repMod / 2, false);
                            }
                        }
                    }
                } 
                else
                {
                    /* This section tries to give the reputation to the killer of the Creature. Normally this is
                     * not important, but when dominating or when allys make a kill this can be useful to track. 
                     */

                    // Get the brain of the killer.
                    Brain killersBrain = myKiller.GetPart("Brain") as Brain;
                    if (killersBrain == null) return true;

                    // Adjust the killers reputation.
                    foreach (KeyValuePair<string, int> item in ParentObject.pBrain.FactionMembership)
                    {
                        AddOrChangeFactionFeelings(killersBrain, item.Key, -repMod);
                    }
                    foreach (FriendorFoe relatedFaction in relatedFactions)
                    {
                        if (relatedFaction.status == "friend")
                        {
                            if ((repMod / 2) > 0)
                            {
                                AddOrChangeFactionFeelings(killersBrain, relatedFaction.faction, -repMod / 2);
                            }
                        }
                        else if (relatedFaction.status == "dislike")
                        {
                            if ((repMod / 4) > 0)
                            {
                                AddOrChangeFactionFeelings(killersBrain, relatedFaction.faction, repMod / 4);
                            }
                        }
                        else if (relatedFaction.status == "hate")
                        {
                            if ((repMod / 2) > 0)
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
