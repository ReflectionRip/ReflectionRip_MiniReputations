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
        public List<string> parentFactions = new List<string>();
        public List<FriendorFoe> relatedFactions = new List<FriendorFoe>();

        public rr_DeathReputation()
        {
            this.Name = "rr_DeathReputation";
        }

        public override bool SameAs(IPart p)
        {
            return true;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "BeforeDeathRemoval");
            Object.RegisterPartEvent(this, "FactionsAdded");
            Object.RegisterPartEvent(this, "GetShortDescription");
        }

        public override bool FireEvent(Event E)
        {
            // Stop if this object already gives reputation.
            if (ParentObject.GetPart("GivesRep") != null)
            {
                return base.FireEvent(E);
            }

            // Add the Factions
            if (E.ID == "FactionsAdded")
            {
                int index = 0;

                foreach (KeyValuePair<string, int> item in ParentObject.pBrain.FactionMembership)
                {
                    parentFactions.Add(item.Key);
                }

                int numParents = parentFactions.Count;
                if (numParents == 0) return false;

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

                int maxFactions = Rules.Stat.Random(1, 3);
                List<string[]> factionList = new List<string[]>();

                for (index = 1; index <= maxFactions; ++index)
                {
                    Brain myBrain = ParentObject.GetPart("Brain") as Brain;
                    string FoF = GenerateFriendOrFoe.getRandomFaction(ParentObject);
                    int randPercent = Rules.Stat.Random(1, 100);
                    if (randPercent <= 10)
                    {
                        if (myBrain.FactionFeelings.ContainsKey(FoF))
                        {
                            myBrain.FactionFeelings[FoF] += 100;
                        }
                        else
                        {
                            myBrain.FactionFeelings.Add(FoF, 100);
                        }
                        string reason = GenerateFriendOrFoe.getLikeReason();
                        relatedFactions.Add(new FriendorFoe(FoF, "friend", reason));
                    }
                    else if (randPercent <= 55)
                    {
                        if (myBrain.FactionFeelings.ContainsKey(FoF))
                        {
                            myBrain.FactionFeelings[FoF] -= 50;
                        }
                        else
                        {
                            myBrain.FactionFeelings.Add(FoF, -50);
                        }
                        string reason = GenerateFriendOrFoe.getHateReason();
                        relatedFactions.Add(new FriendorFoe(FoF, "dislike", reason));
                    }
                    else
                    {
                        if (myBrain.FactionFeelings.ContainsKey(FoF))
                        {
                            myBrain.FactionFeelings[FoF] -= 100;
                        }
                        else
                        {
                            myBrain.FactionFeelings.Add(FoF, -100);
                        }
                        string reason = GenerateFriendOrFoe.getHateReason();
                        relatedFactions.Add(new FriendorFoe(FoF, "hate", reason));
                    }
                }

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
            if (E.ID == "BeforeDeathRemoval")
            {
                GameObject myKiller = E.GetParameter("Killer") as GameObject;
                if (myKiller != null && myKiller.IsPlayer())
                {
                    int repMod = repValue * ParentObject.Statistics["Level"].Value;
                    if (repMod == 0) return true;

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
